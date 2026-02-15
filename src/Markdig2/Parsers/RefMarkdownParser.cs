// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Diagnostics;
using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Parsers;

/// <summary>
/// Main entry point for parsing markdown from a Span of characters.
/// </summary>
public static class RefMarkdownParser
{
    /// <summary>
    /// Parses markdown text into a RefMarkdownDocument.
    /// </summary>
    /// <param name="source">The markdown source text.</param>
    /// <returns>A RefMarkdownDocument containing the parsed structure.</returns>
    public static RefMarkdownDocument Parse(Span<char> source)
    {
        // Use stack allocation for small documents, pool for larger ones
        RefCollection<Block> blocks = new(stackalloc Block[64]);
        var lineReader = new RefLineReader(source);

        int currentLine = 0;
        int paragraphStartLine = -1;
        int paragraphStartOffset = -1;
        int paragraphEndOffset = -1;

        while (lineReader.HasMore)
        {
            var lineStart = lineReader.Position;
            var line = lineReader.ReadLine();
            var trimmedLine = line.TrimStart();

            // Check if line is blank
            if (trimmedLine.IsEmpty)
            {
                // Close any open paragraph
                if (paragraphStartLine >= 0)
                {
                    var paragraph = Block.CreateParagraph(paragraphStartLine, 0);
                    paragraph.ContentStart = paragraphStartOffset;
                    paragraph.ContentEnd = paragraphEndOffset;
                    blocks.Add(paragraph);
                    paragraphStartLine = -1;
                }

                // Add blank line block
                var blankLine = Block.CreateBlankLine(currentLine, 0);
                blankLine.ContentStart = lineStart;
                blankLine.ContentEnd = lineReader.Position;
                blocks.Add(blankLine);
            }
            else
            {
                // Non-blank line - part of a paragraph
                if (paragraphStartLine < 0)
                {
                    // Start new paragraph
                    paragraphStartLine = currentLine;
                    paragraphStartOffset = lineStart;
                }

                // Update end offset to include this line
                paragraphEndOffset = lineReader.Position;
            }

            currentLine++;
        }

        // Close any remaining open paragraph at end of document
        if (paragraphStartLine >= 0)
        {
            var paragraph = Block.CreateParagraph(paragraphStartLine, 0);
            paragraph.ContentStart = paragraphStartOffset;
            paragraph.ContentEnd = paragraphEndOffset;
            blocks.Add(paragraph);
        }

        // Copy to array since RefMarkdownDocument needs to outlive this method
        var blockArray = blocks.AsReadOnlySpan().ToArray();
        return new RefMarkdownDocument(source, blockArray, blockArray.Length, currentLine);
    }
}
