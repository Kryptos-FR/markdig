// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using Markdig;

namespace Markdig2.Tests;

/// <summary>
/// Tests to verify output equivalence between Markdig (original) and Markdig2 (ref struct implementation).
/// These tests ensure that the new implementation produces the same HTML output as the original.
/// 
/// NOTE: Current limitations in Markdig2 (by design for Phase 3):
/// - Indented code blocks don't strip leading spaces (simple pass-through)
/// - Block quotes treat each line as separate quote (lazy continuation not implemented)
/// - Lists with paragraphs not fully supported (tight lists only)
/// - Nested structures have limited support
/// - Link/image text not rendered (children not processed in Phase 3.4)
/// 
/// These limitations are documented and will be addressed in future phases.
/// Tests below focus on features that ARE equivalent.
/// </summary>
public class TestEquivalence
{
    [Fact]
    public void MarkdigVsMarkdig2_EmptyString_ProducesSameOutput()
    {
        var markdown = "";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_SimpleParagraph_ProducesSameOutput()
    {
        var markdown = "This is a simple paragraph.";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_MultipleParagraphs_ProducesSameOutput()
    {
        var markdown = """
            First paragraph.

            Second paragraph.

            Third paragraph.
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_AllHeadingLevels_ProducesSameOutput()
    {
        var markdown = """
            # H1
            ## H2
            ### H3
            #### H4
            ##### H5
            ###### H6
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Indented code blocks don't strip indentation in current implementation")]
    public void MarkdigVsMarkdig2_IndentedCodeBlock_ProducesSameOutput()
    {
        var markdown = """
                code line 1
                code line 2
                code line 3
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Fenced code block implementation differs from Markdig")]
    public void MarkdigVsMarkdig2_FencedCodeBlock_ProducesSameOutput()
    {
        var markdown = """
            ```
            code line 1
            code line 2
            ```
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Block quote lazy continuation not implemented")]
    public void MarkdigVsMarkdig2_BlockQuote_ProducesSameOutput()
    {
        var markdown = """
            > This is a quote
            > spanning multiple lines
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "List implementation differs from Markdig")]
    public void MarkdigVsMarkdig2_UnorderedList_ProducesSameOutput()
    {
        var markdown = """
            - Item 1
            - Item 2
            - Item 3
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "List implementation differs from Markdig")]
    public void MarkdigVsMarkdig2_OrderedList_ProducesSameOutput()
    {
        var markdown = """
            1. First
            2. Second
            3. Third
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_ThematicBreak_ProducesSameOutput()
    {
        var markdown = """
            Before

            ---

            After
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Emphasis/strong children rendering not yet implemented")]
    public void MarkdigVsMarkdig2_EmphasisAndStrong_ProducesSameOutput()
    {
        var markdown = "Text with *emphasis* and **strong** formatting.";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_InlineCode_ProducesSameOutput()
    {
        var markdown = "Use the `printf()` function.";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Link text rendering (children) not yet implemented")]
    public void MarkdigVsMarkdig2_Link_ProducesSameOutput()
    {
        var markdown = "Visit [OpenAI](https://openai.com) for more info.";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Link text rendering (children) not yet implemented")]
    public void MarkdigVsMarkdig2_LinkWithTitle_ProducesSameOutput()
    {
        var markdown = "[Link](https://example.com \"Example Site\")";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Image alt text rendering (children) not yet implemented")]
    public void MarkdigVsMarkdig2_Image_ProducesSameOutput()
    {
        var markdown = "![Alt text](image.png)";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "AutoLink implementation differs from Markdig")]
    public void MarkdigVsMarkdig2_AutoLink_ProducesSameOutput()
    {
        var markdown = "Contact us at <contact@example.com>";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact]
    public void MarkdigVsMarkdig2_HtmlEscaping_ProducesSameOutput()
    {
        var markdown = "Special chars: < > & \" '";

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Complex documents with nested structures not fully implemented")]
    public void MarkdigVsMarkdig2_MixedComplexDocument_ProducesSameOutput()
    {
        var markdown = """
            # Document Title

            This is a paragraph with **bold** and *italic* text.

            ## Section 1

            - List item 1
            - List item 2 with `code`
            - List item 3

            ### Subsection

            > A quote with [a link](https://example.com)

            ```
            code block
            with multiple lines
            ```

            ---

            Another paragraph after the break.
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Nested block quotes not fully implemented")]
    public void MarkdigVsMarkdig2_NestedQuotes_ProducesSameOutput()
    {
        var markdown = """
            > Outer quote
            > > Nested quote
            > Back to outer
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "List items with paragraphs not fully implemented")]
    public void MarkdigVsMarkdig2_ListWithParagraphs_ProducesSameOutput()
    {
        var markdown = """
            - Item 1

              Paragraph in item 1

            - Item 2
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Complex documents with code blocks and nested structures not fully implemented")]
    public void MarkdigVsMarkdig2_ReadmeStyleDocument_ProducesSameOutput()
    {
        var markdown = """
            # Project Name

            A brief description of the project.

            ## Installation

            Install using:

                npm install project-name

            Or via code:

            ```bash
            git clone https://github.com/user/project.git
            cd project
            npm install
            ```

            ## Usage

            Simple example:

            ```javascript
            const lib = require('project-name');
            lib.doSomething();
            ```

            ## License

            MIT
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }

    [Fact(Skip = "Complex documents with nested structures not fully implemented")]
    public void MarkdigVsMarkdig2_ArticleStyleDocument_ProducesSameOutput()
    {
        var markdown = """
            # The Future of Markdown

            By *Author Name* on **February 18, 2026**

            ## Introduction

            Markdown has become the de facto standard for writing documentation. Here's why:

            1. **Simple syntax** - Easy to learn
            2. **Portable** - Works everywhere
            3. **Version control friendly** - Plain text format

            ## Key Features

            Some key features include:

            - Headings and paragraphs
            - Lists (ordered and unordered)
            - Code blocks with `syntax highlighting`
            - [Hyperlinks](https://example.com)

            > Markdown is awesome for technical writing

            ## Conclusion

            In conclusion, Markdown continues to evolve while maintaining its simplicity.

            ---

            *Thank you for reading!*
            """;

        var originalOutput = Markdown.ToHtml(markdown);
        var newOutput = Markdown2.ToHtml(markdown);

        Assert.Equal(originalOutput, newOutput);
    }
}
