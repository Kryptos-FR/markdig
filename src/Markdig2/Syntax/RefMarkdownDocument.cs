// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Syntax;

/// <summary>
/// Represents a parsed markdown document as a ref struct.
/// This is a stack-only type tied to the lifetime of the source span.
/// Blocks are stored in a flat array with parent-child relationships tracked via indices.
/// </summary>
public ref struct RefMarkdownDocument
{
    /// <summary>
    /// Initializes a new instance of <see cref="RefMarkdownDocument"/>.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <param name="allBlocks">All parsed blocks in a flat span.</param>
    /// <param name="topLevelCount">The number of top-level blocks.</param>
    /// <param name="lineCount">The total number of lines in the source.</param>
    public RefMarkdownDocument(ReadOnlySpan<char> source, Span<Block> allBlocks, int topLevelCount, int lineCount)
    {
        Source = source;
        AllBlocks = allBlocks;
        TopLevelBlockCount = topLevelCount;
        LineCount = lineCount;
    }

    /// <summary>
    /// Gets the source markdown text.
    /// </summary>
    public ReadOnlySpan<char> Source { get; }

    /// <summary>
    /// Gets all blocks (including nested children) in a flat span.
    /// Top-level blocks are first, followed by their descendants.
    /// </summary>
    public Span<Block> AllBlocks { get; }

    /// <summary>
    /// Gets the number of top-level blocks.
    /// </summary>
    public int TopLevelBlockCount { get; }

    /// <summary>
    /// Gets the total number of lines in the source document.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Gets the total number of blocks (including nested).
    /// </summary>
    public readonly int TotalBlockCount => AllBlocks.Length;

    /// <summary>
    /// Gets a value indicating whether this document has any blocks.
    /// </summary>
    public readonly bool HasBlocks => AllBlocks.Length > 0;

    /// <summary>
    /// Gets a span of top-level blocks.
    /// </summary>
    public readonly Span<Block> GetTopLevelBlocks()
    {
        return AllBlocks[..TopLevelBlockCount];
    }

    /// <summary>
    /// Gets the children of a container block.
    /// </summary>
    /// <param name="block">The container block.</param>
    /// <returns>A span of child blocks, or empty if no children.</returns>
    public readonly Span<Block> GetChildren(ref Block block)
    {
        if (!block.IsContainerBlock || block.ChildCount == 0)
        {
            return [];
        }

        return AllBlocks.Slice(block.FirstChildIndex, block.ChildCount);
    }

    /// <summary>
    /// Gets a view of the entire source as a RefStringView.
    /// </summary>
    public readonly RefStringView GetSourceView()
    {
        return new RefStringView(Source);
    }

    /// <summary>
    /// Returns a string representation for debugging.
    /// </summary>
    public override readonly string ToString()
    {
        return $"Document(TopLevel={TopLevelBlockCount}, Total={TotalBlockCount}, Lines={LineCount})";
    }
}
