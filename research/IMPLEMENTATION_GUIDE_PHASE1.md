# Implementation Guide: Phase 1 - Span/Memory Support for MarkdownParser

## Overview

This document provides detailed, step-by-step implementation guidance for Phase 1: Adding `Span<char>` and `Memory<char>` overloads to the Markdig parser using Strategy 2 (string conversion approach).

## Key Principle

**Input Conversion**: Convert `Span<char>` and `Memory<char>` to `string` at the entry point, then use existing infrastructure. This approach:
- Requires minimal code changes
- Has zero risk to existing functionality
- Provides API users expect
- Can evolve to zero-copy later if profiling justifies it

---

## Part 1: MarkdownParser.cs Changes

### Current Code (Reference)

```csharp
public static class MarkdownParser
{
    public static MarkdownDocument Parse(string text, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
    {
        if (text is null) ThrowHelper.ArgumentNullException_text();
        // ... implementation
    }
}
```

### New Overloads to Add

Add these three overloads after the existing `Parse` method:

#### Overload 1: ReadOnlySpan<char>

```csharp
/// <summary>
/// Parses the specified markdown from a read-only span into an AST <see cref="MarkdownDocument"/>.
/// </summary>
/// <remarks>
/// The span content is converted to a string for processing. This overload is provided for
/// convenience when working with stack-allocated buffers or string subregions.
/// </remarks>
/// <param name="text">A Markdown read-only span</param>
/// <param name="pipeline">The pipeline used for the parsing.</param>
/// <param name="context">A parser context used for the parsing.</param>
/// <returns>An AST Markdown document</returns>
/// <exception cref="ArgumentOutOfRangeException">If the span is larger than <see cref="string.MaxLength"/></exception>
public static MarkdownDocument Parse(ReadOnlySpan<char> text, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    // Convert span to string and use existing implementation
    string str = text.IsEmpty ? string.Empty : new string(text);
    return Parse(str, pipeline, context);
}
```

#### Overload 2: Memory<char>

```csharp
/// <summary>
/// Parses the specified markdown from a memory region into an AST <see cref="MarkdownDocument"/>.
/// </summary>
/// <remarks>
/// The memory content is converted to a string for processing. This overload is provided for
/// convenience when working with allocated buffers from <see cref="MemoryPool{T}"/> or similar sources.
/// </remarks>
/// <param name="text">A Markdown memory region</param>
/// <param name="pipeline">The pipeline used for the parsing.</param>
/// <param name="context">A parser context used for the parsing.</param>
/// <returns>An AST Markdown document</returns>
/// <exception cref="ArgumentOutOfRangeException">If the memory region is larger than <see cref="string.MaxLength"/></exception>
public static MarkdownDocument Parse(Memory<char> text, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return Parse(text.Span, pipeline, context);
}
```

#### Overload 3: Unsafe pointer (Optional, for interop)

```csharp
/// <summary>
/// Parses the specified markdown from a pointer and length into an AST <see cref="MarkdownDocument"/>.
/// </summary>
/// <remarks>
/// This method is provided for high-performance scenarios and interop with unmanaged code.
/// The caller is responsible for ensuring the pointer remains valid for the duration of the method call.
/// The content is converted to a string for processing.
/// </remarks>
/// <param name="text">A pointer to the start of markdown text (must not be null)</param>
/// <param name="length">The length of the markdown text in characters</param>
/// <param name="pipeline">The pipeline used for the parsing.</param>
/// <param name="context">A parser context used for the parsing.</param>
/// <returns>An AST Markdown document</returns>
/// <exception cref="ArgumentNullException">If text pointer is null</exception>
/// <exception cref="ArgumentOutOfRangeException">If length is negative or exceeds <see cref="string.MaxLength"/></exception>
public static unsafe MarkdownDocument Parse(char* text, int length, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    if (text == null) ThrowHelper.ArgumentNullException_text();
    if (length < 0) ThrowHelper.ArgumentOutOfRangeException_length();
    if (length > string.MaxLength) throw new ArgumentOutOfRangeException(nameof(length), "Length exceeds maximum string length");

    string str = length == 0 ? string.Empty : new string(text, 0, length);
    return Parse(str, pipeline, context);
}
```

### Implementation Notes

1. **Empty Span Handling**: The `new string(span)` constructor handles empty spans gracefully, but we optimize to use `string.Empty` for zero-length cases
2. **Null Pointer Check**: Only the unsafe overload needs explicit null checking
3. **MaxLength Check**: String constructor will throw if length exceeds `string.MaxLength`; we could add explicit checks if we want custom error messages
4. **Delegation Pattern**: All overloads delegate to the existing string overload, ensuring no duplication of parsing logic

---

## Part 2: Markdown.cs Wrapper Changes

The `Markdown` class provides convenience methods wrapping `MarkdownParser`. We need parallel overloads for each public method that currently takes a string.

### Methods Needing Overloads

Current string-based methods in `Markdown.cs`:
1. `Parse(string markdown, bool trackTrivia = false)` 
2. `Parse(string markdown, MarkdownPipeline? pipeline, MarkdownParserContext? context = null)`
3. `Normalize(string markdown, ...)`
4. `Normalize(string markdown, TextWriter writer, ...)`
5. `ToHtml(string markdown, MarkdownPipeline?, MarkdownParserContext?)`
6. `ToHtml(string markdown, TextWriter writer, ...)`
7. `ToPlainText(string markdown, TextWriter writer, ...)`
8. `ToPlainText(string markdown, MarkdownPipeline?, MarkdownParserContext?)`
9. `Convert(string markdown, IMarkdownRenderer renderer, ...)`

### Template for Adding Overloads

For each method, add overloads following this pattern:

```csharp
// Original (keep unchanged)
public static MarkdownDocument Parse(string markdown, bool trackTrivia = false)
{
    if (markdown is null) ThrowHelper.ArgumentNullException_markdown();
    MarkdownPipeline? pipeline = trackTrivia ? DefaultTrackTriviaPipeline : null;
    return Parse(markdown, pipeline);
}

// NEW: Add span overload
/// <summary>
/// Parses the specified markdown span into an AST <see cref="MarkdownDocument"/>.
/// The span content is converted to a string for processing.
/// </summary>
public static MarkdownDocument Parse(ReadOnlySpan<char> markdown, bool trackTrivia = false)
{
    if (markdown.IsEmpty && !trackTrivia)
        return new MarkdownDocument();
    
    string str = new string(markdown);
    return Parse(str, trackTrivia);
}

// NEW: Add memory overload
/// <summary>
/// Parses the specified markdown memory region into an AST <see cref="MarkdownDocument"/>.
/// The memory content is converted to a string for processing.
/// </summary>
public static MarkdownDocument Parse(Memory<char> markdown, bool trackTrivia = false)
{
    return Parse(markdown.Span, trackTrivia);
}
```

### Specific Overloads to Add

#### 1. Parse() Overloads

```csharp
public static MarkdownDocument Parse(ReadOnlySpan<char> markdown, bool trackTrivia = false)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return Parse(str, trackTrivia);
}

public static MarkdownDocument Parse(Memory<char> markdown, bool trackTrivia = false)
{
    return Parse(markdown.Span, trackTrivia);
}

public static MarkdownDocument Parse(ReadOnlySpan<char> markdown, MarkdownPipeline? pipeline, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return Parse(str, pipeline, context);
}

public static MarkdownDocument Parse(Memory<char> markdown, MarkdownPipeline? pipeline, MarkdownParserContext? context = null)
{
    return Parse(markdown.Span, pipeline, context);
}
```

#### 2. ToHtml() Overloads

```csharp
public static string ToHtml(ReadOnlySpan<char> markdown, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return ToHtml(str, pipeline, context);
}

public static string ToHtml(Memory<char> markdown, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return ToHtml(markdown.Span, pipeline, context);
}

public static MarkdownDocument ToHtml(ReadOnlySpan<char> markdown, TextWriter writer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return ToHtml(str, writer, pipeline, context);
}

public static MarkdownDocument ToHtml(Memory<char> markdown, TextWriter writer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return ToHtml(markdown.Span, writer, pipeline, context);
}
```

#### 3. Normalize() Overloads

```csharp
public static string Normalize(ReadOnlySpan<char> markdown, NormalizeOptions? options = null, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return Normalize(str, options, pipeline, context);
}

public static string Normalize(Memory<char> markdown, NormalizeOptions? options = null, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return Normalize(markdown.Span, options, pipeline, context);
}

public static MarkdownDocument Normalize(ReadOnlySpan<char> markdown, TextWriter writer, NormalizeOptions? options = null, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return Normalize(str, writer, options, pipeline, context);
}

public static MarkdownDocument Normalize(Memory<char> markdown, TextWriter writer, NormalizeOptions? options = null, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return Normalize(markdown.Span, writer, options, pipeline, context);
}
```

#### 4. ToPlainText() Overloads

```csharp
public static MarkdownDocument ToPlainText(ReadOnlySpan<char> markdown, TextWriter writer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return ToPlainText(str, writer, pipeline, context);
}

public static MarkdownDocument ToPlainText(Memory<char> markdown, TextWriter writer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return ToPlainText(markdown.Span, writer, pipeline, context);
}

public static string ToPlainText(ReadOnlySpan<char> markdown, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return ToPlainText(str, pipeline, context);
}

public static string ToPlainText(Memory<char> markdown, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return ToPlainText(markdown.Span, pipeline, context);
}
```

#### 5. Convert() Overload

```csharp
public static object Convert(ReadOnlySpan<char> markdown, IMarkdownRenderer renderer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    string str = markdown.IsEmpty ? string.Empty : new string(markdown);
    return Convert(str, renderer, pipeline, context);
}

public static object Convert(Memory<char> markdown, IMarkdownRenderer renderer, MarkdownPipeline? pipeline = null, MarkdownParserContext? context = null)
{
    return Convert(markdown.Span, renderer, pipeline, context);
}
```

---

## Part 3: Testing Strategy

### Test File: TestMarkdownParserSpan.cs (New)

Location: `src/Markdig.Tests/TestMarkdownParserSpan.cs`

```csharp
using System;
using System.IO;
using System.Text;
using Markdig.Parsers;
using Markdig.Syntax;
using Xunit;

namespace Markdig.Tests
{
    public class TestMarkdownParserSpan
    {
        [Fact]
        public void Parse_ReadOnlySpan_SimpleMarkdown()
        {
            ReadOnlySpan<char> markdown = "# Heading\n\nParagraph".AsSpan();
            var doc = MarkdownParser.Parse(markdown);
            
            Assert.NotNull(doc);
            Assert.Equal(2, doc.Count);
        }

        [Fact]
        public void Parse_Memory_SimpleMarkdown()
        {
            var chars = "# Heading\n\nParagraph".ToCharArray();
            var markdown = new Memory<char>(chars);
            var doc = MarkdownParser.Parse(markdown);
            
            Assert.NotNull(doc);
            Assert.Equal(2, doc.Count);
        }

        [Fact]
        public void Parse_EmptySpan()
        {
            ReadOnlySpan<char> markdown = ReadOnlySpan<char>.Empty;
            var doc = MarkdownParser.Parse(markdown);
            
            Assert.NotNull(doc);
            Assert.Empty(doc);
        }

        [Fact]
        public void Parse_StackAllocBuffer()
        {
            Span<char> buffer = stackalloc char[100];
            var text = "Simple *emphasis* **strong**".AsSpan();
            text.CopyTo(buffer);
            
            // Only pass the used portion
            var doc = MarkdownParser.Parse(buffer[..text.Length]);
            
            Assert.NotNull(doc);
            Assert.Single(doc);
        }

        [Fact]
        public void Parse_PooledMemory()
        {
            using var handle = System.Buffers.MemoryPool<char>.Shared.Rent(100);
            var text = "- Item 1\n- Item 2\n- Item 3".AsSpan();
            text.CopyTo(handle.Memory.Span);
            
            var doc = MarkdownParser.Parse(handle.Memory[..text.Length]);
            
            Assert.NotNull(doc);
            Assert.Single(doc); // One list block
        }

        [Fact]
        public void Parse_Span_WithPipeline()
        {
            var pipeline = new MarkdownPipelineBuilder()
                .UseEmphasisExtras()
                .Build();
            
            ReadOnlySpan<char> markdown = "~~strikethrough~~".AsSpan();
            var doc = MarkdownParser.Parse(markdown, pipeline);
            
            Assert.NotNull(doc);
        }

        [Fact]
        public void Parse_Span_WithContext()
        {
            var context = new MarkdownParserContext();
            ReadOnlySpan<char> markdown = "# Heading".AsSpan();
            var doc = MarkdownParser.Parse(markdown, null, context);
            
            Assert.NotNull(doc);
        }

        [Fact]
        public void Markdown_Parse_Span()
        {
            ReadOnlySpan<char> markdown = "**Bold text**".AsSpan();
            var doc = Markdown.Parse(markdown);
            
            Assert.NotNull(doc);
        }

        [Fact]
        public void Markdown_ToHtml_Span()
        {
            ReadOnlySpan<char> markdown = "# Hello\n\nWorld".AsSpan();
            string html = Markdown.ToHtml(markdown);
            
            Assert.Contains("<h1>", html);
            Assert.Contains("</h1>", html);
            Assert.Contains("<p>", html);
        }

        [Fact]
        public void Markdown_ToPlainText_Span()
        {
            ReadOnlySpan<char> markdown = "# Heading\n\n**Bold** text".AsSpan();
            string plain = Markdown.ToPlainText(markdown);
            
            Assert.Contains("Heading", plain);
            Assert.Contains("Bold", plain);
        }

        [Fact]
        public void Parse_Span_SameAsString()
        {
            const string markdown = "**Bold** and *italic* and `code`";
            
            ReadOnlySpan<char> span = markdown.AsSpan();
            var docSpan = Markdown.Parse(span);
            var docString = Markdown.Parse(markdown);
            
            // Both should produce identical results
            Assert.Equal(docString.Count, docSpan.Count);
        }

        [Theory]
        [InlineData("Simple text")]
        [InlineData("# Heading")]
        [InlineData("**Bold** and *italic*")]
        [InlineData("- List item 1\n- List item 2")]
        [InlineData("> Blockquote\n> More")]
        [InlineData("[Link](https://example.com)")]
        public void Parse_Span_MatchesStringResult(string markdown)
        {
            ReadOnlySpan<char> span = markdown.AsSpan();
            
            var docString = Markdown.Parse(markdown);
            var docSpan = Markdown.Parse(span);
            string htmlString = Markdown.ToHtml(markdown);
            string htmlSpan = Markdown.ToHtml(span);
            
            // Results should be identical
            Assert.Equal(docString.Count, docSpan.Count);
            Assert.Equal(htmlString, htmlSpan);
        }
    }
}
```

### Manual Testing Scenarios

Before committing, manually test these scenarios:

1. **StackAlloc Buffer**:
   ```csharp
   Span<char> buffer = stackalloc char[256];
   "# Test\n\nContent".AsSpan().CopyTo(buffer);
   var doc = Markdown.Parse(buffer[..14]);
   Console.WriteLine(doc.Count); // Should work
   ```

2. **Memory<char> from Array**:
   ```csharp
   var arr = "**Bold** text".ToCharArray();
   var doc = Markdown.Parse(new Memory<char>(arr));
   Console.WriteLine(Markdown.ToHtml(doc)); // Should work
   ```

3. **Large Document**:
   ```csharp
   var large = new StringBuilder();
   for (int i = 0; i < 10000; i++)
       large.AppendLine("Line " + i);
   
   ReadOnlySpan<char> span = large.ToString().AsSpan();
   var doc = Markdown.Parse(span);
   // Should not crash and should handle large inputs
   ```

4. **Empty Input**:
   ```csharp
   var doc = Markdown.Parse(ReadOnlySpan<char>.Empty);
   Assert.Empty(doc);
   ```

---

## Part 4: Performance Considerations

### Memory Allocation Profile

**Current behavior** (string input):
```
Input: string "..." (already allocated)
           ↓
Parse(): Uses input string directly
           ↓
No additional allocation
```

**New behavior** (span input):
```
Input: Span<char> (stack buffer or unowned memory)
           ↓
Parse(span): new string(span) → allocates new string on heap
           ↓
Parse(string): Uses the new string
           ↓
1 additional allocation (the string conversion)
```

### When This Conversion Is Worthwhile

1. **Stack-allocated buffers** (stackalloc):
   - Input: Stack memory (free once method returns)
   - Output: GC heap string (survives method return)
   - **Worth it**: Yes, if the parsed document is needed beyond the stack scope

2. **Temporary buffers** (from MemoryPool):
   - Input: Rented buffer from pool
   - Output: GC heap string
   - **Worth it**: Yes if you need the AST to exist after releasing the buffer
   - **Note**: Can optimize later by materializing to string before releasing buffer

3. **String subregions** (string data as span):
   - Input: `someString.AsSpan(start, length)`
   - Output: Copy to new string
   - **Worth it**: Debatable, but more convenient API

4. **Interop** (from unmanaged code):
   - Input: `char*` from native code
   - Output: GC string
   - **Worth it**: Yes, necessary for safety

### Optimization Opportunities (Future)

For Phase 2, if performance profiling shows this is a bottleneck:

1. **Skip conversion for frequently-cached strings**:
   ```csharp
   // Pseudo-code - not recommended for Phase 1
   static readonly ConditionalWeakTable<string, MarkdownDocument> cache;
   
   if (span is string s && cache.TryGetValue(s, out var cachedDoc))
       return cachedDoc;
   ```

2. **Use MemoryPool<char> for intermediate conversion**:
   ```csharp
   using var buffer = MemoryPool<char>.Shared.Rent(text.Length);
   text.CopyTo(buffer.Memory.Span);
   // ... process using GCHandle pinning or array extraction
   ```

3. **Lazy materialization**:
   Only convert to string when parsing requires it (most calls do immediately anyway)

---

## Part 5: Documentation Updates

### Add to README.md

```markdown
## Parsing from Spans

Markdig now supports parsing from `Span<char>` and `Memory<char>` sources in addition to strings:

### From Stack-Allocated Buffers
```csharp
// Using stackalloc for zero-allocation markdown source
Span<char> buffer = stackalloc char[256];
"# Hello\n\nWorld".AsSpan().CopyTo(buffer);

var doc = Markdown.Parse(buffer[..text.Length]);
var html = Markdown.ToHtml(buffer[..text.Length]);
```

### From MemoryPool
```csharp
using var rental = MemoryPool<char>.Shared.Rent(1024);
// ... fill rental.Memory with markdown...

var doc = Markdown.Parse(rental.Memory[..actualLength]);
```

### From Existing Span or Memory
```csharp
ReadOnlySpan<char> span = someString.AsSpan();
var doc = Markdown.Parse(span);

Memory<char> memory = new Memory<char>(charArray);
var doc = Markdown.Parse(memory);
```

The span/memory content is converted to a string internally, so the main benefit is API convenience when you already have data in span or memory form.
```

### Add XML Documentation Notes

```csharp
/// <remarks>
/// <para>
/// This overload converts the input span to a string internally. While the string
/// allocation adds a small overhead, it is often worthwhile when:
/// </para>
/// <list type="bullet">
///   <item><description>The input is from a stack-allocated buffer (stackalloc)</description></item>
///   <item><description>The input is from a temporary buffer that will be deallocated after this call</description></item>
///   <item><description>Parsing multiple documents from the same buffer - only costs one string allocation per parse</description></item>
/// </list>
/// <para>
/// The parsing pipeline and AST structure are identical to parsing from <see cref="string"/> input,
/// including full support for trivia tracking and all extensions.
/// </para>
/// </remarks>
```

---

## Implementation Checklist

### Code Changes
- [ ] Add 3 overloads to `MarkdownParser.Parse()`
- [ ] Add ~15 overloads to `Markdown` wrapper class
  - `Parse(ReadOnlySpan<char>, ...)`
  - `Parse(Memory<char>, ...)`
  - `ToHtml(ReadOnlySpan<char>, ...)`
  - `ToHtml(Memory<char>, ...)`
  - `Normalize(ReadOnlySpan<char>, ...)`
  - `Normalize(Memory<char>, ...)`
  - `ToPlainText(ReadOnlySpan<char>, ...)`
  - `ToPlainText(Memory<char>, ...)`
  - `Convert(ReadOnlySpan<char>, ...)`
  - `Convert(Memory<char>, ...)`

### Testing
- [ ] Create `TestMarkdownParserSpan.cs` test file
- [ ] Add at least 12 unit tests covering basic scenarios
- [ ] Test with StackAlloc buffers
- [ ] Test with MemoryPool rental
- [ ] Test equivalence between string and span inputs
- [ ] Verify all overload combinations work
- [ ] Test empty input edge case

### Documentation
- [ ] Add XML doc comments to all new overloads
- [ ] Update README with usage examples
- [ ] Add inline code examples for common patterns

### Validation
- [ ] Run full test suite
- [ ] No regressions in existing tests
- [ ] New tests all pass
- [ ] Code review by maintainer

---

## Estimated Effort

- **MarkdownParser.cs**: 30 minutes (3 simple overloads)
- **Markdown.cs**: 1.5 hours (15+ overloads, mostly copy-paste)
- **Testing**: 1 hour (write TestMarkdownParserSpan.cs)
- **Documentation**: 30 minutes (README + XML comments)
- **Review & Fixes**: 30 minutes

**Total: ~4 hours**

---

## Success Criteria

✅ Phase 1 is complete when:
1. All new overloads compile without warnings
2. All unit tests pass (existing + new)
3. No performance regression on string input path
4. Documentation is clear on the string conversion semantics
5. Code review approved
6. Builds successfully on all target frameworks (net8.0+)
