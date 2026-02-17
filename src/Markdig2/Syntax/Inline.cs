// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig2.Helpers;

namespace Markdig2.Syntax;

/// <summary>
/// A struct representing a markdown inline element using discriminated union pattern.
/// Stores content as indices into the source for zero-copy parsing.
/// </summary>
public struct Inline
{
    /// <summary>
    /// Gets or sets the type of this inline element.
    /// </summary>
    public InlineType Type { get; set; }

    // === Content Data (for literal, code, link text, image alt) ===

    /// <summary>
    /// Gets or sets the start index of content in the source.
    /// For Literal, Code, Link text, Image alt: points to the beginning of the content.
    /// </summary>
    public int ContentStart { get; set; }

    /// <summary>
    /// Gets or sets the end index of content in the source (exclusive).
    /// For Literal, Code, Link text, Image alt: points to the end of the content.
    /// </summary>
    public int ContentEnd { get; set; }

    // === Link/Image Data ===

    /// <summary>
    /// For Link/Image: start index of the URL in the source.
    /// </summary>
    public int LinkUrlStart { get; set; }

    /// <summary>
    /// For Link/Image: end index of the URL in the source (exclusive).
    /// </summary>
    public int LinkUrlEnd { get; set; }

    /// <summary>
    /// For Link/Image: start index of the title in the source (if present).
    /// If no title, this will be 0 and LinkTitleEnd will be 0.
    /// </summary>
    public int LinkTitleStart { get; set; }

    /// <summary>
    /// For Link/Image: end index of the title in the source (exclusive).
    /// </summary>
    public int LinkTitleEnd { get; set; }

    // === Emphasis/Strong Data ===

    /// <summary>
    /// For Emphasis/Strong: the delimiter character ('*' or '_').
    /// </summary>
    public char DelimiterChar { get; set; }

    /// <summary>
    /// For Emphasis/Strong: the number of delimiter characters (1 for emphasis, 2 for strong).
    /// </summary>
    public int DelimiterCount { get; set; }

    // === Container/Child Data ===

    /// <summary>
    /// If > 0: this inline has children (container type like Emphasis).
    /// Points to the first child inline element in the inlines array.
    /// </summary>
    public int FirstChildIndex { get; set; }

    /// <summary>
    /// Number of child inline elements.
    /// </summary>
    public int ChildCount { get; set; }

    // ==================== Factory Methods ====================

    /// <summary>
    /// Creates a literal text inline element.
    /// </summary>
    public static Inline CreateLiteral(int contentStart, int contentEnd)
    {
        return new Inline
        {
            Type = InlineType.Literal,
            ContentStart = contentStart,
            ContentEnd = contentEnd,
        };
    }

    /// <summary>
    /// Creates an emphasis inline element with optional children.
    /// </summary>
    public static Inline CreateEmphasis(char delimiterChar, int firstChildIndex = 0, int childCount = 0)
    {
        return new Inline
        {
            Type = InlineType.Emphasis,
            DelimiterChar = delimiterChar,
            DelimiterCount = 1,
            FirstChildIndex = firstChildIndex,
            ChildCount = childCount,
        };
    }

    /// <summary>
    /// Creates a strong (bold) inline element with optional children.
    /// </summary>
    public static Inline CreateStrong(char delimiterChar, int firstChildIndex = 0, int childCount = 0)
    {
        return new Inline
        {
            Type = InlineType.Strong,
            DelimiterChar = delimiterChar,
            DelimiterCount = 2,
            FirstChildIndex = firstChildIndex,
            ChildCount = childCount,
        };
    }

    /// <summary>
    /// Creates a code span (inline code) element.
    /// </summary>
    public static Inline CreateCode(int contentStart, int contentEnd)
    {
        return new Inline
        {
            Type = InlineType.Code,
            ContentStart = contentStart,
            ContentEnd = contentEnd,
        };
    }

    /// <summary>
    /// Creates a link inline element.
    /// </summary>
    public static Inline CreateLink(int textStart, int textEnd, int urlStart, int urlEnd,
        int titleStart = 0, int titleEnd = 0)
    {
        return new Inline
        {
            Type = InlineType.Link,
            ContentStart = textStart,
            ContentEnd = textEnd,
            LinkUrlStart = urlStart,
            LinkUrlEnd = urlEnd,
            LinkTitleStart = titleStart,
            LinkTitleEnd = titleEnd,
        };
    }

    /// <summary>
    /// Creates an image inline element.
    /// </summary>
    public static Inline CreateImage(int altStart, int altEnd, int urlStart, int urlEnd,
        int titleStart = 0, int titleEnd = 0)
    {
        return new Inline
        {
            Type = InlineType.Image,
            ContentStart = altStart,
            ContentEnd = altEnd,
            LinkUrlStart = urlStart,
            LinkUrlEnd = urlEnd,
            LinkTitleStart = titleStart,
            LinkTitleEnd = titleEnd,
        };
    }

    /// <summary>
    /// Creates a hard line break inline element.
    /// </summary>
    public static Inline CreateHardLineBreak()
    {
        return new Inline
        {
            Type = InlineType.HardLineBreak,
        };
    }

    /// <summary>
    /// Creates a soft line break inline element.
    /// </summary>
    public static Inline CreateSoftLineBreak()
    {
        return new Inline
        {
            Type = InlineType.SoftLineBreak,
        };
    }

    /// <summary>
    /// Creates an autolink inline element.
    /// </summary>
    public static Inline CreateAutoLink(int urlStart, int urlEnd)
    {
        return new Inline
        {
            Type = InlineType.AutoLink,
            LinkUrlStart = urlStart,
            LinkUrlEnd = urlEnd,
        };
    }

    /// <summary>
    /// Creates an HTML inline element.
    /// </summary>
    public static Inline CreateHtmlInline(int contentStart, int contentEnd)
    {
        return new Inline
        {
            Type = InlineType.HtmlInline,
            ContentStart = contentStart,
            ContentEnd = contentEnd,
        };
    }

    // ==================== Helper Properties/Methods ====================

    /// <summary>
    /// Gets the content of this inline element from the source.
    /// Works for Literal, Code, Link text, Image alt, etc.
    /// </summary>
    public RefStringView GetContent(Span<char> source)
    {
        return new RefStringView(source, ContentStart, ContentEnd);
    }

    /// <summary>
    /// Gets the URL of this link/image from the source.
    /// </summary>
    public RefStringView GetLinkUrl(Span<char> source)
    {
        return new RefStringView(source, LinkUrlStart, LinkUrlEnd);
    }

    /// <summary>
    /// Gets the title of this link/image from the source.
    /// Returns empty view if no title.
    /// </summary>
    public RefStringView GetLinkTitle(Span<char> source)
    {
        if (LinkTitleStart == 0 && LinkTitleEnd == 0)
            return new RefStringView(source, 0, 0);
        return new RefStringView(source, LinkTitleStart, LinkTitleEnd);
    }

    /// <summary>
    /// Gets whether this inline has children.
    /// </summary>
    public bool HasChildren => ChildCount > 0;

    /// <summary>
    /// Gets whether this inline is a container type (has children capability).
    /// </summary>
    public bool IsContainerType => Type is InlineType.Emphasis or InlineType.Strong;
}
