// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Parsers;

/// <summary>
/// A ref struct that processes markdown blocks using a two-pass algorithm.
/// First pass: collect line boundaries; second pass: identify block types.
/// </summary>
public ref struct RefBlockProcessor
{
    private readonly Span<char> _source;
    private RefCollection<Block> _blocks;
    private RefCollection<int> _lineBoundaries; // Stores start and end index of each line
    private RefCollection<int> _containerStack;
    private int _lineCount;

    public RefBlockProcessor(Span<char> source, Span<Block> blockBuffer, Span<int> containerBuffer, Span<int> lineBoundaryBuffer)
    {
        _source = source;
        _blocks = new RefCollection<Block>(blockBuffer);
        _containerStack = new RefCollection<int>(containerBuffer);
        _lineBoundaries = new RefCollection<int>(lineBoundaryBuffer);
        _lineCount = 0;
    }

    /// <summary>
    /// Processes all lines from the source and returns the parsed blocks.
    /// </summary>
    public Span<Block> ProcessBlocks()
    {
        // First pass: collect line boundaries
        var lineReader = new RefLineReader(_source);
        while (lineReader.HasMore)
        {
            var line = lineReader.ReadLine();

            // Store the actual boundaries of the line content (without line terminators)
            _lineBoundaries.Add(line.Start);
            _lineBoundaries.Add(line.End);
            _lineCount++;
        }

        // Second pass: process each line to identify block types
        for (int i = 0; i < _lineCount; i++)
        {
            ProcessLine(i);
        }

        // Close any remaining open containers
        while (_containerStack.Length > 0)
        {
            CloseTopContainer();
        }

        return _blocks.AsReadOnlySpan().ToArray();
    }

    /// <summary>
    /// Gets a line view by index.
    /// </summary>
    private RefStringView GetLine(int lineIndex)
    {
        if (lineIndex < 0 || lineIndex >= _lineCount)
            return new RefStringView(_source, 0, 0);

        var boundariesSpan = _lineBoundaries.AsSpan();
        int start = boundariesSpan[lineIndex * 2];
        int end = boundariesSpan[lineIndex * 2 + 1];
        return new RefStringView(_source, start, end);
    }

    /// <summary>
    /// Processes a single line, determining what type of block it belongs to.
    /// For paragraphs, accumulates consecutive non-blank, non-block lines.
    /// </summary>
    private void ProcessLine(int lineIndex)
    {
        var line = GetLine(lineIndex);

        // Get indent (number of spaces)
        int indent = GetLineIndent(line, out var trimmedLine);

        // Check if this line is truly empty (no content when trimmed)
        if (trimmedLine.IsEmpty)
        {
            AddBlock(Block.CreateBlankLine(lineIndex, 0), lineIndex);
            return;
        }

        // Check for ATX headings
        if (TryParseHeading(lineIndex, trimmedLine, indent, out var headingBlock))
        {
            AddBlock(headingBlock, lineIndex);
            return;
        }

        // Check for thematic breaks
        if (TryParseThematicBreak(lineIndex, trimmedLine, indent, line, out var breakBlock))
        {
            AddBlock(breakBlock, lineIndex);
            return;
        }

        // Check for fenced code blocks
        if (TryParseFencedCodeBlock(lineIndex, trimmedLine, indent, out var fencedCodeBlock))
        {
            AddBlock(fencedCodeBlock, lineIndex);
            return;
        }

        // Check for HTML blocks
        if (TryParseHtmlBlock(lineIndex, trimmedLine, indent, line, out var htmlBlock))
        {
            AddBlock(htmlBlock, lineIndex);
            return;
        }

        // Check for block quotes
        if (TryParseListOrQuote(lineIndex, trimmedLine, indent, out var containerBlock))
        {
            AddBlock(containerBlock, lineIndex);
            return;
        }

        // Check for indented code blocks (4-space indent)
        if (indent >= 4 && !IsInTightList())
        {
            var codeBlock = ParseIndentedCodeBlock(lineIndex, line);
            AddBlock(codeBlock, lineIndex);
            return;
        }

        // Default to paragraph - try to accumulate consecutive lines
        var blockSpan = _blocks.AsReadOnlySpan();
        bool shouldMergeWithPrevious = false;

        if (blockSpan.Length > 0)
        {
            var lastBlock = blockSpan[blockSpan.Length - 1];
            // Only merge if last block is a paragraph AND it's not a container AND it's not a blank line
            shouldMergeWithPrevious = lastBlock.Type == BlockType.Paragraph && lastBlock.FirstChildIndex == 0;
        }

        if (shouldMergeWithPrevious)
        {
            // Extend the last paragraph block to include this line
            var mutableSpan = _blocks.AsSpan();
            var lastBlock = mutableSpan[_blocks.Length - 1];
            lastBlock.LineCount++;
            lastBlock.ContentEnd = line.End;
            mutableSpan[_blocks.Length - 1] = lastBlock;
            return;
        }

        // Create new paragraph block
        var paraBlock = Block.CreateParagraph(lineIndex, indent);
        paraBlock.ContentStart = line.Start;
        paraBlock.ContentEnd = line.End;
        paraBlock.LineCount = 1;
        AddBlock(paraBlock, lineIndex);
    }

    /// <summary>
    /// Tries to parse an ATX heading (# Heading).
    /// </summary>
    private bool TryParseHeading(int lineIndex, RefStringView trimmedLine, int indent, out Block headingBlock)
    {
        headingBlock = default;

        if (trimmedLine.Length < 2 || trimmedLine[0] != '#')
            return false;

        // Count opening hashes
        int level = 0;
        int i = 0;
        while (i < trimmedLine.Length && trimmedLine[i] == '#' && level < 6)
        {
            level++;
            i++;
        }

        // Must have space or end of line after hashes
        if (i < trimmedLine.Length && !char.IsWhiteSpace(trimmedLine[i]))
            return false;

        // Skip spaces after hashes
        while (i < trimmedLine.Length && char.IsWhiteSpace(trimmedLine[i]))
            i++;

        // Skip trailing hashes and spaces
        int contentEnd = i;
        while (contentEnd < trimmedLine.Length && trimmedLine[contentEnd] != '#')
            contentEnd++;

        // Trim trailing space before trailing hashes
        while (contentEnd > i && char.IsWhiteSpace(trimmedLine[contentEnd - 1]))
            contentEnd--;

        // Calculate content span in original source
        int contentStart = trimmedLine.Start + i;
        int contentEndInSource = trimmedLine.Start + contentEnd;

        headingBlock = Block.CreateHeading(lineIndex, indent, level, '#');
        headingBlock.ContentStart = contentStart;
        headingBlock.ContentEnd = contentEndInSource;
        headingBlock.LineCount = 1;

        return true;
    }

    /// <summary>
    /// Tries to parse a thematic break (---, ***, ___).
    /// </summary>
    private bool TryParseThematicBreak(int lineIndex, RefStringView trimmedLine, int indent, RefStringView originalLine, out Block breakBlock)
    {
        breakBlock = default;

        if (indent > 3 || trimmedLine.Length < 3)
            return false;

        char breakChar = '\0';
        int count = 0;

        for (int i = 0; i < trimmedLine.Length; i++)
        {
            char c = trimmedLine[i];

            if (c == '-' || c == '*' || c == '_')
            {
                if (breakChar == '\0')
                    breakChar = c;
                else if (c != breakChar)
                    return false;

                count++;
            }
            else if (!char.IsWhiteSpace(c))
            {
                return false;
            }
        }

        if (count < 3)
            return false;

        breakBlock = Block.CreateThematicBreak(lineIndex, indent, breakChar);
        breakBlock.ContentStart = originalLine.Start;
        breakBlock.ContentEnd = originalLine.End;
        breakBlock.LineCount = 1;

        return true;
    }

    /// <summary>
    /// Tries to parse a fenced code block (``` or ~~~).
    /// </summary>
    private bool TryParseFencedCodeBlock(int lineIndex, RefStringView trimmedLine, int indent, out Block codeBlock)
    {
        codeBlock = default;

        if (indent > 3 || trimmedLine.Length < 3)
            return false;

        char fenceChar = trimmedLine[0];
        if (fenceChar != '`' && fenceChar != '~')
            return false;

        // Count fence characters
        int fenceLength = 0;
        while (fenceLength < trimmedLine.Length && trimmedLine[fenceLength] == fenceChar)
            fenceLength++;

        if (fenceLength < 3)
            return false;

        // For backticks, remaining chars shouldn't contain backticks
        if (fenceChar == '`')
        {
            for (int i = fenceLength; i < trimmedLine.Length; i++)
            {
                if (trimmedLine[i] == '`')
                    return false;
            }
        }

        // Extract info string (language identifier)
        int infoStart = fenceLength;
        while (infoStart < trimmedLine.Length && char.IsWhiteSpace(trimmedLine[infoStart]))
            infoStart++;

        int infoEnd = infoStart;
        while (infoEnd < trimmedLine.Length && trimmedLine[infoEnd] != '\n' && trimmedLine[infoEnd] != '\r')
            infoEnd++;

        while (infoEnd > infoStart && char.IsWhiteSpace(trimmedLine[infoEnd - 1]))
            infoEnd--;

        codeBlock = Block.CreateCodeBlock(lineIndex, indent, isFenced: true, fenceChar);
        codeBlock.Data1 = fenceLength; // Store fence length
        if (infoStart < infoEnd)
        {
            codeBlock.DataViewStart = trimmedLine.Start + infoStart;
            codeBlock.DataViewEnd = trimmedLine.Start + infoEnd;
        }

        // Collect all lines until closing fence
        codeBlock.ContentStart = lineIndex + 1; // Line number where content starts
        int endLine = lineIndex + 1;
        while (endLine < _lineCount)
        {
            var contentLine = GetLine(endLine);
            int contentIndent = GetLineIndent(contentLine, out var contentTrimmed);

            if (contentIndent <= 3 && contentTrimmed.Length >= fenceLength)
            {
                // Check for closing fence
                bool isClosingFence = true;
                for (int i = 0; i < fenceLength; i++)
                {
                    if (contentTrimmed[i] != fenceChar)
                    {
                        isClosingFence = false;
                        break;
                    }
                }

                if (isClosingFence)
                {
                    // Check that rest of line is whitespace
                    int j = fenceLength;
                    while (j < contentTrimmed.Length && char.IsWhiteSpace(contentTrimmed[j]))
                        j++;

                    if (j == contentTrimmed.Length)
                    {
                        codeBlock.ContentEnd = endLine; // Store ending line number
                        codeBlock.LineCount = endLine - lineIndex + 1;
                        return true;
                    }
                }
            }

            endLine++;
        }

        // No closing fence found - still treat as code block until EOF
        codeBlock.ContentEnd = _lineCount - 1;
        codeBlock.LineCount = _lineCount - lineIndex;
        return true;
    }

    /// <summary>
    /// Parses an indented code block (4-space indent).
    /// </summary>
    private Block ParseIndentedCodeBlock(int lineIndex, RefStringView line)
    {
        var codeBlock = Block.CreateCodeBlock(lineIndex, 0, isFenced: false, '\0');
        codeBlock.ContentStart = line.Start;
        codeBlock.LineCount = 1;
        codeBlock.ContentEnd = line.End;

        return codeBlock;
    }

    /// <summary>
    /// Tries to parse an HTML block (basic).
    /// </summary>
    private bool TryParseHtmlBlock(int lineIndex, RefStringView trimmedLine, int indent, RefStringView originalLine, out Block htmlBlock)
    {
        htmlBlock = default;

        if (indent > 3 || trimmedLine.Length < 2 || trimmedLine[0] != '<')
            return false;

        // Basic HTML block types
        // Check for common HTML tags
        if (TryMatchHtmlTag(trimmedLine, out int tagEnd))
        {
            htmlBlock = Block.CreateHtmlBlock(lineIndex, indent);
            htmlBlock.ContentStart = originalLine.Start;
            htmlBlock.ContentEnd = originalLine.End;
            htmlBlock.LineCount = 1;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to parse a block quote or list.
    /// </summary>
    private bool TryParseListOrQuote(int lineIndex, RefStringView trimmedLine, int indent, out Block containerBlock)
    {
        containerBlock = default;

        // Block quote: starts with >
        if (trimmedLine.Length > 0 && trimmedLine[0] == '>')
        {
            int quoteIndent = 1;
            // Skip optional space after >
            if (quoteIndent < trimmedLine.Length && trimmedLine[quoteIndent] == ' ')
                quoteIndent++;

            containerBlock = Block.CreateQuote(lineIndex, indent);
            containerBlock.FirstChildIndex = _blocks.Length;
            containerBlock.ChildCount = 0;
            _containerStack.Add(_blocks.Length);

            return true;
        }

        // List: starts with -, +, * (unordered) or digit + . or ) (ordered)
        if (TryParseListMarker(trimmedLine, out bool isOrdered, out char marker, out int markerLength, out int contentsAfterMarker))
        {
            var listBlock = Block.CreateList(lineIndex, indent, isOrdered, marker);
            listBlock.FirstChildIndex = _blocks.Length;
            listBlock.ChildCount = 0;
            _containerStack.Add(_blocks.Length);

            containerBlock = listBlock;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Tries to parse a list marker (-, +, *, digit+., digit+)).
    /// </summary>
    private bool TryParseListMarker(RefStringView trimmedLine, out bool isOrdered, out char marker, out int markerLength, out int contentsAfterMarker)
    {
        isOrdered = false;
        marker = '\0';
        markerLength = 0;
        contentsAfterMarker = 0;

        if (trimmedLine.Length < 2)
            return false;

        // Unordered list markers: -, +, *
        if (trimmedLine[0] is '-' or '+' or '*')
        {
            // Must be followed by space or tab or EOL
            if (trimmedLine.Length > 1 && !char.IsWhiteSpace(trimmedLine[1]))
                return false;

            marker = trimmedLine[0];
            markerLength = 1;
            contentsAfterMarker = 1;

            // Skip one space after marker
            if (contentsAfterMarker < trimmedLine.Length && trimmedLine[contentsAfterMarker] == ' ')
                contentsAfterMarker++;

            return true;
        }

        // Ordered list markers: digit(s) followed by . or )
        if (char.IsDigit(trimmedLine[0]))
        {
            int numLength = 0;
            while (numLength < trimmedLine.Length && numLength < 10 && char.IsDigit(trimmedLine[numLength]))
                numLength++;

            if (numLength < 1 || numLength > 9)
                return false;

            if (numLength >= trimmedLine.Length)
                return false;

            char delim = trimmedLine[numLength];
            if (delim != '.' && delim != ')')
                return false;

            // Must be followed by space or tab or EOL
            if (numLength + 1 < trimmedLine.Length && !char.IsWhiteSpace(trimmedLine[numLength + 1]))
                return false;

            isOrdered = true;
            marker = delim;
            markerLength = numLength + 1;
            contentsAfterMarker = markerLength;

            // Skip one space after marker
            if (contentsAfterMarker < trimmedLine.Length && trimmedLine[contentsAfterMarker] == ' ')
                contentsAfterMarker++;

            return true;
        }

        return false;
    }

    /// <summary>
    /// Checks if we're currently inside a tight list (affects paragraph behavior).
    /// </summary>
    private bool IsInTightList()
    {
        // For now, return false - this is simplified
        // A full implementation would track list state
        return false;
    }

    /// <summary>
    /// Tries to match an HTML tag.
    /// </summary>
    private bool TryMatchHtmlTag(RefStringView line, out int tagEnd)
    {
        tagEnd = -1;

        if (line.Length < 3 || line[0] != '<')
            return false;

        // HTML opening/closing tags, comments, processing instructions
        if (line[1] == '!' || line[1] == '?' || line[1] == '/')
        {
            // Simple approach: just find any >
            for (int i = 1; i < line.Length; i++)
            {
                if (line[i] == '>')
                {
                    tagEnd = i + 1;
                    return true;
                }
            }
        }
        else if (char.IsLetter(line[1]) || line[1] == ':')
        {
            // Opening tag - find the closing >
            for (int i = 1; i < line.Length; i++)
            {
                if (line[i] == '>')
                {
                    tagEnd = i + 1;
                    return true;
                }
            }
        }

        return false;
    }

    /// <summary>
    /// Gets the indentation level of a line (number of spaces).
    /// </summary>
    private int GetLineIndent(RefStringView line, out RefStringView trimmedLine)
    {
        int indent = 0;
        int i = 0;

        while (i < line.Length && line[i] == ' ')
        {
            indent++;
            i++;
        }

        // Tabs count as moving to next tab stop (indent % 4)
        while (i < line.Length && line[i] == '\t')
        {
            indent += 4 - (indent % 4);
            i++;
        }

        trimmedLine = new RefStringView(_source, line.Start + i, line.End);
        return indent;
    }

    /// <summary>
    /// Adds a block and closes any open containers that should be closed by this block.
    /// </summary>
    private void AddBlock(Block block, int lineIndex)
    {
        // Close open containers if this block isn't a continuation
        if (_containerStack.Length > 0 && !ShouldContinueContainer(block, lineIndex))
        {
            CloseTopContainer();
        }

        _blocks.Add(block);
    }

    /// <summary>
    /// Checks if a block should continue an open container.
    /// </summary>
    private bool ShouldContinueContainer(Block block, int lineIndex)
    {
        if (_containerStack.Length == 0)
            return false;

        // Blank lines can be part of containers
        if (block.Type == BlockType.BlankLine)
            return true;

        // Otherwise, check if block is indented enough
        // This is simplified - a full implementation would check proper List Item indentation
        return false;
    }

    /// <summary>
    /// Closes the top-most open container.
    /// </summary>
    private void CloseTopContainer()
    {
        if (_containerStack.Length == 0)
            return;

        // Get the last element (we need to work with RefCollection)
        ReadOnlySpan<int> containerSpan = _containerStack.AsReadOnlySpan();
        if (containerSpan.Length == 0)
            return;

        int containerIndex = containerSpan[containerSpan.Length - 1];
        if (containerIndex >= 0)
        {
            var blockSpan = _blocks.AsReadOnlySpan();
            if (containerIndex < blockSpan.Length)
            {
                var container = blockSpan[containerIndex];
                container.ChildCount = _blocks.Length - container.FirstChildIndex - 1;
                // Update the block in the collection by writing to the buffer
                var mutableSpan = _blocks.AsSpan();
                mutableSpan[containerIndex] = container;
            }
        }

        // Manually remove the last item by decrementing length
        _containerStack.Length = _containerStack.Length - 1;
    }
}
