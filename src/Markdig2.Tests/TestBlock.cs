// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Tests;

public class BlockTests
{
    [Fact]
    public void CreateParagraph_CreatesCorrectBlock()
    {
        var block = Block.CreateParagraph(5, 2);

        Assert.Equal(BlockType.Paragraph, block.Type);
        Assert.Equal(5, block.Line);
        Assert.Equal(2, block.Column);
        Assert.True(block.IsOpen);
        Assert.True(block.IsBreakable);
        Assert.True(block.IsLeafBlock);
        Assert.False(block.IsContainerBlock);
    }

    [Fact]
    public void CreateHeading_CreatesCorrectBlock()
    {
        var block = Block.CreateHeading(10, 0, 3, '#');

        Assert.Equal(BlockType.Heading, block.Type);
        Assert.Equal(10, block.Line);
        Assert.Equal(0, block.Column);
        Assert.Equal(3, block.HeadingLevel);
        Assert.Equal('#', block.HeaderChar);
        Assert.True(block.IsLeafBlock);
    }

    [Fact]
    public void CreateCodeBlock_Indented_CreatesCorrectBlock()
    {
        var block = Block.CreateCodeBlock(20, 4, isFenced: false);

        Assert.Equal(BlockType.CodeBlock, block.Type);
        Assert.Equal(20, block.Line);
        Assert.Equal(4, block.Column);
        Assert.False(block.IsFencedCodeBlock);
        Assert.True(block.IsLeafBlock);
    }

    [Fact]
    public void CreateCodeBlock_Fenced_CreatesCorrectBlock()
    {
        var block = Block.CreateCodeBlock(15, 0, isFenced: true, fenceChar: '`');

        Assert.Equal(BlockType.CodeBlock, block.Type);
        Assert.True(block.IsFencedCodeBlock);
        Assert.Equal('`', block.FenceChar);
    }

    [Fact]
    public void CreateThematicBreak_CreatesCorrectBlock()
    {
        var block = Block.CreateThematicBreak(8, 0, '-');

        Assert.Equal(BlockType.ThematicBreak, block.Type);
        Assert.Equal('-', block.ThematicBreakChar);
        Assert.False(block.IsOpen);
        Assert.True(block.IsLeafBlock);
    }

    [Fact]
    public void CreateBlankLine_CreatesCorrectBlock()
    {
        var block = Block.CreateBlankLine(12, 0);

        Assert.Equal(BlockType.BlankLine, block.Type);
        Assert.False(block.IsOpen);
        Assert.True(block.IsLeafBlock);
    }

    [Fact]
    public void CreateHtmlBlock_CreatesCorrectBlock()
    {
        var block = Block.CreateHtmlBlock(7, 0);

        Assert.Equal(BlockType.HtmlBlock, block.Type);
        Assert.True(block.IsOpen);
        Assert.True(block.IsLeafBlock);
    }

    [Fact]
    public void CreateQuote_CreatesCorrectBlock()
    {
        var block = Block.CreateQuote(3, 0);

        Assert.Equal(BlockType.Quote, block.Type);
        Assert.True(block.IsOpen);
        Assert.True(block.IsBreakable);
        Assert.True(block.IsContainerBlock);
        Assert.False(block.IsLeafBlock);
    }

    [Fact]
    public void CreateList_Unordered_CreatesCorrectBlock()
    {
        var block = Block.CreateList(5, 0, isOrdered: false, bulletChar: '-');

        Assert.Equal(BlockType.List, block.Type);
        Assert.False(block.IsOrderedList);
        Assert.Equal('-', block.BulletChar);
        Assert.True(block.IsContainerBlock);
    }

    [Fact]
    public void CreateList_Ordered_CreatesCorrectBlock()
    {
        var block = Block.CreateList(5, 0, isOrdered: true, startNumber: 5);

        Assert.Equal(BlockType.List, block.Type);
        Assert.True(block.IsOrderedList);
        Assert.Equal(5, block.ListStartNumber);
        Assert.True(block.IsContainerBlock);
    }

    [Fact]
    public void CreateListItem_CreatesCorrectBlock()
    {
        var block = Block.CreateListItem(6, 2);

        Assert.Equal(BlockType.ListItem, block.Type);
        Assert.True(block.IsContainerBlock);
    }

    [Fact]
    public void CreateDocument_CreatesCorrectBlock()
    {
        var block = Block.CreateDocument();

        Assert.Equal(BlockType.Document, block.Type);
        Assert.Equal(0, block.Line);
        Assert.Equal(0, block.Column);
        Assert.True(block.IsOpen);
        Assert.True(block.IsContainerBlock);
    }

    [Fact]
    public void GetContent_ReturnsCorrectView()
    {
        Span<char> source = "Hello World\nLine 2".ToCharArray();
        var block = Block.CreateParagraph(0, 0);
        block.ContentStart = 0;
        block.ContentEnd = 11;

        var content = block.GetContent(source);

        Assert.Equal("Hello World", content.ToString());
    }

    [Fact]
    public void GetContent_EmptyForContainerBlock()
    {
        Span<char> source = "test".ToCharArray();
        var block = Block.CreateQuote(0, 0);

        var content = block.GetContent(source);

        Assert.True(content.IsEmpty);
    }

    [Fact]
    public void GetDataView_ReturnsCorrectView()
    {
        Span<char> source = "```csharp\ncode\n```".ToCharArray();
        var block = Block.CreateCodeBlock(0, 0, isFenced: true, fenceChar: '`');
        block.DataViewStart = 3;
        block.DataViewEnd = 9;

        var dataView = block.GetDataView(source);

        Assert.Equal("csharp", dataView.ToString());
    }

    [Fact]
    public void ToString_Paragraph_FormatsCorrectly()
    {
        var block = Block.CreateParagraph(5, 0);
        block.LineCount = 3;

        var result = block.ToString();

        Assert.Contains("Paragraph", result);
        Assert.Contains("Line=5", result);
        Assert.Contains("Lines=3", result);
    }

    [Fact]
    public void ToString_Heading_FormatsCorrectly()
    {
        var block = Block.CreateHeading(10, 0, 2);

        var result = block.ToString();

        Assert.Contains("Heading", result);
        Assert.Contains("Level=2", result);
        Assert.Contains("Line=10", result);
    }

    [Fact]
    public void ToString_FencedCodeBlock_FormatsCorrectly()
    {
        var block = Block.CreateCodeBlock(15, 0, isFenced: true, fenceChar: '`');

        var result = block.ToString();

        Assert.Contains("FencedCodeBlock", result);
        Assert.Contains("Fence=`", result);
    }

    [Fact]
    public void ToString_IndentedCodeBlock_FormatsCorrectly()
    {
        var block = Block.CreateCodeBlock(15, 4, isFenced: false);

        var result = block.ToString();

        Assert.Contains("IndentedCodeBlock", result);
    }

    [Fact]
    public void ToString_Quote_FormatsCorrectly()
    {
        var block = Block.CreateQuote(3, 0);
        block.ChildCount = 2;

        var result = block.ToString();

        Assert.Contains("Quote", result);
        Assert.Contains("Children=2", result);
    }

    [Fact]
    public void ToString_OrderedList_FormatsCorrectly()
    {
        var block = Block.CreateList(7, 0, isOrdered: true, startNumber: 5);

        var result = block.ToString();

        Assert.Contains("OrderedList", result);
        Assert.Contains("Start=5", result);
    }

    [Fact]
    public void ToString_UnorderedList_FormatsCorrectly()
    {
        var block = Block.CreateList(7, 0, isOrdered: false, bulletChar: '*');

        var result = block.ToString();

        Assert.Contains("UnorderedList", result);
        Assert.Contains("Bullet=*", result);
    }

    [Fact]
    public void ChildIndexing_WorksCorrectly()
    {
        var block = Block.CreateQuote(0, 0);
        block.FirstChildIndex = 5;
        block.ChildCount = 3;

        Assert.Equal(5, block.FirstChildIndex);
        Assert.Equal(3, block.ChildCount);
    }

    [Fact]
    public void ContentIndexing_WorksCorrectly()
    {
        var block = Block.CreateParagraph(0, 0);
        block.ContentStart = 10;
        block.ContentEnd = 50;
        block.LineCount = 2;

        Assert.Equal(10, block.ContentStart);
        Assert.Equal(50, block.ContentEnd);
        Assert.Equal(2, block.LineCount);
    }
}
