# Analysis: Adding Span&lt;char&gt; and Memory&lt;char&gt; Support to Markdig Parser

## Executive Summary

This document provides a comprehensive analysis of the changes required to add `Span<char>` and `Memory<char>` overloads to the `MarkdownParser.Parse()` method while maintaining backward compatibility. The analysis identifies several architectural constraints and proposes multiple implementation strategies with different trade-offs.

## Current Architecture

### 1. Current Data Flow

```
Input: string text
    ↓
MarkdownParser.Parse(string)
    ↓
LineReader(string text)
    ├─ Reads lines sequentially
    └─ Yields StringSlice objects
         ├─ Reference to original string
         └─ Start/End indices into that string
    ↓
BlockProcessor
    ├─ Processes StringSlice lines
    └─ Stores trivia as List<StringSlice>
    ↓
InlineProcessor
    └─ Processes inline content from StringSlice objects
    ↓
MarkdownDocument (AST)
    └─ Contains Block objects with StringSlice trivia
```

### 2. Key Structures

#### LineReader
- **Current Implementation**: Holds reference to `string _text`
- **Current Behavior**: Reads lines from the string, tracking line positions with `SourcePosition` (int)
- **Returns**: `StringSlice` objects that reference the original string
- **Constraints**: 
  - Assumes text is non-null and mutable position tracking
  - Uses `MemoryMarshal.CreateReadOnlySpan()` for NET targets
  - Cannot work with `Span<char>` directly (spans can't be stored in fields)

#### StringSlice
- **Current Implementation**: Value type (struct)
- **Fields**: 
  - `string Text` (immutable reference to source)
  - `int Start` (start index, inclusive)  
  - `int End` (end index, inclusive)
  - `NewLine NewLine` (line separator info)
- **Key Methods**: Indexing, character iteration, substring matching, span conversion via `AsSpan()`
- **Usage Scope**: Used everywhere in parsing (block parsing, inline parsing, trivia storage)

#### BlockProcessor
- **Key Field**: `public StringSlice Line` (current line being processed)
- **Usage**: 
  - Stores line boundaries and processes line parts
  - Creates StringSlice objects for trivia storage
  - Maintains `List<StringSlice>? LinesBefore` and stores in block trivia

#### MarkdownDocument (AST)
- **Trivia Storage**: 
  - `TriviaBefore` / `TriviaAfter` as `StringSlice` properties
  - `LinesBefore` / `LinesAfter` as `List<StringSlice>?`
- **Constraint**: These persist after parsing completes
- **Problem**: StringSlice with `Memory<char>` backing could become invalid when the source buffer is released

---

## Core Challenge: Span vs Memory

### Span<char> Limitations
1. **Cannot be stored in fields** - Only allowed in stack-like contexts (local variables, ref parameters)
2. **Cannot be boxed or used in collections** - This rules out storing in `List<StringSlice>` directly
3. **Lifetime constraints** - Compiler enforces that spans don't outlive the data they reference

**Impact**: Cannot use `Span<char>` directly in StringSlice without major architectural changes.

### Memory<char> as Alternative
1. **Can be stored in fields and collections** - Uses MemoryPool<char> or GC-managed arrays
2. **More restrictive than Span** - Must pin memory or use GC arrays
3. **Overhead**: Wrapper around underlying data; handles lifetime via reference counting
4. **Lifetime Management**: 
   - For user-provided buffer: User must keep buffer alive during parsing
   - For MemoryPool: Can be returned to pool after document creation
5. **Access via MemoryMarshal.TryGetArray()** to get underlying array if needed

**Recommendation**: `Memory<char>` is viable, but requires careful lifetime documentation.

---

## Implementation Strategies

### Strategy 1: Parallel StringSpan Type (RECOMMENDED for compatibility)

**Concept**: Create a new `StringSpan<T>` generic struct parallel to `StringSlice`, supporting both string and memory-based buffers.

**Advantages**:
- ✅ Zero breaking changes to existing API
- ✅ Allows gradual migration
- ✅ Can test independently without affecting production code
- ✅ Type-safe: compiler prevents mixing StringSlice and StringSpan
- ✅ Extensible: can add more backing store types in future

**Disadvantages**:
- ❌ Code duplication between StringSlice and StringSpan
- ❌ All parsers need two versions (IParsable methods)
- ❌ Complex maintenance

**Scope of Changes**:

1. **New Types to Create**:
   ```csharp
   // Generic backing buffer trait
   interface ICharBuffer { char Get(int index); int Length { get; } }
   
   struct StringCharBuffer : ICharBuffer { }    // wraps string
   struct MemoryCharBuffer : ICharBuffer { }    // wraps Memory<char>
   
   struct StringSpan<TBuffer> 
   where TBuffer : struct, ICharBuffer { }
   ```

2. **Affected Files**:
   - New: `Helpers/ICharBuffer.cs`, `Helpers/StringSpan.cs`
   - Modify: `LineReader.cs` → Add `LineReaderSpan<T>` or generic version
   - Modify: `MarkdownParser.cs` → Add overloads
   - Modify: Processor classes → Conditional logic for string vs span
   - Modify: AST Block classes → Store union of `StringSlice | StringSpan<T>`

3. **Trivia Storage Problem**: 
   - AST nodes would need to track which type of backing store they have
   - Could use discriminated union or parallel collections
   - Major complexity

**Note**: This approach adds significant complexity due to the need for generic constraints and conditional code paths throughout the codebase.

---

### Strategy 2: LineReader Overloads + String Buffer Copy (PRAGMATIC)

**Concept**: Support `Span<char>` / `Memory<char>` input by converting to string internally. Simple, low-complexity approach.

**Advantages**:
- ✅ Minimal code changes
- ✅ No architectural changes to LineReader, StringSlice, AST
- ✅ Zero breaking changes
- ✅ Zero maintenance burden
- ✅ Simple to document and maintain

**Disadvantages**:
- ❌ Defeats purpose of span allocation: requires string copy
- ❌ No real zero-copy benefit unless input is already string

**Viable For**: 
- When input is from high-allocation sources (pooled buffers, temporary allocations)
- When parsing performance is secondary to memory management

**Implementation**:
```csharp
// In MarkdownParser.cs
public static MarkdownDocument Parse(Span<char> text, MarkdownPipeline? pipeline = null)
{
    string str = new string(text);
    return Parse(str, pipeline);
}

public static MarkdownDocument Parse(Memory<char> text, MarkdownPipeline? pipeline = null)
{
    string str = text.Span.ToString();
    return Parse(str, pipeline);
}
```

**Scope of Changes**:
- **Modified Files**: Only `MarkdownParser.cs` (3 new method overloads, ~5 lines each)
- **Breaking Changes**: None
- **Test Changes**: Add basic overload tests

---

### Strategy 3: Owned Memory Wrapper + Lifetime Guard (PERFORMANCE-FOCUSED)

**Concept**: Use `Owned<T>` pattern with pinned buffers and lifetime guards to support true zero-copy parsing.

**Advantages**:
- ✅ True zero-copy for user-provided buffers
- ✅ Potential performance gains for large documents
- ✅ Memory-efficient for one-time parsing tasks

**Disadvantages**:
- ❌ Complex lifetime management
- ❌ Requires pinning GC objects (if not stackalloc)
- ❌ Complex error handling for use-after-free bugs
- ❌ Difficult to maintain

**How It Works**:
1. Create `IBufferOwner<char>` interface for custom buffers
2. Wrap input span/memory in an `OwnerScope<T>` RAII-like object  
3. Pass through parsing pipeline
4. Document that AST trivia references are valid only while scope is alive
5. Optionally: Materialize trivia to strings before scope exit

**Technical Challenges**:
- **Pinning**: User-provided buffers on stack would need pinning (slow)
- **GC Objects**: If backed by arrays, needs `GCHandle.Alloc()` (perf penalty)
- **Trivia Lifetime**: AST trivia (`List<StringSlice>`) becomes dangerous
  - Solution 1: Don't capture trivia when using span source
  - Solution 2: Materialize trivia to strings before scope exit
  - Solution 3: Add lifetime checker (complex)

**Scope of Changes**:
- **New Types**: `IBufferOwner<T>`, `OwnerScope<T>`, `OwnedStringSlice`
- **Modified Types**: `LineReader` (generic), `StringSlice` (union type or discriminator)
- **Modified Files**: Most parsing files (significant refactor)
- **Breaking Changes**: None technically, but adds API surface
- **Maintenance**: High - complex lifetime semantics

---

### Strategy 4: Use MemoryPool<char> Internally (BALANCED)

**Concept**: Rent buffer from `MemoryPool<char>`, copy input, parse, return buffer. Reduces allocation spikes.

**Advantages**:
- ✅ Reduces GC pressure through pooling
- ✅ Minimal changes to existing code
- ✅ No complex lifetime management
- ✅ Works with any input type (string, span, memory)

**Disadvantages**:
- ❌ Still requires copy (not zero-copy)
- ❌ Slightly different copy mechanism than string allocation
- ❌ Must handle pool buffer lifecycle

**Implementation**:
```csharp
public static MarkdownDocument Parse(ReadOnlySpan<char> text, ...)
{
    if (text.Length == 0) return new MarkdownDocument();
    
    using var buffer = MemoryPool<char>.Shared.Rent(text.Length);
    text.CopyTo(buffer.Memory.Span);
    
    // Create document wrapper that tracks buffer lifetime
    return ParseInternal(buffer.Memory[..text.Length], ...);
}
```

**Key Difference from Strategy 2**: 
- Could theoretically return pooled buffer reference in metadata
- But in practice, still materializes to string for safety

---

## Detailed Analysis of Affected Components

### 1. LineReader Class

**Current Signature**:
```csharp
public struct LineReader
{
    private readonly string _text;
    
    public LineReader(string text)
    public StringSlice ReadLine()
}
```

**Needed Changes**:

- **Option A - Generic variant**:
  ```csharp
  public struct LineReader<TBuffer> where TBuffer : ICharBuffer
  {
      private readonly TBuffer _buffer;
  }
  ```
  Requires interface/trait abstraction (see Strategy 1)

- **Option B - Overloads**:
  ```csharp
  // Keep existing
  public LineReader(string text)
  
  // Add overloads
  public LineReader(Memory<char> text)
  public LineReader(ReadOnlySpan<char> text) // Internal helper
  ```
  Each requires separate implementation

- **Option C - Memory<char> only** (Recommended for .NET 8.0+ only):
  ```csharp
  // NEW - stores Memory<char> 
  // Reference implementation for Memory backing
  
  // OLD - for backward compatibility
  public LineReader(string text) // backwards compat
  ```

**Critical Implementation Detail**: 
- LineReader must convert input to string early (Option B/C apply Strategy 2)
- OR maintain memory reference safely (requires pinning/lifetime guards)

**Recommendation**: For .NET 8.0 only target, store both `Memory<char>` and optional string reference, with conversion fallback.

---

### 2. StringSlice Structure

**Current Usage Pattern**:
```csharp
// In AST (Block.cs)
public StringSlice TriviaBefore { get; set; }
public List<StringSlice>? LinesBefore { get; set; }
public List<StringSlice>? LinesAfter { get; set; }

// In Processors
public StringSlice Line;
var trivia = new StringSlice(Line.Text, start, end, newLine);
```

**Challenge**: StringSlice holds reference to string. If we want span-based, we need union type or parallel struct.

**Recommended Changes**:

For short-term (Strategy 2): **No changes needed**
- Just convert Span<char> to string at entry point

For medium-term (Strategy 1): **Major refactor**
- Create generic `StringSpan<TBuffer>` parallel type
- Update AST to hold `StringSlice | StringSpan` discriminated union
- All processors implement generic methods

**Key Decision**: Should AST trivia be retained from span-backed documents?

Recommendation: 
- **Option A** (Safe): Don't capture trivia when parsing span-backed input
  - Processor flag: `bool captureTrivia` (disabled for span)
  - Breaking? No - trivia capture is optional feature

- **Option B** (Convenient): Materialize trivia to strings at parse time
  - Extra copies of trivia (usually small)
  - Lifetime safe by design
  - Recommended for this reason

---

### 3. BlockProcessor Class

**Current State**:
```csharp
public class BlockProcessor
{
    public StringSlice Line;
    public List<StringSlice>? LinesBefore { get; set; }
    
    public void ProcessLine(StringSlice newLine)
    public StringSlice UseTrivia(int end) => 
        new StringSlice(Line.Text, TriviaStart, end);
}
```

**Changes Needed**:

**Minimal Change (Strategy 2)**:
- No changes - LineReader converts to string

**Full Generalization (Strategy 1)**:
```csharp
public class BlockProcessor<TBuffer> where TBuffer : struct, ICharBuffer
{
    public StringSpan<TBuffer> Line;
    public List<StringSpan<TBuffer>>? LinesBefore { get; set; }
}
```

**Recommendation**: Strategy 2 (no changes needed to BlockProcessor)

---

### 4. InlineProcessor Class

**Current State**: Similar to BlockProcessor, stores StringSlice for inline content

**Changes Needed**: Same as BlockProcessor

**Additional Challenge**: InlineProcessor manages source position tracking for precise source locations:
```csharp
private readonly List<StringLineGroup.LineOffset> lineOffsets = [];
```

This offset list would need to work with both string and span-backed documents. Likely requires separate implementation or generic variant.

---

### 5. Markdown.cs Wrapper Class

**Current State**:
```csharp
public static class Markdown
{
    public static MarkdownDocument Parse(string markdown, ...)
    public static MarkdownDocument ToHtml(string markdown, TextWriter writer, ...)
    // ... 6+ overloads taking strings
}
```

**Required Additions**:
```csharp
public static MarkdownDocument Parse(ReadOnlySpan<char> markdown, ...)
public static MarkdownDocument Parse(Memory<char> markdown, ...)
public static string ToHtml(ReadOnlySpan<char> markdown, TextWriter writer, ...)
public static string ToHtml(Memory<char> markdown, TextWriter writer, ...)
// ... etc for Normalize, ToPlainText, Convert
```

Total: ~6-8 new method overloads

---

## Target Framework Considerations (.NET 8.0+)

Assuming we support only .NET 8.0+:

1. **Span<T>** - Fully supported, but can't be stored in fields
2. **Memory<T>** - Fully supported, can be stored in fields
3. **MemoryMarshal** - Full API available
4. **StackAlloc** - Fully supported, larger limits (stackalloc char[256])
5. **PinningGC** - Available but should avoid

**Recommended Approach**:
- Use `Memory<char>` for public API (vs `ReadOnlySpan` which has lifetime constraints)
- Offer `ReadOnlySpan<char>` where caller knows it lives long enough
- For trivia: Either materialize to string or disable capture

---

## Recommendations & Implementation Path

### Phase 1: Minimal MVP (2-3 hours)
**Goal**: Span/Memory support with string conversion internally

**Implementation (Strategy 2)**:
1. Add 3 overloads to `MarkdownParser.Parse()`:
   ```csharp
   public static MarkdownDocument Parse(ReadOnlySpan<char> text, ...)
   public static MarkdownDocument Parse(Memory<char> text, ...)
   public static unsafe MarkdownDocument Parse(char* text, int length, ...)
   ```

2. Add corresponding overloads to `Markdown.cs` wrapper methods (~8 overloads)

3. Implementation: Convert to string with `new string(span)` and delegate

4. Tests: Basic functionality tests for each overload

5. Documentation: Add comments explaining that conversion happens internally

**Benefits**:
- Minimal code changes
- No architectural changes
- Easy to review and maintain
- Users get API they expect
- Can upgrade later if needed

**Limitations**:
- No zero-copy benefit
- Not optimal for high-throughput scenarios

---

### Phase 2: Optimized Version (if needed later)
**Goal**: True zero-copy with Memory<char> backing

**Prerequisites**:
- Profiling data showing span input is performance bottleneck
- User demand for zero-copy support
- Careful consideration of trivia lifetime implications

**Implementation (Hybrid of Strategies 1 & 4)**:
1. Keep StringSlice string-based for backward compatibility
2. Create specialized "MaterializedSpanDocument" variant that:
   - Accepts `Memory<char>` input
   - Materializes trivia to strings during parsing
   - Returns AST with string-backed trivia for safety
3. InlineProcessor remains string-based
4. LineReader gains optional Memory<char> codepath

**Scope**: More involved, requires careful testing

---

## Implementation Checklist for Phase 1

### Files to Modify:
- [ ] `src/Markdig/Parsers/MarkdownParser.cs` (3 method overloads)
- [ ] `src/Markdig/Markdown.cs` (8+ method overloads)
- [ ] `src/Markdig.Tests/TestParser.cs` (new test suite)

### Files to Review (No Changes):
- [ ] `src/Markdig/Helpers/LineReader.cs`
- [ ] `src/Markdig/Helpers/StringSlice.cs`
- [ ] `src/Markdig/Parsers/BlockProcessor.cs`
- [ ] `src/Markdig/Parsers/InlineProcessor.cs`

### New Tests:
```csharp
[Test]
public void ParseSpan_SimpleMarkdown()
{
    ReadOnlySpan<char> markdown = "# Heading\nParagraph".AsSpan();
    var doc = MarkdownParser.Parse(markdown);
    Assert.Single(doc);
}

[Test]
public void ParseMemory_SimpleMarkdown()
{
    var markdown = new Memory<char>("# Heading".ToCharArray());
    var doc = MarkdownParser.Parse(markdown);
    Assert.Single(doc);
}

[Test]
public void Parse_StackAllocSpan()
{
    Span<char> buffer = stackalloc char[100];
    "Simple *text*".AsSpan().CopyTo(buffer);
    var doc = MarkdownParser.Parse(buffer);
    Assert.NotNull(doc);
}
```

### Documentation Needs:
- [ ] XML doc comments on new overloads explaining conversion semantics
- [ ] Update README if relevant
- [ ] Consider blog post on usage patterns

---

## Risk Analysis

### Phase 1 (String Conversion) Risks:
- **Low Risk Overall**
- Minimal code changes = minimal surface for bugs
- New overloads don't affect existing code
- Main risk: Overhead from string allocation (but acceptable for MVP)

### Phase 2 (Zero-Copy) Risks:
- **High Risk** if not done carefully
- Lifetime management bugs (AST referring to freed memory)
- Performance regression from complexity
- Maintenance burden from parallel code paths

---

## Alternative: Conservative Approach

If zero-copy is not critical, consider **not implementing Span support** and instead:
1. Document how to pre-allocate buffers using `MemoryPool<char>`
2. Provide helper methods for buffer pooling scenarios
3. Focus on other performance optimizations (e.g., parser improvements)

This avoids API clutter and maintenance burden for minimal practical benefit.

---

## Summary of Recommendations

| Strategy | Effort | Risk | Benefit | Recommended |
|----------|--------|------|---------|------------|
| 1: Parallel StringSpan | High | Medium | Good code reuse potential | No - too complex for benefit |
| 2: String Conversion | Low | Low | MVP support, simple | **Yes - Phase 1** |
| 3: Owned Memory Wrapper | Very High | High | True zero-copy | No - too risky |
| 4: MemoryPool Pooling | Medium | Low | GC reduction | Maybe - Phase 2 |

### Recommended Path Forward:
1. **Immediate (Phase 1)**: Implement Strategy 2 (string conversion overloads)
   - 2-3 hours of implementation
   - Satisfies API expectation
   - Low risk

2. **If Performance Data Justifies (Phase 2)**: Implement Strategy 4 (MemoryPool pooling)
   - Similar to Strategy 2 but with pool rental
   - Still requires string conversion for safety
   - Small overhead vs benefit trade-off

3. **Avoid**: Strategy 1 and 3 unless absolutely required
   - Too complex for current benefit analysis
   - Can be added later if stronger performance requirements emerge
