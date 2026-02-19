// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using BenchmarkDotNet.Attributes;

namespace Markdig2.Benchmarks;

/// <summary>
/// Benchmarks comparing Markdig vs Markdig2 performance across different document sizes.
/// Tests small (<1KB), medium (10-100KB), and large (>1MB) markdown documents.
/// </summary>
[MemoryDiagnoser]
[GroupBenchmarksBy(BenchmarkDotNet.Configs.BenchmarkLogicalGroupRule.ByCategory)]
public class DocumentSizeBenchmarks
{
    private string _smallDoc = null!;
    private char[] _smallChars = null!;

    private string _mediumDoc = null!;
    private char[] _mediumChars = null!;

    private string _largeDoc = null!;
    private char[] _largeChars = null!;

    [GlobalSetup]
    public void Setup()
    {
        // Small document (~500 bytes) - typical comment or short message
        _smallDoc = """
            # Quick Note
            
            This is a **short** markdown document with some *emphasis* and a [link](https://example.com).
            
            - Item 1
            - Item 2
            - Item 3
            
            ```
            code snippet
            ```
            
            Final paragraph with `inline code` and more text.
            """.Replace("\r\n", "\n");
        _smallChars = _smallDoc.ToCharArray();

        // Medium document (~50KB) - typical blog post or documentation page
        _mediumDoc = GenerateMediumDocument();
        _mediumChars = _mediumDoc.ToCharArray();

        // Large document (~1.2MB) - very large documentation file or book chapter
        _largeDoc = GenerateLargeDocument();
        _largeChars = _largeDoc.ToCharArray();
    }

    private string GenerateMediumDocument()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Comprehensive Markdown Guide");
        sb.AppendLine();
        sb.AppendLine("This document demonstrates various markdown features and is designed to be representative of a typical blog post or documentation page.");
        sb.AppendLine();

        for (int section = 1; section <= 10; section++)
        {
            sb.AppendLine($"## Section {section}: Feature Overview");
            sb.AppendLine();
            sb.AppendLine($"This section covers important concepts about markdown feature set number {section}. We'll explore various aspects including **bold text**, *italic text*, and `code snippets` throughout this section.");
            sb.AppendLine();

            sb.AppendLine("### Key Points");
            sb.AppendLine();
            sb.AppendLine("Here are the main considerations:");
            sb.AppendLine();

            for (int item = 1; item <= 5; item++)
            {
                sb.AppendLine($"{item}. **Point {item}**: This is an important point about the topic. It contains detailed information with *emphasis* where needed and links to [external resources](https://example.com/page{section}-{item}).");
            }
            sb.AppendLine();

            sb.AppendLine("### Code Example");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine($"public class Example{section}");
            sb.AppendLine("{");
            sb.AppendLine($"    private readonly string _value = \"Section {section}\";");
            sb.AppendLine();
            sb.AppendLine("    public void Process()");
            sb.AppendLine("    {");
            sb.AppendLine("        Console.WriteLine($\"Processing: {_value}\");");
            sb.AppendLine("        // Additional logic here");
            sb.AppendLine("        for (int i = 0; i < 10; i++)");
            sb.AppendLine("        {");
            sb.AppendLine("            DoWork(i);");
            sb.AppendLine("        }");
            sb.AppendLine("    }");
            sb.AppendLine("}");
            sb.AppendLine("```");
            sb.AppendLine();

            sb.AppendLine("### Nested Lists and Complex Structures");
            sb.AppendLine();
            sb.AppendLine("Here we demonstrate more complex structures:");
            sb.AppendLine();
            sb.AppendLine("- **Primary Category**");
            sb.AppendLine("  - Subcategory with *details*");
            sb.AppendLine("  - Another subcategory with `code`");
            sb.AppendLine("    - Deeply nested item");
            sb.AppendLine("    - Another deep item");
            sb.AppendLine("- **Secondary Category**");
            sb.AppendLine("  - Information here");
            sb.AppendLine("  - More details");
            sb.AppendLine();

            sb.AppendLine("> **Note**: This is a blockquote with important information.");
            sb.AppendLine("> It can span multiple lines and contain *formatted* text.");
            sb.AppendLine("> ");
            sb.AppendLine("> > Nested quotes are also supported.");
            sb.AppendLine();

            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("## Conclusion");
        sb.AppendLine();
        sb.AppendLine("This document has covered all the major features and provides a realistic test case for markdown parsing and rendering performance.");
        sb.AppendLine();
        sb.AppendLine("For more information, visit <https://spec.commonmark.org> or contact <support@example.com>.");

        return sb.ToString().Replace("\r\n", "\n");
    }

    private string GenerateLargeDocument()
    {
        var sb = new System.Text.StringBuilder();

        sb.AppendLine("# Complete Markdown Specification and Examples");
        sb.AppendLine();
        sb.AppendLine("This is a comprehensive document that tests parser and renderer performance on large files similar to complete books, extensive documentation, or aggregated content.");
        sb.AppendLine();

        // Generate 25 chapters with extensive content
        for (int chapter = 1; chapter <= 25; chapter++)
        {
            sb.AppendLine($"# Chapter {chapter}: Advanced Topics in Markdown");
            sb.AppendLine();
            sb.AppendLine($"Welcome to chapter {chapter}. This chapter explores advanced concepts and provides extensive examples.");
            sb.AppendLine();

            // 8 sections per chapter
            for (int section = 1; section <= 8; section++)
            {
                sb.AppendLine($"## {chapter}.{section} Section Title: Core Concepts");
                sb.AppendLine();

                // Multiple paragraphs
                for (int para = 1; para <= 4; para++)
                {
                    sb.AppendLine($"This is paragraph {para} in section {chapter}.{section}. It contains detailed information with **bold text**, *italic text*, and `inline code`. The content is designed to be realistic and includes [links to resources](https://example.com/ch{chapter}/sec{section}/para{para}) as well as technical terminology.");
                    sb.AppendLine();
                }

                sb.AppendLine("### Detailed Examples");
                sb.AppendLine();

                // Ordered list
                for (int i = 1; i <= 7; i++)
                {
                    sb.AppendLine($"{i}. **Example {i}**: This demonstrates feature {i} with comprehensive details. See [documentation](https://docs.example.com/{chapter}-{section}-{i}) for more information.");
                }
                sb.AppendLine();

                // Code block
                sb.AppendLine("```csharp");
                sb.AppendLine($"// Example code for Chapter {chapter}, Section {section}");
                sb.AppendLine($"namespace Examples.Chapter{chapter}");
                sb.AppendLine("{");
                sb.AppendLine($"    public class Section{section}Example");
                sb.AppendLine("    {");
                sb.AppendLine($"        private readonly ILogger _logger;");
                sb.AppendLine($"        private readonly IConfiguration _config;");
                sb.AppendLine();
                sb.AppendLine($"        public Section{section}Example(ILogger logger, IConfiguration config)");
                sb.AppendLine("        {");
                sb.AppendLine("            _logger = logger ?? throw new ArgumentNullException(nameof(logger));");
                sb.AppendLine("            _config = config ?? throw new ArgumentNullException(nameof(config));");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        public async Task<Result> ProcessAsync(Input input)");
                sb.AppendLine("        {");
                sb.AppendLine("            _logger.LogInformation(\"Processing started\");");
                sb.AppendLine();
                sb.AppendLine("            try");
                sb.AppendLine("            {");
                sb.AppendLine("                var result = await PerformOperationAsync(input);");
                sb.AppendLine("                _logger.LogInformation(\"Processing completed successfully\");");
                sb.AppendLine("                return result;");
                sb.AppendLine("            }");
                sb.AppendLine("            catch (Exception ex)");
                sb.AppendLine("            {");
                sb.AppendLine("                _logger.LogError(ex, \"Processing failed\");");
                sb.AppendLine("                throw;");
                sb.AppendLine("            }");
                sb.AppendLine("        }");
                sb.AppendLine();
                sb.AppendLine("        private async Task<Result> PerformOperationAsync(Input input)");
                sb.AppendLine("        {");
                sb.AppendLine("            // Implementation details");
                sb.AppendLine("            await Task.Delay(100);");
                sb.AppendLine("            return new Result { Success = true };");
                sb.AppendLine("        }");
                sb.AppendLine("    }");
                sb.AppendLine("}");
                sb.AppendLine("```");
                sb.AppendLine();

                // Blockquote
                sb.AppendLine("> **Important Note**: This section contains critical information that should be carefully reviewed.");
                sb.AppendLine("> ");
                sb.AppendLine("> The concepts covered here form the foundation for understanding advanced topics in later chapters.");
                sb.AppendLine("> Make sure to review the code examples and try implementing them yourself.");
                sb.AppendLine();

                // Unordered list
                sb.AppendLine("### Key Takeaways");
                sb.AppendLine();
                sb.AppendLine("- **Performance**: Efficient algorithms make a significant difference");
                sb.AppendLine("  - Time complexity matters");
                sb.AppendLine("  - Space complexity is equally important");
                sb.AppendLine("  - Measure before optimizing");
                sb.AppendLine("- **Maintainability**: Write clear, self-documenting code");
                sb.AppendLine("  - Use meaningful variable names");
                sb.AppendLine("  - Add comments where necessary");
                sb.AppendLine("  - Follow coding standards");
                sb.AppendLine("- **Testing**: Comprehensive test coverage ensures reliability");
                sb.AppendLine("  - Unit tests for individual components");
                sb.AppendLine("  - Integration tests for workflows");
                sb.AppendLine("  - Performance tests under load");
                sb.AppendLine();

                sb.AppendLine("---");
                sb.AppendLine();
            }

            // Chapter summary
            sb.AppendLine($"## Chapter {chapter} Summary");
            sb.AppendLine();
            sb.AppendLine($"In this chapter, we explored numerous aspects of the topic. The key concepts included comprehensive examples with **emphasized text**, *italicized content*, `code snippets`, and [hyperlinks](https://example.com/chapter{chapter}/summary).");
            sb.AppendLine();
            sb.AppendLine($"The next chapter will build upon these foundations and introduce more advanced topics.");
            sb.AppendLine();
            sb.AppendLine("---");
            sb.AppendLine();
        }

        sb.AppendLine("# Appendix: Additional Resources");
        sb.AppendLine();
        sb.AppendLine("For further reading and reference materials, please visit:");
        sb.AppendLine();
        sb.AppendLine("- <https://spec.commonmark.org>");
        sb.AppendLine("- <https://github.github.com/gfm/>");
        sb.AppendLine("- Contact: <documentation@example.com>");

        return sb.ToString().Replace("\r\n", "\n");
    }

    // Small document benchmarks
    [Benchmark(Baseline = true, Description = "Markdig (Original) - Small Doc")]
    [BenchmarkCategory("Small")]
    public string Small_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_smallDoc);
    }

    [Benchmark(Description = "Markdig2 (Ref Struct) - Small Doc")]
    [BenchmarkCategory("Small")]
    public string Small_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _smallChars;
        return Markdown2.ToHtml(source);
    }

    // Medium document benchmarks
    [Benchmark(Baseline = true, Description = "Markdig (Original) - Medium Doc")]
    [BenchmarkCategory("Medium")]
    public string Medium_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_mediumDoc);
    }

    [Benchmark(Description = "Markdig2 (Ref Struct) - Medium Doc")]
    [BenchmarkCategory("Medium")]
    public string Medium_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _mediumChars;
        return Markdown2.ToHtml(source);
    }

    // Large document benchmarks
    [Benchmark(Baseline = true, Description = "Markdig (Original) - Large Doc")]
    [BenchmarkCategory("Large")]
    public string Large_Markdig_ToHtml()
    {
        return Markdig.Markdown.ToHtml(_largeDoc);
    }

    [Benchmark(Description = "Markdig2 (Ref Struct) - Large Doc")]
    [BenchmarkCategory("Large")]
    public string Large_Markdig2_ToHtml()
    {
        ReadOnlySpan<char> source = _largeChars;
        return Markdown2.ToHtml(source);
    }
}
