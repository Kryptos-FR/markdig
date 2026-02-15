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
        // Create a basic markdown document with paragraphs and blank lines
        _markdownString = @"# Introduction

This is the first paragraph. It contains some text that spans multiple lines.
This is still part of the first paragraph, demonstrating how consecutive lines
are grouped together into a single block.

This is the second paragraph. It's separated from the first by a blank line.
Markdown parsers need to handle this correctly.

## Features

Here are some key points:

- Point one with some details
- Point two with more information
- Point three to round things out

Another paragraph after the list. This helps test the parser's ability to
handle transitions between different block types.

## Conclusion

This is a final paragraph to wrap things up. It provides a good test case
for measuring parsing performance and memory allocation patterns.

The document is intentionally kept simple to focus on the core parsing
infrastructure implemented in Phase 1.
".Replace("\r\n", "\n"); // Normalize to Unix line endings

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
