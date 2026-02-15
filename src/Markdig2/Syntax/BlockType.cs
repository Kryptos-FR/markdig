// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Markdig2.Syntax;

/// <summary>
/// Enum representing the type of a markdown block.
/// </summary>
public enum BlockType
{
    /// <summary>
    /// Unknown or uninitialized block type.
    /// </summary>
    None = 0,

    /// <summary>
    /// A paragraph block (leaf).
    /// </summary>
    Paragraph,

    /// <summary>
    /// A heading block (leaf with level).
    /// </summary>
    Heading,

    /// <summary>
    /// An indented or fenced code block (leaf).
    /// </summary>
    CodeBlock,

    /// <summary>
    /// A thematic break (horizontal rule) block (leaf).
    /// </summary>
    ThematicBreak,

    /// <summary>
    /// An HTML block (leaf).
    /// </summary>
    HtmlBlock,

    /// <summary>
    /// A blank line block (leaf).
    /// </summary>
    BlankLine,

    /// <summary>
    /// A quote/blockquote container.
    /// </summary>
    Quote,

    /// <summary>
    /// A list container (ordered or unordered).
    /// </summary>
    List,

    /// <summary>
    /// A list item container.
    /// </summary>
    ListItem,

    /// <summary>
    /// The root document container.
    /// </summary>
    Document,
}
