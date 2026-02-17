// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Syntax;

namespace Markdig2.Renderers;

/// <summary>
/// Base class for rendering markdown documents.
/// Provides visitor pattern for traversing ref struct AST.
/// </summary>
public abstract class MarkdownRenderer
{
    /// <summary>
    /// The text writer for output.
    /// </summary>
    protected TextWriter Writer;

    /// <summary>
    /// Initializes a new instance of <see cref="MarkdownRenderer"/>.
    /// </summary>
    /// <param name="writer">The text writer for output.</param>
    protected MarkdownRenderer(TextWriter writer)
    {
        Writer = writer;
    }

    /// <summary>
    /// Gets whether this is the first block/inline in a container.
    /// </summary>
    public bool IsFirstInContainer { get; private set; }

    /// <summary>
    /// Gets whether this is the last block/inline in a container.
    /// </summary>
    public bool IsLastInContainer { get; private set; }

    /// <summary>
    /// Renders a markdown document.
    /// </summary>
    /// <param name="document">The markdown document to render.</param>
    public void Render(RefMarkdownDocument document)
    {
        var blocks = document.AllBlocks[..document.TopLevelBlockCount];
        for (int i = 0; i < blocks.Length; i++)
        {
            IsFirstInContainer = i == 0;
            IsLastInContainer = i == blocks.Length - 1;
            RenderBlock(document.Source, ref blocks[i], document.AllBlocks);
        }
    }

    /// <summary>
    /// Renders a single block.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <param name="block">The block to render.</param>
    /// <param name="allBlocks">All blocks in the document (for navigating children).</param>
    protected void RenderBlock(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        switch (block.Type)
        {
            case BlockType.Paragraph:
                RenderParagraph(source, ref block, allBlocks);
                break;
            case BlockType.Heading:
                RenderHeading(source, ref block, allBlocks);
                break;
            case BlockType.CodeBlock:
                RenderCodeBlock(source, ref block, allBlocks);
                break;
            case BlockType.Quote:
                RenderQuote(source, ref block, allBlocks);
                break;
            case BlockType.List:
                RenderList(source, ref block, allBlocks);
                break;
            case BlockType.ListItem:
                RenderListItem(source, ref block, allBlocks);
                break;
            case BlockType.ThematicBreak:
                RenderThematicBreak(source, ref block, allBlocks);
                break;
            case BlockType.HtmlBlock:
                RenderHtmlBlock(source, ref block, allBlocks);
                break;
            case BlockType.BlankLine:
                // Blank lines are typically skipped in rendering
                break;
            default:
                throw new ArgumentException($"Unknown block type: {block.Type}");
        }
    }

    /// <summary>
    /// Renders child blocks of a container block.
    /// </summary>
    protected void RenderChildren(ReadOnlySpan<char> source, ref Block containerBlock, Span<Block> allBlocks)
    {
        if (containerBlock.ChildCount == 0)
            return;

        var children = allBlocks.Slice(containerBlock.FirstChildIndex, containerBlock.ChildCount);
        for (int i = 0; i < children.Length; i++)
        {
            IsFirstInContainer = i == 0;
            IsLastInContainer = i == children.Length - 1;
            RenderBlock(source, ref children[i], allBlocks);
        }
    }

    /// <summary>
    /// Renders inline elements within a block.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <param name="inlines">The inline elements to render.</param>
    protected void RenderInlines(ReadOnlySpan<char> source, Span<Inline> inlines)
    {
        for (int i = 0; i < inlines.Length; i++)
        {
            IsFirstInContainer = i == 0;
            IsLastInContainer = i == inlines.Length - 1;
            RenderInline(source, ref inlines[i], inlines);
        }
    }

    /// <summary>
    /// Renders a single inline element.
    /// </summary>
    protected void RenderInline(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines)
    {
        switch (inline.Type)
        {
            case InlineType.Literal:
                RenderLiteral(source, ref inline);
                break;
            case InlineType.Code:
                RenderCode(source, ref inline);
                break;
            case InlineType.Emphasis:
                RenderEmphasis(source, ref inline, allInlines);
                break;
            case InlineType.Strong:
                RenderStrong(source, ref inline, allInlines);
                break;
            case InlineType.Link:
                RenderLink(source, ref inline, allInlines);
                break;
            case InlineType.Image:
                RenderImage(source, ref inline, allInlines);
                break;
            case InlineType.SoftLineBreak:
                RenderSoftLineBreak(source, ref inline);
                break;
            case InlineType.HardLineBreak:
                RenderHardLineBreak(source, ref inline);
                break;
            case InlineType.HtmlInline:
                RenderHtmlInline(source, ref inline);
                break;
            case InlineType.AutoLink:
                RenderAutoLink(source, ref inline);
                break;
            default:
                throw new ArgumentException($"Unknown inline type: {inline.Type}");
        }
    }

    /// <summary>
    /// Renders child inlines of a container inline (like Emphasis).
    /// </summary>
    protected void RenderInlineChildren(ReadOnlySpan<char> source, ref Inline containerInline, Span<Inline> allInlines)
    {
        if (containerInline.ChildCount == 0)
            return;

        var children = allInlines.Slice(containerInline.FirstChildIndex, containerInline.ChildCount);
        RenderInlines(source, children);
    }

    // Abstract methods for rendering specific block types
    protected abstract void RenderParagraph(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderHeading(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderCodeBlock(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderQuote(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderList(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderListItem(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderThematicBreak(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);
    protected abstract void RenderHtmlBlock(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks);

    // Abstract methods for rendering specific inline types
    protected abstract void RenderLiteral(ReadOnlySpan<char> source, ref Inline inline);
    protected abstract void RenderCode(ReadOnlySpan<char> source, ref Inline inline);
    protected abstract void RenderEmphasis(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines);
    protected abstract void RenderStrong(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines);
    protected abstract void RenderLink(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines);
    protected abstract void RenderImage(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines);
    protected abstract void RenderSoftLineBreak(ReadOnlySpan<char> source, ref Inline inline);
    protected abstract void RenderHardLineBreak(ReadOnlySpan<char> source, ref Inline inline);
    protected abstract void RenderHtmlInline(ReadOnlySpan<char> source, ref Inline inline);
    protected abstract void RenderAutoLink(ReadOnlySpan<char> source, ref Inline inline);
}
