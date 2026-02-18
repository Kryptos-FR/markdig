// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Syntax;

/// <summary>
/// Represents a parsed markdown document as a ref struct.
/// This is a stack-only type tied to the lifetime of the source span.
/// Blocks are stored in a flat array with parent-child relationships tracked via indices.
/// Inlines are also stored in a flat array with parent-child relationships tracked via indices.
/// </summary>
public ref struct RefMarkdownDocument
{
    /// <summary>
    /// Initializes a new instance of <see cref="RefMarkdownDocument"/>.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <param name="allBlocks">All parsed blocks in a flat span.</param>
    /// <param name="topLevelCount">The number of top-level blocks.</param>
    /// <param name="allInlines">All parsed inline elements in a flat span.</param>
    /// <param name="lineCount">The total number of lines in the source.</param>
    public RefMarkdownDocument(ReadOnlySpan<char> source, Span<Block> allBlocks, int topLevelCount, Span<Inline> allInlines, int lineCount)
    {
        Source = source;
        AllBlocks = allBlocks;
        TopLevelBlockCount = topLevelCount;
        AllInlines = allInlines;
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
    /// Gets all inline elements in a flat span.
    /// Inlines are grouped by the blocks they belong to, starting after all blocks.
    /// </summary>
    public Span<Inline> AllInlines { get; }

    /// <summary>
    /// Gets the total number of lines in the source document.
    /// </summary>
    public int LineCount { get; }

    /// <summary>
    /// Gets the total number of blocks (including nested).
    /// </summary>
    public readonly int TotalBlockCount => AllBlocks.Length;

    /// <summary>
    /// Gets the total number of inline elements.
    /// </summary>
    public readonly int TotalInlineCount => AllInlines.Length;

    /// <summary>
    /// Gets a value indicating whether this document has any blocks.
    /// </summary>
    public readonly bool HasBlocks => AllBlocks.Length > 0;

    /// <summary>
    /// Gets a value indicating whether this document has any inlines.
    /// </summary>
    public readonly bool HasInlines => AllInlines.Length > 0;

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
    /// Gets the inline elements of a leaf block.
    /// </summary>
    /// <param name="block">The leaf block.</param>
    /// <returns>A span of inline elements, or empty if no inlines.</returns>
    public readonly Span<Inline> GetInlines(ref Block block)
    {
        if (!block.IsLeafBlock || block.InlineCount == 0)
        {
            return [];
        }

        return AllInlines.Slice(block.FirstInlineIndex, block.InlineCount);
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
        return $"Document(TopLevel={TopLevelBlockCount}, Total={TotalBlockCount}, Inlines={TotalInlineCount}, Lines={LineCount})";
    }
}
