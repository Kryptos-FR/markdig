// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Jobs;
using Markdig2.Parsers;

namespace Markdig2.Benchmarks;

/// <summary>
/// Benchmark comparing Markdig (original) vs Markdig2 (ref struct) parsing performance.
/// </summary>
[MemoryDiagnoser]
public class ParsingBenchmark
{
    private string _markdownString = null!;
    private char[] _markdownChars = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Create a comprehensive markdown document with Phase 2 features:
        // - Block types: headings, paragraphs, lists, code blocks, quotes, thematic breaks
        // - Inline types: emphasis, strong, code spans, links, images
        _markdownString = """
            # Markdig2 Performance Test Document

            This document tests **all Phase 2 features** including *emphasis*, `code spans`, and [links](https://example.com).

            ## Core Features

            Here are the *key capabilities* that we're benchmarking:

            - **Bold text** for emphasis
            - `inline code` for technical terms
            - [Hyperlinks](https://github.com) for navigation
            - *Italic text* for subtle emphasis

            ### Nested Structures

            We also support nested formatting like ***bold and italic*** together, and even `code with **bold**` nearby.

            ## Code Blocks

            ```csharp
            public class Example
            {
                public void Method()
                {
                    Console.WriteLine("Hello, World!");
                }
            }
            ```

            Another paragraph after code.

            ## Blockquotes

            > This is a blockquote with *emphasis*.
            > It can span multiple lines.
            > > And even nest with **strong** text.

            Back to normal paragraphs with links to [documentation](https://spec.commonmark.org).

            ## Lists and More

            1. First ordered item with `code`
            2. Second item with [a link](url)
            3. Third item with **strong emphasis**

            Followed by unordered:

            - Item with *italic*
            - Another item
              - Nested item with `code`
              - Another nested item

            ---

            ## Advanced Inline Elements

            Visit <http://example.com> or email <user@example.com> for more information.

            Text with hard line break:  
            Next line here.

            ## Final Section

            This comprehensive document exercises:
            - All **block types**: headings, paragraphs, lists, code, quotes, thematic breaks
            - All **inline types**: emphasis, strong, code spans, links, images, autolinks, line breaks
            - Realistic **nesting** and **mixed content**

            The goal is to measure performance on documents similar to real-world markdown files.

            """.Replace("\r\n", "\n"); // Normalize to Unix line endings

        _markdownChars = _markdownString.ToCharArray();
    }

    [Benchmark(Baseline = true, Description = "Markdig (Original)")]
    public object Markdig_Parse()
    {
        return Markdig.Markdown.Parse(_markdownString);
    }

    [Benchmark(Description = "Markdig2 (Ref Struct)")]
    public void Markdig2_Parse()
    {
        Span<char> source = _markdownChars;
        var doc = RefMarkdownParser.Parse(source);

        // Access the document to ensure it's not optimized away
        _ = doc.TotalBlockCount;
        _ = doc.LineCount;
    }
}
