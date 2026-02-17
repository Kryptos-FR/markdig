// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Renderers;

/// <summary>
/// Renders a markdown document to HTML output.
/// Implements the MarkdownRenderer visitor pattern for HTML generation.
/// </summary>
public class HtmlRenderer : MarkdownRenderer
{
    /// <summary>
    /// Initializes a new instance of <see cref="HtmlRenderer"/>.
    /// </summary>
    /// <param name="writer">The text writer for HTML output.</param>
    public HtmlRenderer(TextWriter writer) : base(writer)
    {
    }

    protected override void RenderParagraph(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        Writer.Write("<p>");

        if (block.ContentStart < block.ContentEnd)
        {
            var content = source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart);
            EscapeHtml(content);
        }

        Writer.WriteLine("</p>");
    }

    protected override void RenderHeading(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        var level = block.HeadingLevel;
        Writer.Write($"<h{level}>");

        if (block.ContentStart < block.ContentEnd)
        {
            var content = source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart);
            EscapeHtml(content);
        }

        Writer.WriteLine($"</h{level}>");
    }

    protected override void RenderCodeBlock(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        Writer.WriteLine("<pre><code>");

        if (block.ContentStart < block.ContentEnd)
        {
            var content = source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart);
            EscapeHtml(content);
        }

        Writer.WriteLine("</code></pre>");
    }

    protected override void RenderQuote(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        Writer.WriteLine("<blockquote>");
        RenderChildren(source, ref block, allBlocks);
        Writer.WriteLine("</blockquote>");
    }

    protected override void RenderList(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        var tag = block.IsOrderedList ? "ol" : "ul";
        Writer.WriteLine($"<{tag}>");
        RenderChildren(source, ref block, allBlocks);
        Writer.WriteLine($"</{tag}>");
    }

    protected override void RenderListItem(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        Writer.Write("<li>");
        RenderChildren(source, ref block, allBlocks);
        Writer.WriteLine("</li>");
    }

    protected override void RenderThematicBreak(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        Writer.WriteLine("<hr />");
    }

    protected override void RenderHtmlBlock(ReadOnlySpan<char> source, ref Block block, Span<Block> allBlocks)
    {
        if (block.ContentStart < block.ContentEnd)
        {
            Writer.WriteLine(source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart));
        }
    }

    protected override void RenderLiteral(ReadOnlySpan<char> source, ref Inline inline)
    {
        if (inline.ContentStart < inline.ContentEnd)
        {
            var content = source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart);
            EscapeHtml(content);
        }
    }

    protected override void RenderCode(ReadOnlySpan<char> source, ref Inline inline)
    {
        Writer.Write("<code>");
        if (inline.ContentStart < inline.ContentEnd)
        {
            var content = source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart);
            EscapeHtml(content);
        }
        Writer.Write("</code>");
    }

    protected override void RenderEmphasis(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines)
    {
        Writer.Write("<em>");
        RenderInlineChildren(source, ref inline, allInlines);
        Writer.Write("</em>");
    }

    protected override void RenderStrong(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines)
    {
        Writer.Write("<strong>");
        RenderInlineChildren(source, ref inline, allInlines);
        Writer.Write("</strong>");
    }

    protected override void RenderLink(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines)
    {
        Writer.Write("<a href=\"");
        if (inline.LinkUrlStart < inline.LinkUrlEnd)
        {
            var url = source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart);
            EscapeHtmlAttribute(url);
        }

        // Add title attribute if present
        if (inline.LinkTitleStart > 0 && inline.LinkTitleEnd > inline.LinkTitleStart)
        {
            Writer.Write("\" title=\"");
            var title = source.Slice(inline.LinkTitleStart, inline.LinkTitleEnd - inline.LinkTitleStart);
            EscapeHtmlAttribute(title);
        }

        Writer.Write("\">");
        RenderInlineChildren(source, ref inline, allInlines);
        Writer.Write("</a>");
    }

    protected override void RenderImage(ReadOnlySpan<char> source, ref Inline inline, Span<Inline> allInlines)
    {
        Writer.Write("<img src=\"");
        if (inline.LinkUrlStart < inline.LinkUrlEnd)
        {
            var url = source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart);
            EscapeHtmlAttribute(url);
        }

        Writer.Write("\" alt=\"");
        // Render alt text - get content without tags
        var altText = GetAltText(source, ref inline, allInlines);
        EscapeHtmlAttribute(altText.AsSpan());

        // Add title attribute if present
        if (inline.LinkTitleStart > 0 && inline.LinkTitleEnd > inline.LinkTitleStart)
        {
            Writer.Write("\" title=\"");
            var title = source.Slice(inline.LinkTitleStart, inline.LinkTitleEnd - inline.LinkTitleStart);
            EscapeHtmlAttribute(title);
        }

        Writer.Write("\" />");
    }

    protected override void RenderSoftLineBreak(ReadOnlySpan<char> source, ref Inline inline)
    {
        Writer.Write(' ');
    }

    protected override void RenderHardLineBreak(ReadOnlySpan<char> source, ref Inline inline)
    {
        Writer.WriteLine("<br />");
    }

    protected override void RenderHtmlInline(ReadOnlySpan<char> source, ref Inline inline)
    {
        if (inline.ContentStart < inline.ContentEnd)
        {
            Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
        }
    }

    protected override void RenderAutoLink(ReadOnlySpan<char> source, ref Inline inline)
    {
        // AutoLink stores URL in both LinkUrlStart/LinkUrlEnd
        Writer.Write("<a href=\"");
        if (inline.LinkUrlStart < inline.LinkUrlEnd)
        {
            var url = source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart);
            EscapeHtmlAttribute(url);
        }
        Writer.Write("\">");
        if (inline.LinkUrlStart < inline.LinkUrlEnd)
        {
            var url = source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart);
            EscapeHtml(url);
        }
        Writer.Write("</a>");
    }

    /// <summary>
    /// Escapes HTML special characters in text content.
    /// </summary>
    private void EscapeHtml(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '&':
                    Writer.Write("&amp;");
                    break;
                case '<':
                    Writer.Write("&lt;");
                    break;
                case '>':
                    Writer.Write("&gt;");
                    break;
                case '"':
                    Writer.Write("&quot;");
                    break;
                default:
                    Writer.Write(text[i]);
                    break;
            }
        }
    }

    /// <summary>
    /// Escapes HTML special characters in attribute values.
    /// More conservative than EscapeHtml to handle attribute context.
    /// </summary>
    private void EscapeHtmlAttribute(ReadOnlySpan<char> text)
    {
        for (int i = 0; i < text.Length; i++)
        {
            switch (text[i])
            {
                case '&':
                    Writer.Write("&amp;");
                    break;
                case '<':
                    Writer.Write("&lt;");
                    break;
                case '>':
                    Writer.Write("&gt;");
                    break;
                case '"':
                    Writer.Write("&quot;");
                    break;
                case '\'':
                    Writer.Write("&#39;");
                    break;
                default:
                    Writer.Write(text[i]);
                    break;
            }
        }
    }

    /// <summary>
    /// Gets the plain text alt text for an image by extracting text from children.
    /// </summary>
    private string GetAltText(ReadOnlySpan<char> source, ref Inline image, Span<Inline> allInlines)
    {
        if (image.ChildCount == 0)
            return "";

        // Collect text from children (recursively for nested inlines)
        var childSpan = allInlines.Slice(image.FirstChildIndex, image.ChildCount);
        var sb = new System.Text.StringBuilder();

        foreach (var child in childSpan)
        {
            CollectTextFromInline(source, child, allInlines, sb);
        }

        return sb.ToString();
    }

    /// <summary>
    /// Recursively collects text from an inline element and its children.
    /// </summary>
    private void CollectTextFromInline(ReadOnlySpan<char> source, Inline inline, Span<Inline> allInlines, System.Text.StringBuilder sb)
    {
        switch (inline.Type)
        {
            case InlineType.Literal:
                if (inline.ContentStart < inline.ContentEnd)
                {
                    sb.Append(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
                }
                break;

            case InlineType.Code:
                if (inline.ContentStart < inline.ContentEnd)
                {
                    sb.Append(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
                }
                break;

            case InlineType.Emphasis:
            case InlineType.Strong:
                // Recursively get text from children
                if (inline.ChildCount > 0)
                {
                    var children = allInlines.Slice(inline.FirstChildIndex, inline.ChildCount);
                    foreach (var child in children)
                    {
                        CollectTextFromInline(source, child, allInlines, sb);
                    }
                }
                break;

            case InlineType.Link:
                // Use link text (children) not URL
                if (inline.ChildCount > 0)
                {
                    var children = allInlines.Slice(inline.FirstChildIndex, inline.ChildCount);
                    foreach (var child in children)
                    {
                        CollectTextFromInline(source, child, allInlines, sb);
                    }
                }
                break;

            case InlineType.HardLineBreak:
                sb.Append('\n');
                break;

            case InlineType.SoftLineBreak:
                sb.Append(' ');
                break;

                // Skip others (Images, HTML, etc.)
        }
    }
}
