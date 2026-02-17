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
/// <summary>
/// Tests for HtmlRenderer
/// </summary>
public class TestHtmlRenderer
{
    // Test renderer that exposes protected methods for testing
    private class TestableHtmlRenderer : HtmlRenderer
    {
        public TestableHtmlRenderer(TextWriter writer) : base(writer) { }

        public void TestRenderInlines(Span<char> source, Span<Inline> inlines)
        {
            RenderInlines(source, inlines);
        }
    }

    [Fact]
    public void RenderParagraph_WithString_EscapesHtmlSpecialChars()
    {
        var markdown = "Hello & <world>";
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
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("&amp;", output);
        Assert.Contains("&lt;", output);
        Assert.Contains("&gt;", output);
        Assert.Equal("<p>Hello &amp; &lt;world&gt;</p>\n", output);
    }

    [Fact]
    public void RenderParagraph_Empty_RendersEmptyParagraph()
    {
        var markdown = "";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var para = Block.CreateParagraph(0, 0);
        para.ContentStart = 0;
        para.ContentEnd = 0;
        para.LineCount = 0;
        blocks[0] = para;

        var doc = new RefMarkdownDocument(source, blocks, 1, 0);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("<p></p>\n", writer.ToString());
    }

    [Fact]
    public void RenderHeading_Level1_RendersCorrectly()
    {
        var markdown = "Heading 1";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var heading = Block.CreateHeading(0, 0, 1);
        heading.ContentStart = 0;
        heading.ContentEnd = markdown.Length;
        heading.LineCount = 1;
        blocks[0] = heading;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("<h1>Heading 1</h1>\n", writer.ToString());
    }

    [Fact]
    public void RenderHeading_Level6_RendersCorrectly()
    {
        var markdown = "Small";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var heading = Block.CreateHeading(0, 0, 6);
        heading.ContentStart = 0;
        heading.ContentEnd = markdown.Length;
        heading.LineCount = 1;
        blocks[0] = heading;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("<h6>Small</h6>\n", writer.ToString());
    }

    [Fact]
    public void RenderCodeBlock_RendersWithPreAndCode()
    {
        var markdown = "int x = 5;";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var codeBlock = Block.CreateCodeBlock(0, 0, isFenced: true, fenceChar: '`');
        codeBlock.ContentStart = 0;
        codeBlock.ContentEnd = markdown.Length;
        codeBlock.LineCount = 1;
        blocks[0] = codeBlock;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<pre><code>", output);
        Assert.Contains("</code></pre>", output);
        Assert.Contains("int x = 5;", output);
    }

    [Fact]
    public void RenderCodeBlock_EscapesHtmlInCode()
    {
        var markdown = "<script>alert('xss')</script>";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var codeBlock = Block.CreateCodeBlock(0, 0, isFenced: true, fenceChar: '`');
        codeBlock.ContentStart = 0;
        codeBlock.ContentEnd = markdown.Length;
        codeBlock.LineCount = 1;
        blocks[0] = codeBlock;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("&lt;script&gt;", output);
        Assert.DoesNotContain("<script>", output);
    }

    [Fact]
    public void RenderThematicBreak_RendersCorrectly()
    {
        var markdown = "---";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var thematicBreak = Block.CreateThematicBreak(0, 0, '-');
        blocks[0] = thematicBreak;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        Assert.Equal("<hr />\n", writer.ToString());
    }

    [Fact]
    public void RenderQuote_WithChildren_RendersBlockquote()
    {
        // Create a quote with one child paragraph
        var markdown = "Quote text";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[2];

        // Quote block
        var quote = Block.CreateQuote(0, 0);
        quote.FirstChildIndex = 1;
        quote.ChildCount = 1;
        blocks[0] = quote;

        // Child paragraph
        var para = Block.CreateParagraph(0, 0);
        para.ContentStart = 0;
        para.ContentEnd = markdown.Length;
        para.LineCount = 1;
        blocks[1] = para;

        var doc = new RefMarkdownDocument(source, blocks, 1, 2);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<blockquote>", output);
        Assert.Contains("</blockquote>", output);
        Assert.Contains("<p>Quote text</p>", output);
    }

    [Fact]
    public void RenderList_Unordered_RendersWithUl()
    {
        // Create an unordered list with one item
        var markdown = "item";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[2];

        // List block
        var list = Block.CreateList(0, 0, isOrdered: false, bulletChar: '-');
        list.FirstChildIndex = 1;
        list.ChildCount = 1;
        blocks[0] = list;

        // List item
        var item = Block.CreateListItem(0, 0);
        item.FirstChildIndex = -1;  // No children for now
        item.ChildCount = 0;
        blocks[1] = item;

        var doc = new RefMarkdownDocument(source, blocks, 1, 2);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<ul>", output);
        Assert.Contains("</ul>", output);
        Assert.Contains("<li>", output);
        Assert.Contains("</li>", output);
    }

    [Fact]
    public void RenderList_Ordered_RendersWithOl()
    {
        // Create an ordered list
        var markdown = "item";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[2];

        // Ordered list block
        var list = Block.CreateList(0, 0, isOrdered: true, startNumber: 1);
        list.FirstChildIndex = 1;
        list.ChildCount = 1;
        blocks[0] = list;

        // List item
        var item = Block.CreateListItem(0, 0);
        item.FirstChildIndex = -1;
        item.ChildCount = 0;
        blocks[1] = item;

        var doc = new RefMarkdownDocument(source, blocks, 1, 2);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<ol>", output);
        Assert.Contains("</ol>", output);
    }

    [Fact]
    public void RenderHtmlBlock_PassesThroughRawHtml()
    {
        var htmlContent = "<div>Custom HTML</div>";
        Span<char> source = htmlContent.ToCharArray();

        Span<Block> blocks = stackalloc Block[1];
        var htmlBlock = Block.CreateHtmlBlock(0, 0);
        htmlBlock.ContentStart = 0;
        htmlBlock.ContentEnd = htmlContent.Length;
        blocks[0] = htmlBlock;

        var doc = new RefMarkdownDocument(source, blocks, 1, 1);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<div>Custom HTML</div>", output);
    }

    [Fact]
    public void RenderLiteral_EscapesHtml()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        var source = "A & B < C > D".ToCharArray();
        inlines[0] = Inline.CreateLiteral(0, source.Length);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("&amp;", output);
        Assert.Contains("&lt;", output);
        Assert.Contains("&gt;", output);
    }

    [Fact]
    public void RenderCode_Inline_RendersWithCodeTag()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateCode(0, 4);

        var source = "code".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        Assert.Equal("<code>code</code>", writer.ToString());
    }

    [Fact]
    public void RenderCode_WithHtml_EscapesContent()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateCode(0, 5);

        var source = "<tag>".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Equal("<code>&lt;tag&gt;</code>", output);
    }

    [Fact]
    public void RenderEmphasis_WrapsWithEmTag()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateEmphasis('*', firstChildIndex: 0, childCount: 0);

        var source = "".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("<em>", output);
        Assert.Contains("</em>", output);
    }

    [Fact]
    public void RenderStrong_WrapsWithStrongTag()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateStrong('*', firstChildIndex: 0, childCount: 0);

        var source = "".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("<strong>", output);
        Assert.Contains("</strong>", output);
    }

    [Fact]
    public void RenderLink_RendersWithHrefAndText()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        const string text = "click me";
        const string url = "https://example.com";

        inlines[0] = Inline.CreateLink(
            textStart: 0, textEnd: text.Length,
            urlStart: text.Length + 1, urlEnd: text.Length + 1 + url.Length);

        var source = (text + " " + url).ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("<a href=\"https://example.com\">", output);
        Assert.Contains("</a>", output);
    }

    [Fact]
    public void RenderLink_WithTitle_IncludesTitleAttribute()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        const string text = "link";
        const string url = "http://example.com";
        const string title = "Example Site";

        var fullText = text + " " + url + " " + title;

        inlines[0] = Inline.CreateLink(
            textStart: 0, textEnd: text.Length,
            urlStart: text.Length + 1, urlEnd: text.Length + 1 + url.Length,
            titleStart: text.Length + 1 + url.Length + 1,
            titleEnd: fullText.Length);

        var source = fullText.ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("title=\"Example Site\"", output);
    }

    [Fact]
    public void RenderLink_EscapesUrlInAttribute()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        const string url = "http://ex.com?a=1&b=2";
        inlines[0] = Inline.CreateLink(
            textStart: 0, textEnd: 4,
            urlStart: 5, urlEnd: 5 + url.Length);

        var source = ("link " + url).ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("&amp;", output);
    }

    [Fact]
    public void RenderImage_RendersWithSrcAndAlt()
    {
        Span<Inline> inlines = stackalloc Inline[2];
        const string alt = "alt text";
        const string src = "image.png";

        // Create a literal inline for the alt text
        inlines[0] = Inline.CreateLiteral(0, alt.Length);

        // Create the image with children pointing to the literal
        inlines[1] = Inline.CreateImage(
            altStart: 0, altEnd: alt.Length,
            urlStart: alt.Length + 1, urlEnd: alt.Length + 1 + src.Length);
        inlines[1].FirstChildIndex = 0;
        inlines[1].ChildCount = 1;

        var source = (alt + " " + src).ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("<img src=\"image.png\"", output);
        Assert.Contains("alt=\"alt text\"", output);
        Assert.Contains("/>", output);
    }

    [Fact]
    public void RenderHardLineBreak_RendersWithBrTag()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateHardLineBreak();

        var source = "".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        Assert.Contains("<br />", writer.ToString());
    }

    [Fact]
    public void RenderSoftLineBreak_RendersAsSpace()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        inlines[0] = Inline.CreateSoftLineBreak();

        var source = "".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        Assert.Equal(" ", writer.ToString());
    }

    [Fact]
    public void RenderHtmlInline_PassesThroughRawHtml()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        const string html = "<span>raw</span>";

        inlines[0] = Inline.CreateHtmlInline(0, html.Length);

        var source = html.ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        Assert.Equal(html, writer.ToString());
    }

    [Fact]
    public void RenderAutoLink_RendersAsLink()
    {
        Span<Inline> inlines = stackalloc Inline[1];
        const string url = "https://example.com";

        inlines[0] = Inline.CreateAutoLink(0, url.Length);

        var source = url.ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("<a href=", output);
        Assert.Contains("https://example.com", output);
        Assert.Contains("</a>", output);
    }

    [Fact]
    public void MultipleBlocks_RendersAllCorrectly()
    {
        var markdown = "Heading\n\nParagraph\n\n---";
        Span<char> source = markdown.ToCharArray();

        Span<Block> blocks = stackalloc Block[3];

        // Heading
        var heading = Block.CreateHeading(0, 0, 1);
        heading.ContentStart = 0;
        heading.ContentEnd = 7;
        heading.LineCount = 1;
        blocks[0] = heading;

        // Paragraph
        var para = Block.CreateParagraph(2, 0);
        para.ContentStart = 9;
        para.ContentEnd = 18;
        para.LineCount = 1;
        blocks[1] = para;

        // Thematic break
        var thematicBreak = Block.CreateThematicBreak(4, 0, '-');
        blocks[2] = thematicBreak;

        var doc = new RefMarkdownDocument(source, blocks, 3, 5);

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.Render(doc);

        var output = writer.ToString();
        Assert.Contains("<h1>Heading</h1>", output);
        Assert.Contains("<p>Paragraph</p>", output);
        Assert.Contains("<hr />", output);
    }

    [Fact]
    public void QuotesInAttribute_GetEscaped()
    {
        Span<Inline> inlines = stackalloc Inline[1];

        inlines[0] = Inline.CreateImage(
            altStart: 0, altEnd: 3,
            urlStart: 4, urlEnd: 20);

        var source = "alt img.png?title=\"bad\"".ToCharArray();

        var sb = new StringBuilder();
        var writer = new TextWriter(sb);
        var renderer = new TestableHtmlRenderer(writer);

        renderer.TestRenderInlines(source, inlines);

        var output = writer.ToString();
        Assert.Contains("&quot;", output);
    }
}

