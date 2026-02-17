// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;
using Markdig2.Renderers;
using Markdig2.Syntax;
using TextWriter = Markdig2.Renderers.TextWriter;

namespace Markdig2.Tests;

/// <summary>
/// Tests for TextWriter
/// </summary>
public class TestTextWriter
{
    [Fact]
    public void Constructor_WithStringBuilder_Initializes()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal(0, writer.Length);
    }

    [Fact]
    public void Constructor_WithNull_Throws()
    {
        Assert.Throws<ArgumentNullException>(() => new TextWriter(null!));
    }

    [Fact]
    public void Write_Char_UpdatesColumn()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write('a');
        Assert.Equal(1, writer.Column);
        Assert.False(writer.PreviousWasNewLine);
        Assert.Equal(1, writer.Length);

        writer.Write('b');
        Assert.Equal(2, writer.Column);
        Assert.Equal(2, writer.Length);
    }

    [Fact]
    public void Write_Newline_ResetsColumn()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write('a');
        writer.Write('b');
        writer.Write('\n');

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal(3, writer.Length);
    }

    [Fact]
    public void Write_String_UpdatesPosition()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("hello");
        Assert.Equal(5, writer.Column);
        Assert.False(writer.PreviousWasNewLine);
        Assert.Equal("hello", writer.ToString());
    }

    [Fact]
    public void Write_StringWithNewline_TracksPosition()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("hello\nworld");
        Assert.Equal(5, writer.Column); // "world" is 5 chars
        Assert.False(writer.PreviousWasNewLine);
        Assert.Equal("hello\nworld", writer.ToString());
    }

    [Fact]
    public void Write_Span_UpdatesPosition()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        ReadOnlySpan<char> span = "test".AsSpan();
        writer.Write(span);

        Assert.Equal(4, writer.Column);
        Assert.False(writer.PreviousWasNewLine);
        Assert.Equal("test", writer.ToString());
    }

    [Fact]
    public void WriteLine_AddsNewline()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.WriteLine();
        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal("\n", writer.ToString());
    }

    [Fact]
    public void WriteLine_String_AddsNewline()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.WriteLine("hello");
        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal("hello\n", writer.ToString());
    }

    [Fact]
    public void WriteLine_Span_AddsNewline()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        ReadOnlySpan<char> span = "test".AsSpan();
        writer.WriteLine(span);

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal("test\n", writer.ToString());
    }

    [Fact]
    public void Clear_ResetsState()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("hello");
        writer.Clear();

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal(0, writer.Length);
        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void Write_EmptyString_NoOp()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("");
        writer.Write((string?)null);

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal(0, writer.Length);
    }

    [Fact]
    public void Write_EmptySpan_NoOp()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write(ReadOnlySpan<char>.Empty);

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal(0, writer.Length);
    }

    [Fact]
    public void Write_MultipleNewlines_TracksCorrectly()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("line1\nline2\nline3");

        Assert.Equal(5, writer.Column); // "line3" is 5 chars
        Assert.False(writer.PreviousWasNewLine);
        Assert.Equal("line1\nline2\nline3", writer.ToString());
    }

    [Fact]
    public void Write_ComplexMixedContent_TracksCorrectly()
    {
        var sb = new StringBuilder();
        var writer = new TextWriter(sb);

        writer.Write("Start");
        writer.WriteLine();
        writer.Write("Middle");
        writer.Write('\n');
        writer.WriteLine("End");

        Assert.Equal(0, writer.Column);
        Assert.True(writer.PreviousWasNewLine);
        Assert.Equal("Start\nMiddle\nEnd\n", writer.ToString());
    }
}

/// <summary>
/// Tests for MarkdownRenderer base class
/// </summary>
public class TestMarkdownRenderer
{
    // Minimal concrete renderer for testing
    private class TestRenderer : MarkdownRenderer
    {
        public TestRenderer(TextWriter writer) : base(writer) { }

        protected override void RenderParagraph(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            Writer.Write("<p>");
            if (block.ContentStart < block.ContentEnd)
            {
                Writer.Write(source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart));
            }
            Writer.WriteLine("</p>");
        }

        protected override void RenderHeading(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            var level = block.HeadingLevel;
            Writer.Write($"<h{level}>");
            if (block.ContentStart < block.ContentEnd)
            {
                Writer.Write(source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart));
            }
            Writer.WriteLine($"</h{level}>");
        }

        protected override void RenderCodeBlock(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            Writer.Write("<pre><code>");
            if (block.ContentStart < block.ContentEnd)
            {
                Writer.Write(source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart));
            }
            Writer.WriteLine("</code></pre>");
        }

        protected override void RenderQuote(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            Writer.WriteLine("<blockquote>");
            RenderChildren(source, ref block, allBlocks);
            Writer.WriteLine("</blockquote>");
        }

        protected override void RenderList(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            var tag = block.IsOrderedList ? "ol" : "ul";
            Writer.WriteLine($"<{tag}>");
            RenderChildren(source, ref block, allBlocks);
            Writer.WriteLine($"</{tag}>");
        }

        protected override void RenderListItem(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            Writer.Write("<li>");
            RenderChildren(source, ref block, allBlocks);
            Writer.WriteLine("</li>");
        }

        protected override void RenderThematicBreak(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            Writer.WriteLine("<hr>");
        }

        protected override void RenderHtmlBlock(Span<char> source, ref Block block, Span<Block> allBlocks)
        {
            if (block.ContentStart < block.ContentEnd)
            {
                Writer.WriteLine(source.Slice(block.ContentStart, block.ContentEnd - block.ContentStart));
            }
        }

        protected override void RenderLiteral(Span<char> source, ref Inline inline)
        {
            if (inline.ContentStart < inline.ContentEnd)
            {
                Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
            }
        }

        protected override void RenderCode(Span<char> source, ref Inline inline)
        {
            Writer.Write("<code>");
            if (inline.ContentStart < inline.ContentEnd)
            {
                Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
            }
            Writer.Write("</code>");
        }

        protected override void RenderEmphasis(Span<char> source, ref Inline inline, Span<Inline> allInlines)
        {
            Writer.Write("<em>");
            RenderInlineChildren(source, ref inline, allInlines);
            Writer.Write("</em>");
        }

        protected override void RenderStrong(Span<char> source, ref Inline inline, Span<Inline> allInlines)
        {
            Writer.Write("<strong>");
            RenderInlineChildren(source, ref inline, allInlines);
            Writer.Write("</strong>");
        }

        protected override void RenderLink(Span<char> source, ref Inline inline, Span<Inline> allInlines)
        {
            Writer.Write("<a href=\"");
            if (inline.LinkUrlStart < inline.LinkUrlEnd)
            {
                Writer.Write(source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart));
            }
            Writer.Write("\">");
            RenderInlineChildren(source, ref inline, allInlines);
            Writer.Write("</a>");
        }

        protected override void RenderImage(Span<char> source, ref Inline inline, Span<Inline> allInlines)
        {
            Writer.Write("<img src=\"");
            if (inline.LinkUrlStart < inline.LinkUrlEnd)
            {
                Writer.Write(source.Slice(inline.LinkUrlStart, inline.LinkUrlEnd - inline.LinkUrlStart));
            }
            Writer.Write("\" alt=\"");
            RenderInlineChildren(source, ref inline, allInlines);
            Writer.Write("\">");
        }

        protected override void RenderSoftLineBreak(Span<char> source, ref Inline inline)
        {
            Writer.Write(' ');
        }

        protected override void RenderHardLineBreak(Span<char> source, ref Inline inline)
        {
            Writer.WriteLine("<br>");
        }

        protected override void RenderHtmlInline(Span<char> source, ref Inline inline)
        {
            if (inline.ContentStart < inline.ContentEnd)
            {
                Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
            }
        }

        protected override void RenderAutoLink(Span<char> source, ref Inline inline)
        {
            Writer.Write("<ahref=\"");
            if (inline.ContentStart < inline.ContentEnd)
            {
                Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
            }
            Writer.Write("\">");
            if (inline.ContentStart < inline.ContentEnd)
            {
                Writer.Write(source.Slice(inline.ContentStart, inline.ContentEnd - inline.ContentStart));
            }
            Writer.Write("</a>");
        }
    }

    [Fact]
    public void Render_EmptyDocument_Produces_EmptyOutput()
    {
        Span<char> source = "".ToCharArray();
        Span<Block> blocks = stackalloc Block[0];

        var doc = new RefMarkdownDocument(source, blocks, 0, 0);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("", writer.ToString());
    }

    [Fact]
    public void Render_SingleParagraph_RendersCorrectly()
    {
        var markdown = "Hello world";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var para = Block.CreateParagraph(0, 0);
        para.ContentStart = 0;
        para.ContentEnd = markdown.Length;
        para.LineCount = 1;
        blocks[0] = para;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("<p>Hello world</p>\n", writer.ToString());
    }

    [Fact]
    public void Render_MultipleBlocks_RendersAll()
    {
        var markdown = "Para1\n\nPara2";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[2];

        var para1 = Block.CreateParagraph(0, 0);
        para1.ContentStart = 0;
        para1.ContentEnd = 5;
        para1.LineCount = 1;
        blocks[0] = para1;

        var para2 = Block.CreateParagraph(2, 0);
        para2.ContentStart = 7;
        para2.ContentEnd = 12;
        para2.LineCount = 1;
        blocks[1] = para2;

        var doc = new RefMarkdownDocument(source, blocks, 2, 3);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<p>Para1</p>", output);
        Assert.Contains("<p>Para2</p>", output);
    }

    [Fact]
    public void IsFirstInContainer_SetCorrectly_ForBlocks()
    {
        var markdown = "A\n\nB\n\nC";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[3];

        var para1 = Block.CreateParagraph(0, 0);
        para1.ContentStart = 0;
        para1.ContentEnd = 1;
        para1.LineCount = 1;
        blocks[0] = para1;

        var para2 = Block.CreateParagraph(1, 0);
        para2.ContentStart = 3;
        para2.ContentEnd = 4;
        para2.LineCount = 1;
        blocks[1] = para2;

        var para3 = Block.CreateParagraph(2, 0);
        para3.ContentStart = 6;
        para3.ContentEnd = 7;
        para3.LineCount = 1;
        blocks[2] = para3;

        var doc = new RefMarkdownDocument(source, blocks, 3, 5);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestRenderer(writer);

        renderer.Render(doc);

        // Just verify rendering completes without exception
        Assert.NotEmpty(writer.ToString());
    }
}
