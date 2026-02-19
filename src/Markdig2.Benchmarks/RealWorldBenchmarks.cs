// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace Markdig2.Benchmarks;

/// <summary>
/// Benchmarks using real-world markdown document patterns:
/// - Reddit-style posts with comments
/// - Blog posts with mixed content
/// - GitHub README files
/// - Technical documentation
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class RealWorldBenchmarks
{
    private string _redditPost = null!;
    private char[] _redditChars = null!;
    
    private string _blogPost = null!;
    private char[] _blogChars = null!;
    
    private string _readme = null!;
    private char[] _readmeChars = null!;
    
    private string _technicalDoc = null!;
    private char[] _technicalChars = null!;

    [GlobalSetup]
    public void Setup()
    {
        _redditPost = CreateRedditPost();
        _redditChars = _redditPost.ToCharArray();
        
        _blogPost = CreateBlogPost();
        _blogChars = _blogPost.ToCharArray();
        
        _readme = CreateReadme();
        _readmeChars = _readme.ToCharArray();
        
        _technicalDoc = CreateTechnicalDoc();
        _technicalChars = _technicalDoc.ToCharArray();
    }

    private string CreateRedditPost()
    {
        return """
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
    }

    private string CreateBlogPost()
    {
        return """
            # Building a High-Performance Markdown Parser in .NET
            
            *Published on February 19, 2026 by Alex Johnson*
            
            ---
            
            In this article, we'll explore how to build a markdown parser that's faster and more memory-efficient than traditional implementations. We'll use modern .NET features like `Span<T>` and ref structs to achieve significant performance gains.
            
            ## Introduction
            
            Markdown has become the de facto standard for formatted text on the web. From GitHub READMEs to blog posts, markdown is everywhere. But parsing markdown efficiently can be challenging, especially when dealing with large documents.
            
            Let's explore how we can use modern C# features to build a parser that's both fast and memory-efficient.
            
            ## The Challenge
            
            Traditional markdown parsers typically:
            
            1. **Allocate lots of strings** - substring operations create new string instances
            2. **Create object hierarchies** - AST nodes are typically classes on the heap
            3. **Use multiple passes** - separate parsing and rendering phases
            
            Each of these contributes to memory pressure and slower performance.
            
            ## Our Approach: Zero-Copy Parsing
            
            Instead of creating new strings, we'll use `Span<char>` to work directly with the source text:
            
            ```csharp
            public ref struct RefStringView
            {
                private ReadOnlySpan<char> _source;
                private int _start;
                private int _length;
                
                public RefStringView(ReadOnlySpan<char> source, int start, int length)
                {
                    _source = source;
                    _start = start;
                    _length = length;
                }
                
                public ReadOnlySpan<char> AsSpan() => _source.Slice(_start, _length);
            }
            ```
            
            This struct gives us a "view" into the source text without copying any data!
            
            ## Architecture Overview
            
            Our parser follows a simple pipeline:
            
            ```
            Source Text (Span<char>)
                ↓
            Line Reader (ref struct)
                ↓
            Block Parser (ref struct)
                ↓
            Inline Parser (ref struct)
                ↓
            HTML Renderer
                ↓
            Output String
            ```
            
            ### Key Design Principles
            
            - **Stack-based processing** - all parsing happens on the stack
            - **Index-based relationships** - blocks reference children by index, not reference
            - **Single-pass rendering** - we render directly to output
            
            ## Implementation Details
            
            ### Block Parsing
            
            Here's how we parse block-level elements:
            
            ```csharp
            public ref struct RefBlockProcessor
            {
                private ReadOnlySpan<char> _source;
                private List<Block> _blocks;
                
                public void ProcessBlocks()
                {
                    var reader = new RefLineReader(_source);
                    
                    while (!reader.IsEnd)
                    {
                        var line = reader.ReadLine();
                        
                        if (IsHeading(line))
                            ParseHeading(line);
                        else if (IsCodeBlock(line))
                            ParseCodeBlock(ref reader);
                        else
                            ParseParagraph(line);
                    }
                }
            }
            ```
            
            ### Inline Parsing
            
            Inline elements like emphasis and links are parsed recursively:
            
            ```csharp
            public ref struct RefInlineProcessor
            {
                public void ProcessInlines(RefStringView content, List<Inline> output)
                {
                    int pos = 0;
                    var span = content.AsSpan();
                    
                    while (pos < span.Length)
                    {
                        if (span[pos] == '*')
                            ParseEmphasis(ref pos, output);
                        else if (span[pos] == '[')
                            ParseLink(ref pos, output);
                        else
                            ParseLiteral(ref pos, output);
                    }
                }
            }
            ```
            
            ## Performance Results
            
            We benchmarked our implementation against the popular Markdig library:
            
            | Metric | Markdig | Our Parser | Improvement |
            |--------|---------|------------|-------------|
            | Parse Time | 25.69 µs | 12.14 µs | **2.12x faster** |
            | Memory | 24.84 KB | 13.74 KB | **45% less** |
            | GC Gen0 | 2.01 | 0.76 | **62% less** |
            
            These are impressive gains, especially for larger documents!
            
            ## Real-World Benefits
            
            What do these numbers mean in practice?
            
            - **Web servers** can handle more requests with the same hardware
            - **Static site generators** build faster
            - **Command-line tools** feel more responsive
            - **Memory usage** stays low even with many documents
            
            ## Limitations and Trade-offs
            
            Of course, this approach isn't without trade-offs:
            
            1. **Ref struct constraints** - can't implement interfaces or be boxed
            2. **Stack-only** - the parsed document can't outlive the source text
            3. **Complexity** - lifetime management requires more careful coding
            
            For our use case (parse → render → discard), these trade-offs were acceptable.
            
            ## Lessons Learned
            
            > "Premature optimization is the root of all evil" - Donald Knuth
            
            That said, when you *do* need to optimize, modern .NET gives you powerful tools:
            
            - `Span<T>` for zero-copy operations
            - ref structs for stack allocation
            - `stackalloc` for temporary buffers
            - `ArrayPool<T>` for buffer reuse
            
            ## Conclusion
            
            Building a high-performance markdown parser taught us a lot about modern C# performance features. The combination of `Span<T>`, ref structs, and careful design can yield impressive results.
            
            If you're interested in the full implementation, check out the [source code on GitHub](https://github.com/example/markdig2).
            
            ### Further Reading
            
            - [Span<T> documentation](https://docs.microsoft.com/dotnet/api/system.span-1)
            - [ref struct specification](https://docs.microsoft.com/dotnet/csharp/language-reference/builtin-types/ref-struct)
            - [CommonMark specification](https://spec.commonmark.org/)
            
            ---
            
            *Have questions or comments? Reach out at <alex@example.com> or on Twitter [@alexjohnson](https://twitter.com/alexjohnson)*
            
            """.Replace("\r\n", "\n");
    }

    private string CreateReadme()
    {
        return """
            # Markdig2 - High-Performance Markdown Parser
            
            [![Build Status](https://img.shields.io/badge/build-passing-brightgreen)](https://github.com/example/markdig2)
            [![NuGet](https://img.shields.io/badge/nuget-v2.0.0-blue)](https://nuget.org/packages/Markdig2)
            [![License](https://img.shields.io/badge/license-BSD--2-orange)](LICENSE)
            
            A fast, memory-efficient markdown parser and HTML renderer built with modern .NET features.
            
            ## Features
            
            - ⚡ **2x faster** than traditional parsers
            - 💾 **45% less memory** usage
            - 🎯 **Zero-copy parsing** with `Span<T>`
            - ✅ **CommonMark compliant** (core features)
            - 🔧 **Simple API** - just one method call
            
            ## Quick Start
            
            ### Installation
            
            ```bash
            dotnet add package Markdig2
            ```
            
            ### Usage
            
            ```csharp
            using Markdig2;
            
            // Parse and render markdown to HTML
            string markdown = "# Hello, **World**!";
            string html = Markdown2.ToHtml(markdown);
            // Output: <h1>Hello, <strong>World</strong>!</h1>
            
            // Or use the span-based API for even better performance
            ReadOnlySpan<char> markdownSpan = markdown.AsSpan();
            string html2 = Markdown2.ToHtml(markdownSpan);
            ```
            
            ## Why Markdig2?
            
            Traditional markdown parsers allocate lots of intermediate strings and objects. Markdig2 uses ref structs and spans to avoid these allocations:
            
            | Parser | Time | Memory | GC Collections |
            |--------|------|--------|----------------|
            | Markdig | 25.7 µs | 24.8 KB | 2.01 |
            | **Markdig2** | **12.1 µs** | **13.7 KB** | **0.76** |
            
            *Benchmarked on a 2KB markdown document*
            
            ## Architecture
            
            Markdig2 uses a **zero-copy** approach:
            
            1. Parse directly from `ReadOnlySpan<char>` - no string allocations
            2. Store block/inline elements as value types in flat arrays
            3. Reference content via indices into the source span
            4. Render directly to output - no intermediate tree storage
            
            ```
            ┌─────────────────┐
            │  Source Text    │
            │ (Span<char>)    │
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │  RefLineReader  │  (ref struct)
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │ RefBlockParser  │  (ref struct)
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │ RefInlineParser │  (ref struct)
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │  HtmlRenderer   │
            └────────┬────────┘
                     │
                     ▼
            ┌─────────────────┐
            │  HTML Output    │
            └─────────────────┘
            ```
            
            ## Supported Features
            
            ### Block Elements
            
            - [x] Headings (ATX style: `# H1` through `###### H6`)
            - [x] Paragraphs
            - [x] Code blocks (fenced and indented)
            - [x] Blockquotes
            - [x] Lists (ordered and unordered)
            - [x] Thematic breaks (`---`, `***`, `___`)
            - [x] HTML blocks
            
            ### Inline Elements
            
            - [x] Emphasis (`*italic*` or `_italic_`)
            - [x] Strong emphasis (`**bold**` or `__bold__`)
            - [x] Code spans (`` `code` ``)
            - [x] Links (`[text](url "title")`)
            - [x] Images (`![alt](url "title")`)
            - [x] Autolinks (`<http://example.com>`)
            - [x] Line breaks (hard and soft)
            - [x] Raw HTML
            
            ## Performance Tips
            
            1. **Use the span-based API** when possible:
               ```csharp
               ReadOnlySpan<char> span = stackalloc char[100];
               // ... fill span ...
               string html = Markdown2.ToHtml(span);
               ```
            
            2. **Reuse output writers** for multiple documents:
               ```csharp
               var writer = new StringWriter();
               for (int i = 0; i < 1000; i++)
               {
                   writer.Clear();
                   Markdown2.ToHtml(documents[i], writer);
                   ProcessHtml(writer.ToString());
               }
               ```
            
            3. **Avoid reparsing** the same document multiple times
            
            ## Limitations
            
            - Parsed documents are **stack-only** and can't outlive the source text
            - Extension system is simplified compared to Markdig
            - Some advanced CommonMark features not yet implemented
            
            ## Roadmap
            
            - [ ] Tables extension
            - [ ] Strikethrough
            - [ ] Task lists
            - [ ] Footnotes
            - [ ] Math expressions
            - [ ] Emoji shortcuts
            
            ## Contributing
            
            Contributions are welcome! Please read our [Contributing Guide](CONTRIBUTING.md) first.
            
            ### Building
            
            ```bash
            git clone https://github.com/example/markdig2.git
            cd markdig2
            dotnet build
            dotnet test
            ```
            
            ### Running Benchmarks
            
            ```bash
            cd src/Markdig2.Benchmarks
            dotnet run -c Release
            ```
            
            ## License
            
            BSD-2-Clause - see [LICENSE](LICENSE) file for details.
            
            ## Acknowledgments
            
            - Inspired by [Markdig](https://github.com/xoofx/markdig) by Alexandre Mutel
            - Based on the [CommonMark specification](https://spec.commonmark.org/)
            - Performance techniques from the .NET team's span documentation
            
            ## Contact
            
            - **Issues**: [GitHub Issues](https://github.com/example/markdig2/issues)
            - **Email**: <support@example.com>
            - **Twitter**: [@markdig2](https://twitter.com/markdig2)
            
            ---
            
            Made with ❤️ by the Markdig2 team
            
            """.Replace("\r\n", "\n");
    }

    private string CreateTechnicalDoc()
    {
        return """
            # Markdig2 Technical Documentation
            
            ## Table of Contents
            
            1. [Architecture Overview](#architecture)
            2. [Core Components](#components)
            3. [Parsing Pipeline](#parsing)
            4. [Rendering System](#rendering)
            5. [Performance Characteristics](#performance)
            6. [API Reference](#api)
            
            ---
            
            ## Architecture Overview {#architecture}
            
            Markdig2 implements a **zero-copy parsing strategy** using modern .NET features:
            
            - `Span<T>` and `ReadOnlySpan<T>` for zero-copy text operations
            - `ref struct` types for stack-only lifetime management
            - Discriminated unions for AST node types
            - Index-based relationships instead of object references
            
            ### Design Principles
            
            1. **Minimize allocations**: Work directly with source memory
            2. **Stack-based processing**: Use ref structs to keep data on the stack
            3. **Single-pass rendering**: No intermediate tree materialization
            4. **Explicit lifetimes**: Compiler-enforced memory safety
            
            ---
            
            ## Core Components {#components}
            
            ### RefStringView
            
            A zero-copy view into a source span:
            
            ```csharp
            public ref struct RefStringView
            {
                private readonly ReadOnlySpan<char> _source;
                private readonly int _start;
                private readonly int _length;
                
                public int Length => _length;
                public ReadOnlySpan<char> AsSpan() => _source.Slice(_start, _length);
                public char this[int index] => _source[_start + index];
            }
            ```
            
            **Key Features:**
            - No string allocations
            - Direct access to source memory
            - Bounds-checked indexing
            - Efficient slicing
            
            ### RefLineReader
            
            Iterates through lines in a source span:
            
            ```csharp
            public ref struct RefLineReader
            {
                private ReadOnlySpan<char> _source;
                private int _position;
                private int _lineNumber;
                
                public RefStringView ReadLine()
                {
                    // Find next newline
                    int start = _position;
                    int end = FindLineEnd();
                    _position = end + 1;
                    _lineNumber++;
                    
                    return new RefStringView(_source, start, end - start);
                }
            }
            ```
            
            **Features:**
            - Handles Unix/Windows/Mac line endings
            - Tracks line numbers for error reporting
            - Zero allocations during iteration
            
            ### Block Structure
            
            Blocks use a discriminated union pattern:
            
            ```csharp
            public struct Block
            {
                public BlockType Type;          // Discriminator
                public int ContentStart;        // Index into source
                public int ContentEnd;          // Index into source
                public int FirstChildIndex;     // Index into block array
                public int ChildCount;          // Number of children
                public int Data1, Data2, Data3; // Type-specific data
            }
            ```
            
            **Supported Block Types:**
            - Paragraph
            - Heading (levels 1-6)
            - CodeBlock (fenced and indented)
            - Quote (blockquote)
            - List (ordered and unordered)
            - ListItem
            - ThematicBreak (horizontal rule)
            - HTML (raw HTML blocks)
            
            ### Inline Structure
            
            Similar discriminated union for inline elements:
            
            ```csharp
            public struct Inline
            {
                public InlineType Type;
                public int ContentStart;
                public int ContentEnd;
                public int FirstChildIndex;
                public int ChildCount;
                public int Data1, Data2;
            }
            ```
            
            **Supported Inline Types:**
            - Literal
            - Emphasis
            - Strong
            - Code
            - Link
            - Image
            - HardLineBreak
            - SoftLineBreak
            - HTML
            - AutoLink
            
            ---
            
            ## Parsing Pipeline {#parsing}
            
            ### Phase 1: Block Parsing
            
            ```csharp
            public ref struct RefBlockProcessor
            {
                public void ProcessBlocks(ReadOnlySpan<char> source)
                {
                    var reader = new RefLineReader(source);
                    
                    while (!reader.IsEnd)
                    {
                        var line = reader.ReadLine();
                        
                        // Try each parser in order
                        if (TryParseHeading(line, out var heading))
                            _blocks.Add(heading);
                        else if (TryParseCodeBlock(ref reader, out var code))
                            _blocks.Add(code);
                        else if (TryParseList(ref reader, out var list))
                            _blocks.Add(list);
                        // ... etc
                    }
                }
            }
            ```
            
            ### Phase 2: Inline Parsing
            
            ```csharp
            public ref struct RefInlineProcessor
            {
                public void ProcessInlines(RefStringView content, List<Inline> output)
                {
                    var span = content.AsSpan();
                    int pos = 0;
                    
                    while (pos < span.Length)
                    {
                        char c = span[pos];
                        
                        switch (c)
                        {
                            case '*' or '_':
                                ParseEmphasis(ref pos, output);
                                break;
                            case '`':
                                ParseCodeSpan(ref pos, output);
                                break;
                            case '[':
                                ParseLink(ref pos, output);
                                break;
                            // ... etc
                        }
                    }
                }
            }
            ```
            
            ---
            
            ## Rendering System {#rendering}
            
            ### Visitor Pattern
            
            The renderer uses a visitor pattern to traverse the AST:
            
            ```csharp
            public abstract class MarkdownRenderer
            {
                protected abstract void RenderBlock(ref Block block);
                protected abstract void RenderInline(ref Inline inline);
                
                public void Render(RefMarkdownDocument doc)
                {
                    foreach (ref var block in doc.GetBlocks())
                    {
                        RenderBlock(ref block);
                        
                        if (block.HasInlines())
                        {
                            foreach (ref var inline in doc.GetInlines(ref block))
                                RenderInline(ref inline);
                        }
                    }
                }
            }
            ```
            
            ### HTML Renderer
            
            Outputs HTML with proper escaping:
            
            ```csharp
            public class HtmlRenderer : MarkdownRenderer
            {
                private readonly TextWriter _writer;
                
                protected override void RenderBlock(ref Block block)
                {
                    switch (block.Type)
                    {
                        case BlockType.Heading:
                            int level = block.Data1;
                            _writer.Write($"<h{level}>");
                            // ... render content ...
                            _writer.Write($"</h{level}>");
                            break;
                        
                        case BlockType.Paragraph:
                            _writer.Write("<p>");
                            // ... render inlines ...
                            _writer.Write("</p>");
                            break;
                        
                        // ... other block types ...
                    }
                }
            }
            ```
            
            ---
            
            ## Performance Characteristics {#performance}
            
            ### Time Complexity
            
            - **Block parsing**: O(n) where n = document length
            - **Inline parsing**: O(n) where n = content length
            - **Rendering**: O(m) where m = number of nodes
            - **Overall**: O(n) linear in document size
            
            ### Space Complexity
            
            - **Stack usage**: O(d) where d = nesting depth
            - **Heap allocations**: O(b + i) where b = blocks, i = inlines
            - **Output buffer**: O(n) where n = output length
            
            ### Benchmark Results
            
            | Document Size | Parse Time | Memory | Throughput |
            |--------------|------------|---------|------------|
            | 1 KB | 3.2 µs | 4.1 KB | 312 MB/s |
            | 10 KB | 28.5 µs | 38.2 KB | 351 MB/s |
            | 100 KB | 285 µs | 385 KB | 351 MB/s |
            | 1 MB | 2.89 ms | 3.9 MB | 346 MB/s |
            
            **Observations:**
            - Consistent throughput across document sizes
            - Linear scaling with document size
            - Memory usage proportional to document complexity
            
            ---
            
            ## API Reference {#api}
            
            ### Markdown2 Class
            
            Main entry point for parsing and rendering.
            
            #### Methods
            
            ##### `ToHtml(string)`
            
            Parses markdown and returns HTML.
            
            ```csharp
            public static string ToHtml(string markdown)
            ```
            
            **Parameters:**
            - `markdown`: The markdown text to parse
            
            **Returns:** HTML string
            
            **Example:**
            ```csharp
            string html = Markdown2.ToHtml("# Hello");
            // Returns: "<h1>Hello</h1>"
            ```
            
            ##### `ToHtml(ReadOnlySpan<char>)`
            
            Parses markdown from a span (zero-copy).
            
            ```csharp
            public static string ToHtml(ReadOnlySpan<char> markdown)
            ```
            
            **Parameters:**
            - `markdown`: The markdown text as a span
            
            **Returns:** HTML string
            
            **Example:**
            ```csharp
            ReadOnlySpan<char> span = stackalloc char[] { '#', ' ', 'H', 'i' };
            string html = Markdown2.ToHtml(span);
            ```
            
            ##### `ToHtml(string, TextWriter)`
            
            Parses and writes HTML to a writer.
            
            ```csharp
            public static void ToHtml(string markdown, TextWriter writer)
            ```
            
            **Parameters:**
            - `markdown`: The markdown text to parse
            - `writer`: Output writer for HTML
            
            **Example:**
            ```csharp
            var writer = new StringWriter();
            Markdown2.ToHtml("**Bold**", writer);
            string html = writer.ToString();
            ```
            
            ### RefMarkdownParser Class
            
            Low-level parser for advanced scenarios.
            
            #### Methods
            
            ##### `Parse(ReadOnlySpan<char>)`
            
            Parses markdown into a document structure.
            
            ```csharp
            public static RefMarkdownDocument Parse(ReadOnlySpan<char> source)
            ```
            
            **Parameters:**
            - `source`: The markdown source text
            
            **Returns:** `RefMarkdownDocument` (ref struct)
            
            **Warning:** Document is only valid while source span is valid!
            
            **Example:**
            ```csharp
            ReadOnlySpan<char> source = markdown.AsSpan();
            var doc = RefMarkdownParser.Parse(source);
            
            // Use document immediately
            ProcessDocument(doc);
            
            // Don't store doc for later - it will be invalid!
            ```
            
            ---
            
            ## Advanced Topics
            
            ### Memory Management
            
            Markdig2 uses careful memory management strategies:
            
            1. **Stack allocation**: ref structs live on the stack
            2. **Pooled arrays**: Use `ArrayPool<T>` for temporary buffers
            3. **String allocation**: Only allocate output string at the end
            
            ### Thread Safety
            
            - Parsing is **thread-safe** (no shared state)
            - Multiple threads can parse different documents simultaneously
            - Shared `ArrayPool<T>` is thread-safe by design
            
            ### Error Handling
            
            Markdig2 follows the CommonMark principle of graceful degradation:
            
            - Invalid markdown is rendered as literal text
            - No exceptions are thrown during parsing
            - Malformed structures are handled sensibly
            
            ---
            
            ## Appendix: CommonMark Compliance
            
            Markdig2 implements the core CommonMark specification (v0.31.2):
            
            - ✅ Blocks: 95% compliant
            - ✅ Inlines: 90% compliant
            - ⏳ Extensions: Planned for future releases
            
            For full specification details, see <https://spec.commonmark.org/>
            
            ---
            
            *Last updated: February 19, 2026*
            
            """.Replace("\r\n", "\n");
    }

    // Reddit post benchmarks
    [Benchmark(Baseline = true, Description = "Markdig - Reddit Post")]
    [BenchmarkCategory("Reddit")]
    public string Reddit_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_redditPost);
    }

    [Benchmark(Description = "Markdig2 - Reddit Post")]
    [BenchmarkCategory("Reddit")]
    public string Reddit_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _redditChars;
        return Markdown2.ToHtml(source);
    }

    // Blog post benchmarks
    [Benchmark(Baseline = true, Description = "Markdig - Blog Post")]
    [BenchmarkCategory("Blog")]
    public string Blog_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_blogPost);
    }

    [Benchmark(Description = "Markdig2 - Blog Post")]
    [BenchmarkCategory("Blog")]
    public string Blog_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _blogChars;
        return Markdown2.ToHtml(source);
    }

    // README benchmarks
    [Benchmark(Baseline = true, Description = "Markdig - README")]
    [BenchmarkCategory("README")]
    public string Readme_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_readme);
    }

    [Benchmark(Description = "Markdig2 - README")]
    [BenchmarkCategory("README")]
    public string Readme_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _readmeChars;
        return Markdown2.ToHtml(source);
    }

    // Technical doc benchmarks
    [Benchmark(Baseline = true, Description = "Markdig - Tech Doc")]
    [BenchmarkCategory("TechDoc")]
    public string TechDoc_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_technicalDoc);
    }

    [Benchmark(Description = "Markdig2 - Tech Doc")]
    [BenchmarkCategory("TechDoc")]
    public string TechDoc_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _technicalChars;
        return Markdown2.ToHtml(source);
    }
}
