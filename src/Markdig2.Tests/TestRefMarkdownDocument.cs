// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;
using Markdig2.Syntax;

namespace Markdig2.Tests;

public class RefMarkdownDocumentTests
{
    [Fact]
    public void Constructor_InitializesCorrectly()
    {
        Span<char> source = "# Heading\n\nParagraph".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateHeading(0, 0, 1),
            Block.CreateParagraph(2, 0),
        };

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 2, lineCount: 3);

        Assert.Equal(20, doc.Source.Length); // "# Heading\n\nParagraph" = 20 chars
        Assert.Equal(2, doc.TopLevelBlockCount);
        Assert.Equal(2, doc.TotalBlockCount);
        Assert.Equal(3, doc.LineCount);
        Assert.True(doc.HasBlocks);
    }

    [Fact]
    public void GetTopLevelBlocks_ReturnsCorrectSpan()
    {
        Span<char> source = "test".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateParagraph(0, 0),
            Block.CreateHeading(1, 0, 1),
            Block.CreateParagraph(2, 0), // This is a child
        };

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 2, lineCount: 3);

        var topLevel = doc.GetTopLevelBlocks();

        Assert.Equal(2, topLevel.Length);
        Assert.Equal(BlockType.Paragraph, topLevel[0].Type);
        Assert.Equal(BlockType.Heading, topLevel[1].Type);
    }

    [Fact]
    public void GetChildren_ReturnsCorrectChildren()
    {
        Span<char> source = "test".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateQuote(0, 0),     // index 0
            Block.CreateParagraph(1, 0),  // index 1 (child of quote)
            Block.CreateParagraph(2, 0),  // index 2 (child of quote)
        };

        blocks[0].FirstChildIndex = 1;
        blocks[0].ChildCount = 2;

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 1, lineCount: 3);

        var quote = blocks[0];
        var children = doc.GetChildren(ref quote);

        Assert.Equal(2, children.Length);
        Assert.Equal(BlockType.Paragraph, children[0].Type);
        Assert.Equal(BlockType.Paragraph, children[1].Type);
    }

    [Fact]
    public void GetChildren_EmptyForLeafBlock()
    {
        Span<char> source = "test".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateParagraph(0, 0),
        };

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 1, lineCount: 1);

        var paragraph = blocks[0];
        var children = doc.GetChildren(ref paragraph);

        Assert.True(children.IsEmpty);
    }

    [Fact]
    public void GetChildren_EmptyForContainerWithNoChildren()
    {
        Span<char> source = "test".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateQuote(0, 0),
        };

        blocks[0].ChildCount = 0;

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 1, lineCount: 1);

        var quote = blocks[0];
        var children = doc.GetChildren(ref quote);

        Assert.True(children.IsEmpty);
    }

    [Fact]
    public void GetSourceView_ReturnsFullSource()
    {
        Span<char> source = "Hello World".ToCharArray();
        var blocks = new Block[] { Block.CreateParagraph(0, 0) };

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 1, lineCount: 1);

        var sourceView = doc.GetSourceView();

        Assert.Equal("Hello World", sourceView.ToString());
    }

    [Fact]
    public void HasBlocks_FalseForEmptyDocument()
    {
        Span<char> source = "".ToCharArray();
        var blocks = Array.Empty<Block>();

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 0, lineCount: 0);

        Assert.False(doc.HasBlocks);
        Assert.Equal(0, doc.TotalBlockCount);
        Assert.Equal(0, doc.TopLevelBlockCount);
    }

    [Fact]
    public void ToString_FormatsCorrectly()
    {
        Span<char> source = "test".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateParagraph(0, 0),
            Block.CreateHeading(1, 0, 1),
        };

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 2, lineCount: 5);

        var result = doc.ToString();

        Assert.Contains("Document", result);
        Assert.Contains("TopLevel=2", result);
        Assert.Contains("Total=2", result);
        Assert.Contains("Lines=5", result);
    }

    [Fact]
    public void NestedStructure_WorksCorrectly()
    {
        Span<char> source = "> quote\n> - item1\n> - item2".ToCharArray();
        var blocks = new Block[]
        {
            Block.CreateQuote(0, 0),      // index 0
            Block.CreateList(1, 2, false, '-'), // index 1 (child of quote)
            Block.CreateListItem(1, 4),   // index 2 (child of list)
            Block.CreateListItem(2, 4),   // index 3 (child of list)
        };

        // Setup parent-child relationships
        blocks[0].FirstChildIndex = 1;
        blocks[0].ChildCount = 1;
        blocks[1].FirstChildIndex = 2;
        blocks[1].ChildCount = 2;

        var doc = new RefMarkdownDocument(source, blocks, topLevelCount: 1, lineCount: 3);

        // Verify top level
        var topLevel = doc.GetTopLevelBlocks();
        Assert.Equal(1, topLevel.Length);
        Assert.Equal(BlockType.Quote, topLevel[0].Type);

        // Verify quote children
        var quote = blocks[0];
        var quoteChildren = doc.GetChildren(ref quote);
        Assert.Equal(1, quoteChildren.Length);
        Assert.Equal(BlockType.List, quoteChildren[0].Type);

        // Verify list children
        var list = blocks[1];
        var listChildren = doc.GetChildren(ref list);
        Assert.Equal(2, listChildren.Length);
        Assert.Equal(BlockType.ListItem, listChildren[0].Type);
        Assert.Equal(BlockType.ListItem, listChildren[1].Type);
    }
}
