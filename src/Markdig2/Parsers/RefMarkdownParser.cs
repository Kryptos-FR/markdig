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
    public static RefMarkdownDocument Parse(ReadOnlySpan<char> source)
    {
        // Use stack allocation for block collections (8KB for blocks, 1KB for container stack, 8KB for line boundaries)
        Span<Block> blockBuffer = stackalloc Block[256];
        Span<int> containerBuffer = stackalloc int[32];
        Span<int> lineBoundaryBuffer = stackalloc int[1024]; // 512 lines * 2 boundaries each

        // Create processor and parse blocks
        var processor = new RefBlockProcessor(source, blockBuffer, containerBuffer, lineBoundaryBuffer);
        var blockSpan = processor.ProcessBlocks();

        // Copy to array since RefMarkdownDocument needs to outlive this method
        var blockArray = blockSpan.ToArray();

        // Parse inlines for each leaf block
        Span<Inline> inlineBuffer = stackalloc Inline[1024];
        var inlineArray = ParseInlines(source, blockArray, inlineBuffer);

        // Count lines for the document
        int lineCount = 0;
        var lineReader = new RefLineReader(source);
        while (lineReader.HasMore)
        {
            lineReader.ReadLine();
            lineCount++;
        }

        return new RefMarkdownDocument(source, blockArray, blockArray.Length, inlineArray, lineCount);
    }

    /// <summary>
    /// Parses inline elements for all leaf blocks in the document.
    /// Updates each block's FirstInlineIndex and InlineCount fields.
    /// </summary>
    private static Inline[] ParseInlines(ReadOnlySpan<char> source, Block[] blocks, Span<Inline> inlineBuffer)
    {
        if (blocks.Length == 0)
            return [];

        // Use separate buffer for inline processor (temp processing)
        Span<Inline> processingBuffer = stackalloc Inline[512];
        Span<RefInlineProcessor.DelimiterRun> delimiterBuffer = stackalloc RefInlineProcessor.DelimiterRun[512];

        // Use the provided inlineBuffer for accumulating all inlines
        var allInlines = new RefCollection<Inline>(inlineBuffer);

        // Parse inlines for each leaf block
        for (int i = 0; i < blocks.Length; i++)
        {
            if (blocks[i].IsLeafBlock && blocks[i].ContentStart < blocks[i].ContentEnd)
            {
                // Create processor with externally allocated buffers
                var inlineProcessor = new RefInlineProcessor(source, processingBuffer, delimiterBuffer);

                // Parse inlines for this block
                var blockInlines = inlineProcessor.ProcessInlines(blocks[i].ContentStart, blocks[i].ContentEnd);

                // Store the inlines
                int firstInlineIndex = allInlines.Length;
                foreach (var inline in blockInlines)
                {
                    allInlines.Add(inline);
                }

                // Update the block with inline indices
                blocks[i].FirstInlineIndex = firstInlineIndex;
                blocks[i].InlineCount = blockInlines.Length;
            }
        }

        return allInlines.AsReadOnlySpan().ToArray();
    }
}
