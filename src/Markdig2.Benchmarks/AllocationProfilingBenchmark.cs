// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using BenchmarkDotNet.Attributes;
using BenchmarkDotNet.Diagnosers;

namespace Markdig2.Benchmarks;

/// <summary>
/// Detailed allocation profiling benchmark to identify memory hotspots.
/// Uses memory diagnoser to show allocation call stacks.
/// </summary>
[MemoryDiagnoser]
[ShortRunJob] // Faster run for profiling
public class AllocationProfilingBenchmark
{
    private string _redditPost = null!;
    private char[] _redditChars = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Reddit post shows 1.48x more allocations - focus on this
        _redditPost = """
            # TIL: Modern C# Performance Features
            
            I recently discovered some **amazing** performance improvements in modern C# that I thought I'd share with the community.
            
            ## Background
            
            I've been working on a markdown parser and wanted to minimize allocations. Here's what I learned:
            
            ### 1. Ref Structs are Game Changers
            
            ```csharp
            public ref struct SpanParser
            {
                private ReadOnlySpan<char> _source;
                
                public SpanParser(ReadOnlySpan<char> source)
                {
                    _source = source;
                }
                
                public void Parse()
                {
                    // Zero allocations!
                }
            }
            ```
            
            **Key benefits:**
            - No heap allocations for the parser itself
            - Enforced stack-only semantics
            - Works directly with `Span<T>` and `ReadOnlySpan<T>`
            
            ### 2. Performance Numbers
            
            I benchmarked my ref struct parser vs a traditional class-based approach:
            
            | Implementation | Time | Allocations |
            |---------------|------|-------------|
            | Class-based | 28.6 µs | 24.8 KB |
            | Ref struct | 12.1 µs | 13.7 KB |
            
            That's a **2.36x speedup** with **45% less memory**!
            
            ### 3. Real-World Impact
            
            For my use case (parsing markdown documents), this means:
            
            - Faster page load times
            - Better server throughput
            - Reduced GC pressure
            - Lower memory footprint
            
            ## Gotchas to Watch Out For
            
            > **Warning**: Ref structs have limitations!
            
            1. Can't be boxed
            2. Can't implement interfaces
            3. Can't be used as fields in regular classes
            4. Must live on the stack
            
            But for my use case, these constraints were totally worth the performance gains!
            
            ## Code Example
            
            Here's a simplified version of my parser:
            
            ```csharp
            public ref struct MarkdownParser
            {
                private ReadOnlySpan<char> _source;
                private int _position;
                
                public RefMarkdownDocument Parse()
                {
                    while (_position < _source.Length)
                    {
                        if (IsHeading())
                            ParseHeading();
                        else if (IsList())
                            ParseList();
                        else
                            ParseParagraph();
                    }
                    
                    return CreateDocument();
                }
            }
            ```
            
            ## Conclusion
            
            If you're working on performance-critical code, definitely check out ref structs! They're not always the right tool, but when they fit, they can provide massive improvements.
            
            **Edit**: Thanks for the gold, kind stranger!
            
            **Edit 2**: For those asking about benchmarking methodology, I used BenchmarkDotNet with the MemoryDiagnoser. Full code is on my [GitHub](https://github.com/example/parser).
            
            ---
            
            *Posted by /u/performance_enthusiast | 247 comments*
            
            """.Replace("\r\n", "\n");
        _redditChars = _redditPost.ToCharArray();
    }

    // Baseline: Markdig
    [Benchmark(Baseline = true, Description = "Markdig ToHtml")]
    public string Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_redditPost);
    }

    // Test 1: Markdig2 full pipeline
    [Benchmark(Description = "Markdig2 ToHtml (Full)")]
    public string Markdig2_Full()
    {
        ReadOnlySpan<char> source = _redditChars;
        return Markdown2.ToHtml(source);
    }

    // Test 2: Parse only (no rendering)
    [Benchmark(Description = "Markdig2 Parse Only")]
    public void Markdig2_ParseOnly()
    {
        ReadOnlySpan<char> source = _redditChars;
        var doc = Parsers.RefMarkdownParser.Parse(source);

        // Access document to prevent optimization
        _ = doc.TotalBlockCount;
        _ = doc.TotalInlineCount;
    }

    // Test 3: Just char array creation (baseline overhead)
    [Benchmark(Description = "Char Array Creation")]
    public char[] CharArrayCreation()
    {
        return _redditPost.ToCharArray();
    }

    // Test 4: Rendering phase
    [Benchmark(Description = "HTML Rendering")]
    public string Markdig2_RenderingOnly()
    {
        ReadOnlySpan<char> source = _redditChars;

        // Parse first (this allocation is expected)
        var doc = Parsers.RefMarkdownParser.Parse(source);

        // Focus on rendering allocations
        var writer = new System.Text.StringBuilder(capacity: 5000);
        var textWriter = new Renderers.TextWriter(writer);
        var renderer = new Renderers.HtmlRenderer(textWriter);
        renderer.Render(doc);

        return writer.ToString();
    }
}
