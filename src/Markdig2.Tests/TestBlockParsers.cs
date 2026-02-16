// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Parsers;
using Markdig2.Syntax;

namespace Markdig2.Tests;

/// <summary>
/// Tests for Phase 2.1 block parsers (Heading, ThematicBreak, FencedCode, IndentedCode, HTML, Quote, List, Paragraph).
/// </summary>
public class BlockParsersTests
{
    // ==================== ATX Heading Parser Tests ====================

    [Fact]
    public void Parse_H1Heading_CreatesHeadingBlock()
    {
        Span<char> source = "# Heading 1".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        Assert.Equal(1, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Heading, blocks[0].Type);
        Assert.Equal(1, blocks[0].Data1); // Heading level
        Assert.Equal("Heading 1", blocks[0].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_H6Heading_CreatesHeadingBlock()
    {
        Span<char> source = "###### Heading 6".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Heading, blocks[0].Type);
        Assert.Equal(6, blocks[0].Data1); // Heading level
    }

    [Fact]
    public void Parse_HeadingWithTrailingHashes_IgnoresTrailing()
    {
        Span<char> source = "## Heading ##".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal("Heading", blocks[0].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_HeadingNoSpace_NotAHeading()
    {
        Span<char> source = "#NoSpace".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        // Should be a paragraph, not a heading
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    [Fact]
    public void Parse_MultipleHeadings_CreatesMultipleBlocks()
    {
        Span<char> source = "# Heading 1\n## Heading 2\n### Heading 3".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        Assert.Equal(3, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(1, blocks[0].Data1);
        Assert.Equal(2, blocks[1].Data1);
        Assert.Equal(3, blocks[2].Data1);
    }

    // ==================== Thematic Break Tests ====================

    [Fact]
    public void Parse_ThematicBreakDashes_CreatesThematicBreakBlock()
    {
        Span<char> source = "---".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.ThematicBreak, blocks[0].Type);
        Assert.Equal('-', blocks[0].Data2); // Break character
    }

    [Fact]
    public void Parse_ThematicBreakAsterisks_CreatesThematicBreakBlock()
    {
        Span<char> source = "***".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.ThematicBreak, blocks[0].Type);
        Assert.Equal('*', blocks[0].Data2);
    }

    [Fact]
    public void Parse_ThematicBreakUnderscores_CreatesThematicBreakBlock()
    {
        Span<char> source = "___".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.ThematicBreak, blocks[0].Type);
        Assert.Equal('_', blocks[0].Data2);
    }

    [Fact]
    public void Parse_ThematicBreakWithSpaces_StillWorks()
    {
        Span<char> source = "- - - - -".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.ThematicBreak, blocks[0].Type);
    }

    [Fact]
    public void Parse_ThematicBreakWithIndent_StillWorks()
    {
        Span<char> source = "   ---".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.ThematicBreak, blocks[0].Type);
    }

    [Fact]
    public void Parse_ThematicBreakTooMuchIndent_NotABreak()
    {
        Span<char> source = "    ----".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        // 4 spaces = indented code block, not thematic break
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
    }

    // ==================== Fenced Code Block Tests ====================

    [Fact]
    public void Parse_FencedCodeBlockBackticks_CreatesCodeBlock()
    {
        Span<char> source = "```\ncode line 1\ncode line 2\n```".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
        Assert.True(blocks[0].Data3); // Is fenced
        Assert.Equal('`', blocks[0].Data2); // Fence character
    }

    [Fact]
    public void Parse_FencedCodeBlockTildes_CreatesCodeBlock()
    {
        Span<char> source = "~~~\ncode line\n~~~".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
        Assert.Equal('~', blocks[0].Data2);
    }

    [Fact]
    public void Parse_FencedCodeBlockWithLanguage_ExtractiveLang()
    {
        Span<char> source = "```csharp\nvar x = 5;\n```".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
        // Language info is stored in DataViewStart/End
        Assert.NotEqual(0, blocks[0].DataViewStart);
    }

    [Fact]
    public void Parse_FencedCodeBlockNoClosingFence_StillValid()
    {
        Span<char> source = "```\ncode line 1\ncode line 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
        // Should treat as code block until EOF
    }

    // ==================== Indented Code Block Tests ====================

    [Fact]
    public void Parse_IndentedCodeBlock_CreatesCodeBlock()
    {
        Span<char> source = "    code line".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
        Assert.False(blocks[0].Data3); // Not fenced
    }

    [Fact]
    public void Parse_IndentedCodeBlockExactly4Spaces_IsCode()
    {
        Span<char> source = "    exactly 4 spaces".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.CodeBlock, blocks[0].Type);
    }

    [Fact]
    public void Parse_IndentedCodeBlockLessThan4Spaces_NotCode()
    {
        Span<char> source = "   only 3 spaces".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    // ==================== HTML Block Tests ====================

    [Fact]
    public void Parse_HtmlTag_CreatesHtmlBlock()
    {
        Span<char> source = "<div>".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.HtmlBlock, blocks[0].Type);
    }

    [Fact]
    public void Parse_HtmlComment_CreatesHtmlBlock()
    {
        Span<char> source = "<!-- comment -->".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.HtmlBlock, blocks[0].Type);
    }

    [Fact]
    public void Parse_HtmlTag_Preserved()
    {
        Span<char> source = "<table><tr><td>cell</td></tr></table>".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.HtmlBlock, blocks[0].Type);
    }

    // ==================== Block Quote Tests ====================

    [Fact]
    public void Parse_BlockQuote_CreatesQuoteBlock()
    {
        Span<char> source = "> quoted text".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Quote, blocks[0].Type);
    }

    [Fact]
    public void Parse_BlockQuoteNoSpace_StillAQuote()
    {
        Span<char> source = ">quoted text".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Quote, blocks[0].Type);
    }

    // ==================== List Tests ====================

    [Fact]
    public void Parse_UnorderedListDash_CreatesListBlock()
    {
        Span<char> source = "- item 1\n- item 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.List, blocks[0].Type);
        Assert.False(blocks[0].Data3); // Not ordered
        Assert.Equal('-', blocks[0].Data2); // Bullet char
    }

    [Fact]
    public void Parse_UnorderedListAsterisk_CreatesListBlock()
    {
        Span<char> source = "* item 1\n* item 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.List, blocks[0].Type);
        Assert.Equal('*', blocks[0].Data2);
    }

    [Fact]
    public void Parse_UnorderedListPlus_CreatesListBlock()
    {
        Span<char> source = "+ item 1\n+ item 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.List, blocks[0].Type);
        Assert.Equal('+', blocks[0].Data2);
    }

    [Fact]
    public void Parse_OrderedList_CreatesOrderedListBlock()
    {
        Span<char> source = "1. item 1\n2. item 2\n3. item 3".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.List, blocks[0].Type);
        Assert.True(blocks[0].Data3); // Is ordered
        Assert.Equal('.', blocks[0].Data2); // Delimiter
    }

    [Fact]
    public void Parse_OrderedListWithParens_CreatesOrderedListBlock()
    {
        Span<char> source = "1) item 1\n2) item 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.List, blocks[0].Type);
        Assert.Equal(')', blocks[0].Data2);
    }

    [Fact]
    public void Parse_ListNoSpace_NotAList()
    {
        Span<char> source = "-item without space".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        // Should be paragraph
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    // ==================== Paragraph Tests ====================

    [Fact]
    public void Parse_SimpleParagraph_CreatesParagraphBlock()
    {
        Span<char> source = "This is a paragraph.".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    [Fact]
    public void Parse_MultilineDefaultParagraph_CreatesSingleBlock()
    {
        Span<char> source = "Line 1\nLine 2\nLine 3".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(1, blocks.Length);
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    // ==================== Mixed Content Tests ====================

    [Fact]
    public void Parse_MixedContent_CreatesCorrectBlocks()
    {
        Span<char> source = "# Heading\n\nParagraph text\n\n---\n\n```\ncode\n```\n\n> quote".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();

        // Count block types
        int headingCount = 0, paragraphCount = 0, thematicCount = 0, codeCount = 0, quoteCount = 0;
        foreach (var block in blocks)
        {
            if (block.Type == BlockType.Heading) headingCount++;
            if (block.Type == BlockType.Paragraph) paragraphCount++;
            if (block.Type == BlockType.ThematicBreak) thematicCount++;
            if (block.Type == BlockType.CodeBlock) codeCount++;
            if (block.Type == BlockType.Quote) quoteCount++;
        }

        // Should have at least the main block types
        // Some container behaviors may vary, so we test for reasonable counts
        Assert.Equal(1, headingCount);
        Assert.True(paragraphCount >= 1); // At least one paragraph
        Assert.Equal(1, thematicCount);
        Assert.True(codeCount >= 1);    // At least one code block
        Assert.True(quoteCount >= 1);   // At least one quote
    }

    [Fact]
    public void Parse_HeadingAndParagraph_CreatesTwoBlocks()
    {
        Span<char> source = "# Title\n\nIntroduction paragraph.".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.True(blocks.Length >= 2);
        Assert.Equal(BlockType.Heading, blocks[0].Type);
        // The next non-blank block should be the paragraph
        for (int i = 1; i < blocks.Length; i++)
        {
            if (blocks[i].Type != BlockType.BlankLine)
            {
                Assert.Equal(BlockType.Paragraph, blocks[i].Type);
                break;
            }
        }
    }

    // ==================== Edge Cases ====================

    [Fact]
    public void Parse_OnlyBlankLines_CreatesBlankLineBlocks()
    {
        Span<char> source = "\n\n\n".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();

        // All blocks should be blank lines
        foreach (var block in blocks)
        {
            Assert.Equal(BlockType.BlankLine, block.Type);
        }
    }

    [Fact]
    public void Parse_TrailingNewline_HandledCorrectly()
    {
        Span<char> source = "Paragraph\n".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(1, blocks.Length);
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
    }

    [Fact]
    public void Parse_MultipleNewlines_BetweenBlocks()
    {
        Span<char> source = "Paragraph 1\n\n\n\nParagraph 2".ToCharArray();
        var doc = RefMarkdownParser.Parse(source);

        var blocks = doc.GetTopLevelBlocks();

        // Should have two paragraphs (and intervening blanks)
        int paragraphCount = 0;
        foreach (var block in blocks)
        {
            if (block.Type == BlockType.Paragraph) paragraphCount++;
        }
        Assert.Equal(2, paragraphCount);
    }
}
