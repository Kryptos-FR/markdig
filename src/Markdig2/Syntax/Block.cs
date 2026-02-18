// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Syntax;

/// <summary>
/// A struct representing a markdown block using discriminated union pattern.
/// Stores content as indices into the source for zero-copy parsing.
/// </summary>
public struct Block
{
    /// <summary>
    /// Gets or sets the type of this block.
    /// </summary>
    public BlockType Type { get; set; }

    /// <summary>
    /// Gets or sets the line number where this block starts (0-based).
    /// </summary>
    public int Line { get; set; }

    /// <summary>
    /// Gets or sets the column where this block starts (0-based).
    /// </summary>
    public int Column { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this block is still open during parsing.
    /// </summary>
    public bool IsOpen { get; set; }

    /// <summary>
    /// Gets or sets a value indicating whether this block can be broken/interrupted.
    /// </summary>
    public bool IsBreakable { get; set; }

    // === Leaf Block Data (for blocks with content) ===

    /// <summary>
    /// Gets or sets the start index of content in the source.
    /// For leaf blocks, this points to the beginning of the block's text content.
    /// </summary>
    public int ContentStart { get; set; }

    /// <summary>
    /// Gets or sets the end index of content in the source (exclusive).
    /// For leaf blocks, this points to the end of the block's text content.
    /// </summary>
    public int ContentEnd { get; set; }

    /// <summary>
    /// Gets or sets the number of content lines in this leaf block.
    /// </summary>
    public int LineCount { get; set; }

    // === Container Block Data (for blocks with children) ===

    /// <summary>
    /// Gets or sets the index of the first child block in the blocks array.
    /// Only valid for container blocks (Quote, List, ListItem, Document).
    /// </summary>
    public int FirstChildIndex { get; set; }

    /// <summary>
    /// Gets or sets the number of child blocks.
    /// Only valid for container blocks.
    /// </summary>
    public int ChildCount { get; set; }

    // === Inline Data (for leaf blocks with inline content) ===

    /// <summary>
    /// Gets or sets the index of the first inline element in the inlines array.
    /// Only valid for leaf blocks that have inline content (Paragraph, Heading, etc.).
    /// </summary>
    public int FirstInlineIndex { get; set; }

    /// <summary>
    /// Gets or sets the number of inline elements in this leaf block.
    /// Only valid for leaf blocks that have inline content.
    /// </summary>
    public int InlineCount { get; set; }

    // === Type-Specific Data ===

    /// <summary>
    /// For Heading: the heading level (1-6).
    /// For List: the list start number (ordered lists).
    /// </summary>
    public int Data1 { get; set; }

    /// <summary>
    /// For Heading: the header character (usually '#').
    /// For CodeBlock: fence character ('`' or '~').
    /// For List: the list bullet character ('-', '*', '+') or '\0' for ordered.
    /// For ThematicBreak: the break character ('-', '*', '_').
    /// </summary>
    public char Data2 { get; set; }

    /// <summary>
    /// For CodeBlock: whether this is a fenced code block (vs indented).
    /// For List: whether this is an ordered list.
    /// For Heading: whether this is a setext heading.
    /// </summary>
    public bool Data3 { get; set; }

    /// <summary>
    /// For FencedCodeBlock: the start index of the info string (language) in source.
    /// For HtmlBlock: the start index of the HTML tag name in source.
    /// </summary>
    public int DataViewStart { get; set; }

    /// <summary>
    /// For FencedCodeBlock: the end index of the info string (language) in source.
    /// For HtmlBlock: the end index of the HTML tag name in source.
    /// </summary>
    public int DataViewEnd { get; set; }

    // Factory methods for common block types

    /// <summary>
    /// Creates a paragraph block.
    /// </summary>
    public static Block CreateParagraph(int line, int column)
    {
        return new Block
        {
            Type = BlockType.Paragraph,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = true,
        };
    }

    /// <summary>
    /// Creates a heading block.
    /// </summary>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <param name="level">The heading level (1-6).</param>
    /// <param name="headerChar">The header character (usually '#').</param>
    public static Block CreateHeading(int line, int column, int level, char headerChar = '#')
    {
        return new Block
        {
            Type = BlockType.Heading,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = false,
            Data1 = level,
            Data2 = headerChar,
        };
    }

    /// <summary>
    /// Creates a code block.
    /// </summary>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <param name="isFenced">Whether this is a fenced code block.</param>
    /// <param name="fenceChar">The fence character ('`' or '~').</param>
    public static Block CreateCodeBlock(int line, int column, bool isFenced = false, char fenceChar = '\0')
    {
        return new Block
        {
            Type = BlockType.CodeBlock,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = false,
            Data2 = fenceChar,
            Data3 = isFenced,
        };
    }

    /// <summary>
    /// Creates a thematic break block.
    /// </summary>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <param name="breakChar">The break character ('-', '*', or '_').</param>
    public static Block CreateThematicBreak(int line, int column, char breakChar)
    {
        return new Block
        {
            Type = BlockType.ThematicBreak,
            Line = line,
            Column = column,
            IsOpen = false,
            IsBreakable = false,
            Data2 = breakChar,
        };
    }

    /// <summary>
    /// Creates a blank line block.
    /// </summary>
    public static Block CreateBlankLine(int line, int column)
    {
        return new Block
        {
            Type = BlockType.BlankLine,
            Line = line,
            Column = column,
            IsOpen = false,
            IsBreakable = false,
        };
    }

    /// <summary>
    /// Creates an HTML block.
    /// </summary>
    public static Block CreateHtmlBlock(int line, int column)
    {
        return new Block
        {
            Type = BlockType.HtmlBlock,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = false,
        };
    }

    /// <summary>
    /// Creates a quote/blockquote container.
    /// </summary>
    public static Block CreateQuote(int line, int column)
    {
        return new Block
        {
            Type = BlockType.Quote,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = true,
        };
    }

    /// <summary>
    /// Creates a list container.
    /// </summary>
    /// <param name="line">The line number.</param>
    /// <param name="column">The column number.</param>
    /// <param name="isOrdered">Whether this is an ordered list.</param>
    /// <param name="bulletChar">The bullet character for unordered lists.</param>
    /// <param name="startNumber">The start number for ordered lists.</param>
    public static Block CreateList(int line, int column, bool isOrdered, char bulletChar = '\0', int startNumber = 1)
    {
        return new Block
        {
            Type = BlockType.List,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = true,
            Data1 = startNumber,
            Data2 = bulletChar,
            Data3 = isOrdered,
        };
    }

    /// <summary>
    /// Creates a list item container.
    /// </summary>
    public static Block CreateListItem(int line, int column)
    {
        return new Block
        {
            Type = BlockType.ListItem,
            Line = line,
            Column = column,
            IsOpen = true,
            IsBreakable = true,
        };
    }

    /// <summary>
    /// Creates a document (root) container.
    /// </summary>
    public static Block CreateDocument()
    {
        return new Block
        {
            Type = BlockType.Document,
            Line = 0,
            Column = 0,
            IsOpen = true,
            IsBreakable = false,
        };
    }

    // Helper properties for convenience

    /// <summary>
    /// Gets a value indicating whether this is a leaf block (has content, not children).
    /// </summary>
    public readonly bool IsLeafBlock => Type switch
    {
        BlockType.Paragraph or
        BlockType.Heading or
        BlockType.CodeBlock or
        BlockType.ThematicBreak or
        BlockType.HtmlBlock or
        BlockType.BlankLine => true,
        _ => false
    };

    /// <summary>
    /// Gets a value indicating whether this is a container block (has children, not content).
    /// </summary>
    public readonly bool IsContainerBlock => Type switch
    {
        BlockType.Quote or
        BlockType.List or
        BlockType.ListItem or
        BlockType.Document => true,
        _ => false
    };

    /// <summary>
    /// Gets the heading level (valid only for Heading blocks).
    /// </summary>
    public readonly int HeadingLevel => Type == BlockType.Heading ? Data1 : 0;

    /// <summary>
    /// Gets the header character (valid only for Heading blocks).
    /// </summary>
    public readonly char HeaderChar => Type == BlockType.Heading ? Data2 : '\0';

    /// <summary>
    /// Gets a value indicating whether this is a fenced code block (valid only for CodeBlock).
    /// </summary>
    public readonly bool IsFencedCodeBlock => Type == BlockType.CodeBlock && Data3;

    /// <summary>
    /// Gets the fence character for fenced code blocks.
    /// </summary>
    public readonly char FenceChar => Type == BlockType.CodeBlock ? Data2 : '\0';

    /// <summary>
    /// Gets a value indicating whether this is an ordered list (valid only for List blocks).
    /// </summary>
    public readonly bool IsOrderedList => Type == BlockType.List && Data3;

    /// <summary>
    /// Gets the list bullet character (valid only for List blocks).
    /// </summary>
    public readonly char BulletChar => Type == BlockType.List ? Data2 : '\0';

    /// <summary>
    /// Gets the list start number (valid only for ordered List blocks).
    /// </summary>
    public readonly int ListStartNumber => Type == BlockType.List ? Data1 : 0;

    /// <summary>
    /// Gets the thematic break character (valid only for ThematicBreak blocks).
    /// </summary>
    public readonly char ThematicBreakChar => Type == BlockType.ThematicBreak ? Data2 : '\0';

    /// <summary>
    /// Gets the content of this leaf block as a RefStringView from the source.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <returns>A RefStringView of the content.</returns>
    public readonly RefStringView GetContent(ReadOnlySpan<char> source)
    {
        if (!IsLeafBlock || ContentStart >= ContentEnd)
        {
            return new RefStringView(source, 0, 0);
        }

        return new RefStringView(source, ContentStart, ContentEnd);
    }

    /// <summary>
    /// Gets the data view (info string for code blocks, tag name for HTML blocks) as a RefStringView.
    /// </summary>
    /// <param name="source">The source markdown text.</param>
    /// <returns>A RefStringView of the data view.</returns>
    public readonly RefStringView GetDataView(ReadOnlySpan<char> source)
    {
        if (DataViewStart >= DataViewEnd)
        {
            return new RefStringView(source, 0, 0);
        }

        return new RefStringView(source, DataViewStart, DataViewEnd);
    }

    /// <summary>
    /// Returns a string representation of this block for debugging.
    /// </summary>
    public override readonly string ToString()
    {
        return Type switch
        {
            BlockType.Heading => $"Heading(Level={HeadingLevel}, Line={Line})",
            BlockType.Paragraph => $"Paragraph(Line={Line}, Lines={LineCount})",
            BlockType.CodeBlock => IsFencedCodeBlock
                ? $"FencedCodeBlock(Line={Line}, Fence={FenceChar})"
                : $"IndentedCodeBlock(Line={Line})",
            BlockType.ThematicBreak => $"ThematicBreak(Line={Line}, Char={ThematicBreakChar})",
            BlockType.Quote => $"Quote(Line={Line}, Children={ChildCount})",
            BlockType.List => IsOrderedList
                ? $"OrderedList(Line={Line}, Start={ListStartNumber})"
                : $"UnorderedList(Line={Line}, Bullet={BulletChar})",
            BlockType.ListItem => $"ListItem(Line={Line}, Children={ChildCount})",
            BlockType.Document => $"Document(Children={ChildCount})",
            _ => $"{Type}(Line={Line})"
        };
    }
}
