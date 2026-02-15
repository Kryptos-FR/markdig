// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Parsers;
using Markdig2.Syntax;

namespace Markdig2.Tests;

public class RefMarkdownParserTests
{
    [Fact]
    public void Parse_EmptyString_ReturnsEmptyDocument()
    {
        Span<char> source = Array.Empty<char>();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(0, doc.TotalBlockCount);
        Assert.Equal(0, doc.TopLevelBlockCount);
        Assert.Equal(0, doc.LineCount);
        Assert.False(doc.HasBlocks);
    }

    [Fact]
    public void Parse_SingleParagraph_CreatesParagraphBlock()
    {
        Span<char> source = "This is a paragraph.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(1, doc.TotalBlockCount);
        Assert.Equal(1, doc.TopLevelBlockCount);
        Assert.Equal(1, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal("This is a paragraph.", blocks[0].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_MultilineParagraph_CreatesOneParagraphBlock()
    {
        Span<char> source = "First line.\nSecond line.\nThird line.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(1, doc.TotalBlockCount);
        Assert.Equal(3, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal("First line.\nSecond line.\nThird line.", blocks[0].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_TwoParagraphsSeparatedByBlankLine_CreatesTwoBlocks()
    {
        Span<char> source = "First paragraph.\n\nSecond paragraph.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(3, doc.TotalBlockCount); // para1, blank, para2
        Assert.Equal(3, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal(BlockType.BlankLine, blocks[1].Type);
        Assert.Equal(BlockType.Paragraph, blocks[2].Type);
        
        Assert.Equal("First paragraph.", blocks[0].GetContent(doc.Source).ToString().Trim());
        Assert.Equal("Second paragraph.", blocks[2].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_ParagraphWithLeadingBlankLine_CreatesBlankLineAndParagraph()
    {
        Span<char> source = "\nParagraph text.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(2, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        
        Assert.Equal(BlockType.BlankLine, blocks[0].Type);
        Assert.Equal(BlockType.Paragraph, blocks[1].Type);
        Assert.Equal("Paragraph text.", blocks[1].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_ParagraphWithTrailingBlankLine_CreatesParagraphAndBlankLine()
    {
        Span<char> source = "Paragraph text.\n\n".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(2, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal(BlockType.BlankLine, blocks[1].Type);
        Assert.Equal("Paragraph text.", blocks[0].GetContent(doc.Source).ToString().Trim());
    }

    [Fact]
    public void Parse_OnlyBlankLines_CreatesBlankLineBlocks()
    {
        Span<char> source = "\n\n\n".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(3, doc.TotalBlockCount);
        Assert.Equal(3, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        foreach (var block in blocks)
        {
            Assert.Equal(BlockType.BlankLine, block.Type);
        }
    }

    [Fact]
    public void Parse_ComplexDocument_ParsesCorrectly()
    {
        Span<char> source = "Paragraph 1.\n\nParagraph 2 line 1.\nParagraph 2 line 2.\n\n\nParagraph 3.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        // para1, blank, para2, blank, blank, para3
        Assert.Equal(6, doc.TotalBlockCount);
        Assert.Equal(7, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal(BlockType.BlankLine, blocks[1].Type);
        Assert.Equal(BlockType.Paragraph, blocks[2].Type);
        Assert.Equal(BlockType.BlankLine, blocks[3].Type);
        Assert.Equal(BlockType.BlankLine, blocks[4].Type);
        Assert.Equal(BlockType.Paragraph, blocks[5].Type);
        
        Assert.Equal("Paragraph 1.", blocks[0].GetContent(doc.Source).ToString().Trim());
        Assert.Contains("Paragraph 2 line 1", blocks[2].GetContent(doc.Source).ToString());
        Assert.Contains("Paragraph 2 line 2", blocks[2].GetContent(doc.Source).ToString());
        Assert.Equal("Paragraph 3.", blocks[5].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_BlockLineNumbers_AreCorrect()
    {
        Span<char> source = "Line 0.\n\nLine 2.\nLine 3.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(0, blocks[0].Line); // First paragraph at line 0
        Assert.Equal(1, blocks[1].Line); // Blank line at line 1
        Assert.Equal(2, blocks[2].Line); // Second paragraph starts at line 2
    }

    [Fact]
    public void Parse_ContentIndices_AreValid()
    {
        Span<char> source = "Hello\nWorld".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        var blocks = doc.GetTopLevelBlocks();
        var paragraph = blocks[0];
        
        Assert.True(paragraph.ContentStart >= 0);
        Assert.True(paragraph.ContentEnd <= source.Length);
        Assert.True(paragraph.ContentStart < paragraph.ContentEnd);
        
        var content = paragraph.GetContent(doc.Source);
        Assert.Equal("Hello\nWorld", content.ToString());
    }

    [Fact]
    public void Parse_WindowsLineEndings_HandledCorrectly()
    {
        Span<char> source = "Line 1.\r\n\r\nLine 3.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(3, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal(BlockType.BlankLine, blocks[1].Type);
        Assert.Equal(BlockType.Paragraph, blocks[2].Type);
    }

    [Fact]
    public void Parse_MixedLineEndings_HandledCorrectly()
    {
        Span<char> source = "Unix\n\rMac\r\nWindows".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.True(doc.HasBlocks);
        Assert.True(doc.LineCount >= 3);
    }

    [Fact]
    public void Parse_WhitespaceOnlyLine_TreatedAsBlankLine()
    {
        Span<char> source = "Paragraph.\n   \nAnother.".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(3, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal(BlockType.BlankLine, blocks[1].Type);
        Assert.Equal(BlockType.Paragraph, blocks[2].Type);
    }

    [Fact]
    public void Parse_SingleCharacter_CreatesParagraph()
    {
        Span<char> source = "x".ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(1, doc.TotalBlockCount);
        var blocks = doc.GetTopLevelBlocks();
        Assert.Equal(BlockType.Paragraph, blocks[0].Type);
        Assert.Equal("x", blocks[0].GetContent(doc.Source).ToString());
    }

    [Fact]
    public void Parse_VeryLongParagraph_ParsesCorrectly()
    {
        var text = string.Join("\n", Enumerable.Repeat("Line of text.", 100));
        Span<char> source = text.ToCharArray();
        
        var doc = RefMarkdownParser.Parse(source);
        
        Assert.Equal(1, doc.TotalBlockCount);
        Assert.Equal(100, doc.LineCount);
        
        var blocks = doc.GetTopLevelBlocks();
        var content = blocks[0].GetContent(doc.Source).ToString();
        Assert.Contains("Line of text.", content);
    }
}
