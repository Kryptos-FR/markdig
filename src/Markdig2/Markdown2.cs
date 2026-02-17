// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using System.Text;
using Markdig2.Parsers;
using Markdig2.Renderers;

namespace Markdig2;

/// <summary>
/// Provides static methods for parsing markdown and converting it to other formats.
/// Implements zero-copy, stack-based markdown processing for high performance.
/// </summary>
public static class Markdown2
{
    /// <summary>
    /// Converts a markdown string to HTML.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <returns>The HTML string representation of the markdown.</returns>
    /// <exception cref="ArgumentNullException">If markdown is null.</exception>
    public static string ToHtml(string markdown)
    {
        if (markdown is null) throw new ArgumentNullException(nameof(markdown));

        return ToHtml(markdown.AsSpan());
    }

    /// <summary>
    /// Converts markdown text (as a span) to HTML.
    /// </summary>
    /// <param name="markdown">The markdown text span to convert.</param>
    /// <returns>The HTML string representation of the markdown.</returns>
    public static string ToHtml(ReadOnlySpan<char> markdown)
    {
        if (markdown.IsEmpty)
        {
            return string.Empty;
        }

        // Parse the markdown
        Span<char> mutableMarkdown = new Span<char>(markdown.ToArray());
        var document = RefMarkdownParser.Parse(mutableMarkdown);

        // Render to HTML
        var builder = new StringBuilder();
        var writer = new Renderers.TextWriter(builder);
        var renderer = new HtmlRenderer(writer);
        renderer.Render(document);

        return writer.ToString();
    }

    /// <summary>
    /// Converts a markdown document to HTML and outputs to the specified writer.
    /// </summary>
    /// <param name="markdown">The markdown text to convert.</param>
    /// <param name="writer">The text writer to receive the HTML output.</param>
    /// <exception cref="ArgumentNullException">If markdown or writer is null.</exception>
    public static void ToHtml(string markdown, Renderers.TextWriter writer)
    {
        if (markdown is null) throw new ArgumentNullException(nameof(markdown));

        // Parse the markdown
        Span<char> mutableMarkdown = new Span<char>(markdown.ToArray());
        var document = RefMarkdownParser.Parse(mutableMarkdown);

        // Render to HTML
        var renderer = new HtmlRenderer(writer);
        renderer.Render(document);
    }

    /// <summary>
    /// Converts markdown text (as a span) to HTML and outputs to the specified writer.
    /// </summary>
    /// <param name="markdown">The markdown text span to convert.</param>
    /// <param name="writer">The text writer to receive the HTML output.</param>
    /// <exception cref="ArgumentNullException">If writer is null.</exception>
    public static void ToHtml(ReadOnlySpan<char> markdown, Renderers.TextWriter writer)
    {

        if (markdown.IsEmpty)
        {
            return;
        }

        // Parse the markdown
        Span<char> mutableMarkdown = new Span<char>(markdown.ToArray());
        var document = RefMarkdownParser.Parse(mutableMarkdown);

        // Render to HTML
        var renderer = new HtmlRenderer(writer);
        renderer.Render(document);
    }

}
