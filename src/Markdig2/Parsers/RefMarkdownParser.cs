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

        // Count lines for the document
        int lineCount = 0;
        var lineReader = new RefLineReader(source);
        while (lineReader.HasMore)
        {
            lineReader.ReadLine();
            lineCount++;
        }

        return new RefMarkdownDocument(source, blockArray, blockArray.Length, lineCount);
    }
}
