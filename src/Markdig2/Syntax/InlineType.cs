// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

namespace Markdig2.Syntax;

/// <summary>
/// Enum representing the type of a markdown inline element.
/// </summary>
public enum InlineType
{
    /// <summary>
    /// Unknown or uninitialized inline type.
    /// </summary>
    None = 0,

    /// <summary>
    /// A literal text node with no special formatting.
    /// </summary>
    Literal,

    /// <summary>
    /// Emphasized text (single asterisk or underscore: *text* or _text_).
    /// </summary>
    Emphasis,

    /// <summary>
    /// Strong/bold text (double asterisk or underscore: **text** or __text__).
    /// </summary>
    Strong,

    /// <summary>
    /// Inline code (backtick delimited: `code`).
    /// </summary>
    Code,

    /// <summary>
    /// Link with text and URL: [text](url) or [text](url "title").
    /// </summary>
    Link,

    /// <summary>
    /// Image with alt text and URL: ![alt](url) or ![alt](url "title").
    /// </summary>
    Image,

    /// <summary>
    /// Softline break (line ending without two spaces).
    /// </summary>
    SoftLineBreak,

    /// <summary>
    /// Hard line break (two spaces + line ending or backslash + line ending).
    /// </summary>
    HardLineBreak,

    /// <summary>
    /// HTML markup within inline content.
    /// </summary>
    HtmlInline,

    /// <summary>
    /// Autolink (automatic URL linkification: &lt;url&gt;).
    /// </summary>
    AutoLink,
}
