// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Parsers;

/// <summary>
/// A ref struct that processes markdown inline elements using a delimiter-based algorithm.
/// Parses emphasis, strong, code, links, and images from a given text span.
/// </summary>
public ref struct RefInlineProcessor
{
    private readonly Span<char> _source;
    private RefCollection<Inline> _inlines;
    private RefCollection<DelimiterRun> _delimiters;

    /// <summary>
    /// Represents a run of emphasis/strong delimiters in the text.
    /// </summary>
    public struct DelimiterRun
    {
        public int Position { get; set; }
        public char Char { get; set; }
        public int Count { get; set; }
        public bool IsEmphasisDelimiter { get; set; }
        public bool IsOpener { get; set; }
        public bool IsCloser { get; set; }
    }

    public RefInlineProcessor(Span<char> source, Span<Inline> inlineBuffer, Span<DelimiterRun> delimiterBuffer)
    {
        _source = source;
        _inlines = new RefCollection<Inline>(inlineBuffer);
        _delimiters = new RefCollection<DelimiterRun>(delimiterBuffer);
    }

    /// <summary>
    /// Processes inline elements from the given content span.
    /// Returns the inline elements found.
    /// </summary>
    public Span<Inline> ProcessInlines(int contentStart, int contentEnd)
    {
        if (contentStart >= contentEnd)
            return Array.Empty<Inline>();

        var content = _source[contentStart..contentEnd];

        // First pass: collect delimiters (emphasis/strong)
        CollectDelimiters(contentStart, content);

        // Process the text character by character
        int pos = 0;
        while (pos < content.Length)
        {
            // Try to parse code spans (backticks)
            if (TryParseCodeSpan(contentStart, content, ref pos))
                continue;

            // Try to parse links and images
            if (TryParseLink(contentStart, content, ref pos))
                continue;

            // Try to parse emphasis/strong
            if (TryParseEmphasis(contentStart, content, ref pos))
                continue;

            // Try to parse hard/soft line breaks
            if (TryParseLineBreak(content, ref pos))
                continue;

            // Try to parse autolinks (must be before HTML tags)
            if (TryParseAutoLink(contentStart, content, ref pos))
                continue;

            // Try to parse HTML tags
            if (TryParseHtmlInline(contentStart, content, ref pos))
                continue;

            // Otherwise, accumulate literal text
            ParseLiteral(contentStart, content, ref pos);
        }

        return _inlines.AsReadOnlySpan().ToArray();
    }

    /// <summary>
    /// Collects delimiter runs (emphasis/strong markers).
    /// </summary>
    private void CollectDelimiters(int contentStart, ReadOnlySpan<char> content)
    {
        for (int i = 0; i < content.Length; i++)
        {
            char ch = content[i];

            // Check for potential emphasis/strong delimiters
            if (ch is '*' or '_')
            {
                int count = 1;
                while (i + count < content.Length && content[i + count] == ch)
                    count++;

                var delim = new DelimiterRun
                {
                    Position = contentStart + i,
                    Char = ch,
                    Count = count,
                    IsEmphasisDelimiter = true,
                };

                // Basic rules for opener/closer
                // TODO: Implement full emphasis parsing rules from CommonMark spec
                bool beforeIsSpace = i == 0 || char.IsWhiteSpace(content[i - 1]);
                bool afterIsSpace = i + count >= content.Length || char.IsWhiteSpace(content[i + count]);

                delim.IsOpener = !afterIsSpace || (beforeIsSpace && (ch == '*'));
                delim.IsCloser = !beforeIsSpace || (afterIsSpace && (ch == '*'));

                _delimiters.Add(delim);
                i += count - 1;
            }
        }
    }

    /// <summary>
    /// Tries to parse a code span starting at the current position.
    /// Code spans are delimited by backticks (` or ``).
    /// </summary>
    /// <returns>True if a code span was parsed.</returns>
    private bool TryParseCodeSpan(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length || content[pos] != '`')
            return false;

        // Count opening backticks
        int openingCount = 0;
        int startPos = pos;
        while (pos < content.Length && content[pos] == '`')
        {
            openingCount++;
            pos++;
        }

        // Find closing backticks (same count, but not at the beginning)
        int closingPos = pos;
        while (closingPos < content.Length)
        {
            if (content[closingPos] == '`')
            {
                int closingCount = 0;
                int tempPos = closingPos;
                while (tempPos < content.Length && content[tempPos] == '`')
                {
                    closingCount++;
                    tempPos++;
                }

                if (closingCount == openingCount)
                {
                    // Found matching closing backticks
                    int codeStart = pos;
                    int codeEnd = closingPos;

                    // Trim one space from each side if present and code is surrounded by spaces
                    if (codeStart < codeEnd &&
                        content[codeStart] == ' ' &&
                        content[codeEnd - 1] == ' ' &&
                        codeEnd - codeStart > 1)
                    {
                        codeStart++;
                        codeEnd--;
                    }

                    _inlines.Add(Inline.CreateCode(contentStart + codeStart, contentStart + codeEnd));
                    pos = tempPos;
                    return true;
                }

                closingPos = tempPos;
            }
            else
            {
                closingPos++;
            }
        }

        // No closing backticks found, treat opening as literal
        pos = startPos + 1;
        return true; // Consume one backtick as processed
    }

    /// <summary>
    /// Tries to parse a link or image at the current position.
    /// Forms: [text](url) or ![alt](url) or [text](url "title")
    /// </summary>
    private bool TryParseLink(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length)
            return false;

        bool isImage = false;
        int startPos = pos;

        // Check for image (![...](url))
        if (content[pos] == '!' && pos + 1 < content.Length && content[pos + 1] == '[')
        {
            isImage = true;
            pos += 2;
        }
        // Check for link ([...](url))
        else if (content[pos] == '[')
        {
            pos++;
        }
        else
        {
            return false;
        }

        // Find closing ]
        int textStart = pos;
        int bracketCount = 1;
        while (pos < content.Length && bracketCount > 0)
        {
            if (content[pos] == '[' && (pos == 0 || content[pos - 1] != '\\'))
                bracketCount++;
            else if (content[pos] == ']' && (pos == 0 || content[pos - 1] != '\\'))
                bracketCount--;
            pos++;
        }

        if (bracketCount != 0)
        {
            // No closing bracket, reset and return false
            pos = startPos + (isImage ? 2 : 1);
            return false;
        }

        int textEnd = pos - 1;

        // Now we should have (url) or (url "title")
        if (pos >= content.Length || content[pos] != '(')
        {
            // No link destination, reset
            pos = startPos + (isImage ? 2 : 1);
            return false;
        }

        pos++; // Skip '('

        // Skip whitespace
        while (pos < content.Length && char.IsWhiteSpace(content[pos]))
            pos++;

        // Parse URL
        int urlStart = pos;
        while (pos < content.Length && content[pos] != ')' && content[pos] != '"' && content[pos] != '\'' && content[pos] != '(')
        {
            if (char.IsWhiteSpace(content[pos]))
                break;
            pos++;
        }
        int urlEnd = pos;

        // Skip whitespace after URL
        while (pos < content.Length && char.IsWhiteSpace(content[pos]) && content[pos] != ')')
            pos++;

        // Try to parse title
        int titleStart = 0, titleEnd = 0;
        if (pos < content.Length && (content[pos] == '"' || content[pos] == '\''))
        {
            char quoteChar = content[pos];
            pos++;
            titleStart = pos;
            while (pos < content.Length && content[pos] != quoteChar)
                pos++;
            titleEnd = pos;
            if (pos < content.Length && content[pos] == quoteChar)
                pos++;
        }

        // Skip whitespace
        while (pos < content.Length && char.IsWhiteSpace(content[pos]))
            pos++;

        // Should end with )
        if (pos >= content.Length || content[pos] != ')')
        {
            // Invalid link syntax, reset
            pos = startPos + (isImage ? 2 : 1);
            return false;
        }

        pos++; // Skip ')'

        // Create the link or image inline
        if (isImage)
        {
            _inlines.Add(Inline.CreateImage(contentStart + textStart, contentStart + textEnd,
                contentStart + urlStart, contentStart + urlEnd,
                titleStart > 0 ? contentStart + titleStart : 0,
                titleEnd > 0 ? contentStart + titleEnd : 0));
        }
        else
        {
            _inlines.Add(Inline.CreateLink(contentStart + textStart, contentStart + textEnd,
                contentStart + urlStart, contentStart + urlEnd,
                titleStart > 0 ? contentStart + titleStart : 0,
                titleEnd > 0 ? contentStart + titleEnd : 0));
        }

        return true;
    }

    /// <summary>
    /// Tries to parse emphasis or strong emphasis at the current position.
    /// Simple implementation of emphasis parsing rules from CommonMark spec.
    /// </summary>
    private bool TryParseEmphasis(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length || !IsEmphasisChar(content[pos]))
            return false;

        char emphasisChar = content[pos];
        int startPos = pos;
        int count = 0;

        // Count consecutive emphasis characters
        while (pos < content.Length && content[pos] == emphasisChar)
        {
            count++;
            pos++;
        }

        // Determine if this can be an opener
        bool beforeIsSpace = startPos == 0 || char.IsWhiteSpace(content[startPos - 1]);
        bool beforeIsPunct = startPos > 0 && IsPunctuation(content[startPos - 1]);
        bool afterIsSpace = pos >= content.Length || char.IsWhiteSpace(content[pos]);
        bool afterIsPunct = pos < content.Length && IsPunctuation(content[pos]);

        bool canOpen = !afterIsSpace && (!afterIsPunct || beforeIsSpace || beforeIsPunct);

        if (!canOpen)
        {
            return false; // Can't open an emphasis
        }

        // Search for closing delimiters
        int closingPos = pos;
        int closingStart;
        int closingCount = 0;

        while (closingPos < content.Length)
        {
            if (content[closingPos] == emphasisChar)
            {
                closingStart = closingPos;
                closingCount = 0;

                while (closingPos < content.Length && content[closingPos] == emphasisChar)
                {
                    closingCount++;
                    closingPos++;
                }

                // Check if this can be a closer
                bool closerBeforeIsSpace = false;
                bool closerBeforePunct = false;
                if (closingStart > 0)
                {
                    closerBeforeIsSpace = char.IsWhiteSpace(content[closingStart - 1]);
                    closerBeforePunct = IsPunctuation(content[closingStart - 1]);
                }

                bool closerAfterIsSpace = closingPos >= content.Length || char.IsWhiteSpace(content[closingPos]);
                bool closerAfterPunct = closingPos < content.Length && IsPunctuation(content[closingPos]);

                bool canBeCloser = !closerBeforeIsSpace && (!closerBeforePunct || closerAfterIsSpace || closerAfterPunct);

                if (canBeCloser && closingCount >= count)
                {
                    // Found a match! Determine if emphasis or strong
                    int emphasisContentStart = pos;
                    int emphasisContentEnd = closingStart;

                    if (count >= 2 && closingCount >= 2)
                    {
                        // Strong (use 2 delimiters)
                        _inlines.Add(Inline.CreateStrong(emphasisChar, 0, 0));
                        pos = closingStart + 2;
                        return true;
                    }
                    else if (count >= 1 && closingCount >= 1)
                    {
                        // Emphasis (use 1 delimiter)
                        _inlines.Add(Inline.CreateEmphasis(emphasisChar, 0, 0));
                        pos = closingStart + 1;
                        return true;
                    }
                }

                // Continue searching
                closingPos = closingStart + 1; // Move past the first delimiter to avoid infinite loop
            }
            else
            {
                closingPos++;
            }
        }

        // No closing delimiter found, treat as literal
        pos = startPos;
        return false;
    }

    /// <summary>
    /// Checks if a character is a valid emphasis/strong delimiter.
    /// </summary>
    private static bool IsEmphasisChar(char ch) => ch is '*' or '_';

    /// <summary>
    /// Checks if a character is a punctuation character (for CommonMark emphasis rules).
    /// </summary>
    private static bool IsPunctuation(char ch)
    {
        return char.IsPunctuation(ch) || ch is '!' or '"' or '#' or '$' or '%' or '&' or '\'' or '(' or ')'
            or '*' or '+' or ',' or '-' or '.' or '/' or ':' or ';' or '<' or '=' or '>' or '?' or '@'
            or '[' or '\\' or ']' or '^' or '_' or '`' or '{' or '|' or '}' or '~';
    }

    /// <summary>
    /// Tries to parse a line break at the current position.
    /// Hard breaks: two spaces + newline or backslash + newline.
    /// Soft breaks: just newline.
    /// </summary>
    private bool TryParseLineBreak(ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length)
            return false;

        // Check for hard line break
        if (content[pos] == '\\' && pos + 1 < content.Length &&
            (content[pos + 1] == '\n' || content[pos + 1] == '\r'))
        {
            _inlines.Add(Inline.CreateHardLineBreak());
            pos += 2;
            if (pos < content.Length && content[pos - 1] == '\r' && content[pos] == '\n')
                pos++;
            return true;
        }

        // Check for two spaces + newline
        if (pos + 1 < content.Length && content[pos] == ' ' && content[pos + 1] == ' ')
        {
            int tempPos = pos + 2;
            if (tempPos < content.Length && (content[tempPos] == '\n' || content[tempPos] == '\r'))
            {
                _inlines.Add(Inline.CreateHardLineBreak());
                pos = tempPos + 1;
                if (pos < content.Length && content[pos - 1] == '\r' && content[pos] == '\n')
                    pos++;
                return true;
            }
        }

        // Check for soft line break
        if (content[pos] == '\n' || content[pos] == '\r')
        {
            _inlines.Add(Inline.CreateSoftLineBreak());
            pos++;
            if (pos < content.Length && content[pos - 1] == '\r' && content[pos] == '\n')
                pos++;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to parse an HTML tag or entity at the current position.
    /// </summary>
    private bool TryParseHtmlInline(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length || content[pos] != '<')
            return false;

        int startPos = pos;
        pos++;

        // Find closing >
        while (pos < content.Length && content[pos] != '>')
            pos++;

        if (pos >= content.Length)
        {
            pos = startPos + 1;
            return false;
        }

        pos++; // Include the >
        _inlines.Add(Inline.CreateHtmlInline(contentStart + startPos, contentStart + pos));
        return true;
    }

    /// <summary>
    /// Tries to parse an autolink (automatic linkification) at the current position.
    /// Form: &lt;url&gt;
    /// </summary>
    private bool TryParseAutoLink(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        if (pos >= content.Length || content[pos] != '<')
            return false;

        int startPos = pos;
        pos++;

        // Find the closing >
        int urlStart = pos;
        while (pos < content.Length && content[pos] != '>')
            pos++;

        if (pos >= content.Length)
        {
            pos = startPos + 1;
            return false;
        }

        int urlEnd = pos;
        pos++; // Skip '>'

        // Basic check: if content looks like a URL or email
        var urlContent = content[urlStart..urlEnd];
        if (ContainsChar(urlContent, '@') || ContainsSequence(urlContent, "://"))
        {
            _inlines.Add(Inline.CreateAutoLink(contentStart + urlStart, contentStart + urlEnd));
            return true;
        }

        // Not a valid autolink, reset
        pos = startPos + 1;
        return false;
    }

    /// <summary>
    /// Parses literal text until a special character is found.
    /// Accumulates consecutive literal characters into a single literal inline.
    /// </summary>
    private void ParseLiteral(int contentStart, ReadOnlySpan<char> content, ref int pos)
    {
        int literalStart = pos;

        while (pos < content.Length)
        {
            char ch = content[pos];

            // Stop at special characters
            if (ch is '`' or '[' or '!' or '\\' or '<' or '*' or '_' or '\n' or '\r')
                break;

            pos++;
        }

        // If we're at a literal position, add it
        if (pos > literalStart)
        {
            _inlines.Add(Inline.CreateLiteral(contentStart + literalStart, contentStart + pos));
        }
        else if (pos < content.Length)
        {
            // We're at a special character that couldn't be parsed; consume it as literal
            _inlines.Add(Inline.CreateLiteral(contentStart + pos, contentStart + pos + 1));
            pos++;
        }
    }

    /// <summary>
    /// Checks if a span contains a specific character.
    /// </summary>
    private static bool ContainsChar(ReadOnlySpan<char> span, char ch)
    {
        foreach (var c in span)
        {
            if (c == ch)
                return true;
        }
        return false;
    }

    /// <summary>
    /// Checks if a span contains a specific sequence.
    /// </summary>
    private static bool ContainsSequence(ReadOnlySpan<char> span, string sequence)
    {
        if (sequence.Length > span.Length)
            return false;

        for (int i = 0; i <= span.Length - sequence.Length; i++)
        {
            bool match = true;
            for (int j = 0; j < sequence.Length; j++)
            {
                if (span[i + j] != sequence[j])
                {
                    match = false;
                    break;
                }
            }
            if (match)
                return true;
        }
        return false;
    }
}
