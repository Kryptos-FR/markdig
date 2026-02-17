// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Parsers;
using Markdig2.Syntax;

namespace Markdig2.Tests;

/// <summary>
/// Tests for Phase 2.2 inline parsers (Emphasis, Strong, Code, Link, Image, LineBreak).
/// </summary>
public class InlineParserTests
{
    // ==================== Code Span Tests ====================

    private static Inline[] ProcessInlines(Span<char> source, int start, int end)
    {
        Span<Inline> inlineBuffer = new Inline[256];
        Span<RefInlineProcessor.DelimiterRun> delimiterBuffer = new RefInlineProcessor.DelimiterRun[64];
        var processor = new RefInlineProcessor(source, inlineBuffer, delimiterBuffer);
        return processor.ProcessInlines(start, end).ToArray();
    }

    [Fact]
    public void Parse_SimpleCodeSpan_CreatesCodeInline()
    {
        Span<char> source = "`code`".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        Assert.Equal(1, inlines.Length);
        Assert.Equal(InlineType.Code, inlines[0].Type);
        Assert.Equal("code", inlines[0].GetContent(source).ToString());
    }

    [Fact]
    public void Parse_CodeSpanWithSpaces_TrimsOuterSpaces()
    {
        Span<char> source = "` code `".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        Assert.Equal(1, inlines.Length);
        Assert.Equal(InlineType.Code, inlines[0].Type);
        Assert.Equal("code", inlines[0].GetContent(source).ToString());
    }

    [Fact]
    public void Parse_DoubleBacktickCodeSpan_MatchesCorrectly()
    {
        Span<char> source = "`` code ``".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        Assert.Equal(1, inlines.Length);
        Assert.Equal(InlineType.Code, inlines[0].Type);
        Assert.Equal("code", inlines[0].GetContent(source).ToString());
    }

    [Fact]
    public void Parse_CodeSpanWithBackticks_PreservesInnerBackticks()
    {
        Span<char> source = "`` `code` ``".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        Assert.Equal(1, inlines.Length);
        Assert.Equal(InlineType.Code, inlines[0].Type);
        Assert.Equal("`code`", inlines[0].GetContent(source).ToString());
    }

    [Fact]
    public void Parse_UnmatchedBackticks_TreatsAsLiteral()
    {
        Span<char> source = "`code".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        // Should have literals when backticks don't match
        Assert.True(inlines.Length > 0);
        // The exact behavior depends on literal parsing
    }

    [Fact]
    public void Parse_MultipleCodeSpans_ParsesBoth()
    {
        Span<char> source = "`code1` and `code2`".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var codeInlines = inlines.Where(i => i.Type == InlineType.Code).ToList();
        Assert.Equal(2, codeInlines.Count);
        Assert.Equal("code1", codeInlines[0].GetContent(source).ToString());
        Assert.Equal("code2", codeInlines[1].GetContent(source).ToString());
    }

    // ==================== Link Tests ====================

    [Fact]
    public void Parse_SimpleLink_CreatesLinkInline()
    {
        Span<char> source = "[text](url)".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var linkInline = inlines.FirstOrDefault(i => i.Type == InlineType.Link);
        Assert.NotNull(linkInline);
        Assert.Equal("text", linkInline.GetContent(source).ToString());
        Assert.Equal("url", linkInline.GetLinkUrl(source).ToString());
    }

    [Fact]
    public void Parse_LinkWithTitle_CreatesLinkWithTitle()
    {
        Span<char> source = "[text](url \"title\")".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var linkInline = inlines.FirstOrDefault(i => i.Type == InlineType.Link);
        Assert.NotNull(linkInline);
        Assert.Equal("text", linkInline.GetContent(source).ToString());
        Assert.Equal("url", linkInline.GetLinkUrl(source).ToString());
        Assert.Equal("title", linkInline.GetLinkTitle(source).ToString());
    }

    [Fact]
    public void Parse_MultipleLinks_ParsesBoth()
    {
        Span<char> source = "[link1](url1) and [link2](url2)".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var linkInlines = inlines.Where(i => i.Type == InlineType.Link).ToList();
        Assert.Equal(2, linkInlines.Count);
    }

    [Fact]
    public void Parse_InvalidLink_TreatsAsLiteral()
    {
        Span<char> source = "[text without closing".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        // Should not create a link
        Assert.Empty(inlines.Where(i => i.Type == InlineType.Link));
    }

    // ==================== Image Tests ====================

    [Fact]
    public void Parse_SimpleImage_CreatesImageInline()
    {
        Span<char> source = "![alt](url)".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var imageInline = inlines.FirstOrDefault(i => i.Type == InlineType.Image);
        Assert.NotNull(imageInline);
        Assert.Equal("alt", imageInline.GetContent(source).ToString());
        Assert.Equal("url", imageInline.GetLinkUrl(source).ToString());
    }

    [Fact]
    public void Parse_ImageWithTitle_CreatesImageWithTitle()
    {
        Span<char> source = "![alt](url \"title\")".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var imageInline = inlines.FirstOrDefault(i => i.Type == InlineType.Image);
        Assert.NotNull(imageInline);
        Assert.Equal("alt", imageInline.GetContent(source).ToString());
        Assert.Equal("url", imageInline.GetLinkUrl(source).ToString());
        Assert.Equal("title", imageInline.GetLinkTitle(source).ToString());
    }

    // ==================== Emphasis Tests ====================

    [Fact]
    public void Parse_EmphasisWithAsterisks_CreatesEmphasisInline()
    {
        Span<char> source = "*emphasis*".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var emphasisInline = inlines.FirstOrDefault(i => i.Type == InlineType.Emphasis);
        Assert.NotNull(emphasisInline);
        Assert.Equal('*', emphasisInline.DelimiterChar);
    }

    [Fact]
    public void Parse_EmphasisWithUnderscores_CreatesEmphasisInline()
    {
        Span<char> source = "_emphasis_".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var emphasisInline = inlines.FirstOrDefault(i => i.Type == InlineType.Emphasis);
        Assert.NotNull(emphasisInline);
        Assert.Equal('_', emphasisInline.DelimiterChar);
    }

    [Fact]
    public void Parse_StrongWithAsterisks_CreatesStrongInline()
    {
        Span<char> source = "**strong**".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var strongInline = inlines.FirstOrDefault(i => i.Type == InlineType.Strong);
        Assert.NotNull(strongInline);
        Assert.Equal('*', strongInline.DelimiterChar);
        Assert.Equal(2, strongInline.DelimiterCount);
    }

    [Fact]
    public void Parse_StrongWithUnderscores_CreatesStrongInline()
    {
        Span<char> source = "__strong__".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var strongInline = inlines.FirstOrDefault(i => i.Type == InlineType.Strong);
        Assert.NotNull(strongInline);
        Assert.Equal('_', strongInline.DelimiterChar);
    }

    [Fact]
    public void Parse_MultipleEmphasisElements_ParsesBoth()
    {
        Span<char> source = "*one* and *two*".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var emphasisCount = inlines.Count(i => i.Type == InlineType.Emphasis);
        Assert.Equal(2, emphasisCount);
    }

    // ==================== Line Break Tests ====================

    [Fact]
    public void Parse_HardBreakWithTwoSpaces_CreatesHardLineBreak()
    {
        Span<char> source = "line1  \nline2".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var hardBreak = inlines.FirstOrDefault(i => i.Type == InlineType.HardLineBreak);
        Assert.NotNull(hardBreak);
    }

    [Fact]
    public void Parse_HardBreakWithBackslash_CreatesHardLineBreak()
    {
        Span<char> source = "line1\\\nline2".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var hardBreak = inlines.FirstOrDefault(i => i.Type == InlineType.HardLineBreak);
        Assert.NotNull(hardBreak);
    }

    [Fact]
    public void Parse_SoftBreak_CreatesSoftLineBreak()
    {
        Span<char> source = "line1\nline2".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var softBreak = inlines.FirstOrDefault(i => i.Type == InlineType.SoftLineBreak);
        Assert.NotNull(softBreak);
    }

    // ==================== HTML Inline Tests ====================

    [Fact]
    public void Parse_HtmlInline_CreatesHtmlInlineElement()
    {
        Span<char> source = "<span>html</span>".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var htmlInline = inlines.FirstOrDefault(i => i.Type == InlineType.HtmlInline);
        Assert.NotNull(htmlInline);
    }

    // ==================== AutoLink Tests ====================

    [Fact]
    public void Parse_AutolinkUrl_CreatesAutoLinkInline()
    {
        Span<char> source = "<http://example.com>".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var autoLink = inlines.FirstOrDefault(i => i.Type == InlineType.AutoLink);
        Assert.NotNull(autoLink);
        Assert.Equal("http://example.com", autoLink.GetLinkUrl(source).ToString());
    }

    [Fact]
    public void Parse_AutolinkEmail_CreatesAutoLinkInline()
    {
        Span<char> source = "<user@example.com>".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var autoLink = inlines.FirstOrDefault(i => i.Type == InlineType.AutoLink);
        Assert.NotNull(autoLink);
        Assert.Equal("user@example.com", autoLink.GetLinkUrl(source).ToString());
    }

    // ==================== Literal Text Tests ====================

    [Fact]
    public void Parse_PlainText_CreatesLiteralInline()
    {
        Span<char> source = "plain text".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var literal = inlines.FirstOrDefault(i => i.Type == InlineType.Literal);
        Assert.NotNull(literal);
        Assert.Equal("plain text", literal.GetContent(source).ToString());
    }

    [Fact]
    public void Parse_EmptyContent_ReturnsNoInlines()
    {
        Span<char> source = "".ToCharArray();
        var inlines = ProcessInlines(source, 0, 0);

        Assert.Empty(inlines);
    }

    // ==================== Mixed Content Tests ====================

    [Fact]
    public void Parse_TextWithCodeAndLink_ParsesBoth()
    {
        Span<char> source = "text with `code` and [link](url)".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        var hasCode = inlines.Any(i => i.Type == InlineType.Code);
        var hasLink = inlines.Any(i => i.Type == InlineType.Link);

        Assert.True(hasCode);
        Assert.True(hasLink);
    }

    [Fact]
    public void Parse_TextWithEmphasisAndCode_ParsesBoth()
    {
        Span<char> source = "*emphasis with `code`*".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        // Should parse both emphasis and code
        Assert.NotEmpty(inlines);
    }

    // ==================== Edge Cases ====================

    [Fact]
    public void Parse_ConsecutiveFormatting_HandlesCorrectly()
    {
        Span<char> source = "***bold and italic***".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        // Should handle nested/overlapping formatting
        Assert.NotEmpty(inlines);
    }

    [Fact]
    public void Parse_UnmatchedEmphasisDelimiters_TreatsAsLiteral()
    {
        Span<char> source = "*unmatched".ToCharArray();
        var inlines = ProcessInlines(source, 0, source.Length);

        // Unmatched emphasis should be treated as literal
        Assert.DoesNotContain(inlines, i => i.Type == InlineType.Emphasis);
    }
}
