# Alternative Strategy: Ref Struct MemoryDocument for Stack-Based Parsing

## Overview

**Concept**: Parse `Span<char>` directly into a stack-only `MemoryDocument` (ref struct) using parallel ref struct versions of core parsing types. Use the MemoryDocument to render directly to HTML (or materialize to persistent `MarkdownDocument` if AST is needed).

**Architecture**:
```
Input: Span<char> (stack buffer)
      ↓
Parse(span) → MemoryDocument (ref struct)
      ├─ Contains: RefStructBlock[] tree on stack
      ├─ Uses: RefStructStringSlice (backed by span)
      └─ Lives: Only on stack during parsing
      ↓
Render(memoryDoc) → HTML string
      ↓
Optional: Materialize(memoryDoc) → MarkdownDocument (long-lived)
```

---

## Key Insight: Ref Struct Lifetime Management

**Why This Works**:
```csharp
// This is valid and enforced by compiler
ref struct MemoryDocument
{
    public Span<char> Source;        // ✅ Ref struct can contain Span<char>
    public RefStructBlock[] Blocks;  // ✅ Ref struct can contain other ref structs
}

// This is INVALID - compiler prevents it
public class Parser
{
    public MemoryDocument Document;  // ❌ Can't store ref struct in class field
}

// This is valid - parameter passing
public void Process(MemoryDocument doc)  // ✅ Ref struct as parameter
{
    // Compiler tracks that 'doc' borrows from original span
    // Won't allow doc to outlive the span
}
```

**Compiler's Guarantee**: You cannot store a ref struct where it could outlive the span it references. This eliminates the lifetime-safety problem from Strategy 3.

---

## Implementation Model

### Type Hierarchy (Parallel)

**Current (String-backed, persistent)**:
```csharp
public class MarkdownDocument : { ... }
public class Block { ... }
public class ContainerBlock : Block { ... }
public struct StringSlice { string Text; int Start; int End; }
public class BlockProcessor { ... }
public class InlineProcessor { ... }
```

**New (Span-backed, stack-only)**:
```csharp
public ref struct MemoryDocument { ... }
public ref struct RefBlock { ... }
public ref struct RefContainerBlock { ... }
public ref struct RefStringView { Span<char> Data; int Start; int End; }
public ref struct RefBlockProcessor { ... }
public ref struct RefInlineProcessor { ... }
```

### Rendering Strategy

**Option A: Direct Streaming Render** (Recommended)
```csharp
public static string ToHtml(Span<char> markdown, MarkdownPipeline? pipeline = null)
{
    var memoryDoc = RefMarkdownParser.Parse(markdown, pipeline);
    
    var stringWriter = new StringWriter();
    var renderer = new HtmlRenderer(stringWriter);
    pipeline.Setup(renderer);
    
    // Render directly from memory document
    RefMarkdownRenderer.Render(memoryDoc, renderer);
    
    return stringWriter.ToString();
}
```

**Option B: Optional Materialization**
```csharp
public static MarkdownDocument ToMarkdownDocument(Span<char> markdown, MarkdownPipeline? pipeline = null)
{
    var memoryDoc = RefMarkdownParser.Parse(markdown, pipeline);
    
    // Convert to persistent document (copies/materializes content)
    return memoryDoc.Materialize(markdown);
}
```

---

## Analysis: Advantages vs Disadvantages

### Advantages ✅

1. **True Zero-Copy**
   - No string allocation required
   - Span<char> used directly throughout
   - Minimal allocation for rendering (only output HTML)
   - Potential 30-50% memory savings on large documents

2. **Memory Safety by Design**
   - Compiler enforces lifetime constraints
   - Impossible to use-after-free (ref struct nature)
   - No manual lifetime guards or unsafe code
   - Clear semantics: MemoryDocument ~= span lifetime

3. **Performance**
   - Stack allocation for parsing state (very fast)
   - Cache-friendly (stack vs heap)
   - No GC pressure during parsing
   - Potentially 20-30% faster on large documents

4. **Clean API**
   - `Markdown.ToHtml(span)` → `string`
   - `Markdown.Parse(span)` → `MemoryDocument` (if AST needed)
   - `Markdown.ToMarkdownDocument(span)` → `MarkdownDocument` (persistent)
   - Clear intent through types

### Disadvantages ❌

1. **Massive Implementation Effort**
   - **Estimated**: 40-60 hours of work
   - Parallel ref struct versions of: BlockProcessor, InlineProcessor, all Block types
   - All methods that reference these need dual implementations
   - Extensions framework would need adaptation

2. **Code Duplication**
   - Likely 5,000+ lines of duplicated logic (StringSlice variant vs RefStringView, etc.)
   - Maintenance burden: bug fixes needed in two places
   - Risk of divergence between implementations

3. **API Surface Explosion**
   - Current: `ToHtml(string)`, `Parse(string)`, etc. (9 methods)
   - New: Add `ToHtml(Span)`, `Parse(Span)`, etc. (9+ more methods)
   - Plus ref struct variants (potentially 18+ methods)
   - Cognitive load on users (which overload should I use?)

4. **Extension System Challenges**
   - Current extensions assume mutable persistent AST
   - Ref struct AST can't be extended with custom fields
   - Would need to adapt all extension architecture
   - Makes plugin model more complex

5. **Materialization Cost**
   - If you need persistent MarkdownDocument, you still need to copy everything
   - Defeats zero-copy advantage
   - Need to recreate class-based Block tree from ref struct tree

6. **Debugging & Error Handling**
   - Ref structs are harder to debug (no identity, can't inspect in debugger)
   - Stack overflow risk if parsing very large documents (stack typically much smaller than heap)
   - Error messages less helpful

7. **Backward Compatibility**
   - IMarkdownRenderer implementations expect mutable Block objects
   - Custom renderers would need to adapt or not work with MemoryDocument
   - Breaking change for extension authors

8. **Limited Reusability**
   - MemoryDocument can't be stored, cached, or reused
   - If you need the AST later, you must materialize immediately
   - Can't build a cache of parsed documents

---

## Comparison: This Approach vs Original Strategies

| Metric | String Conv (Strategy 2) | MemoryDoc Ref Structs | Owned Wrapper (Strategy 3) |
|--------|---|---|---|
| **Implementation Effort** | 4 hours | 50 hours | 30 hours |
| **Code Complexity** | Low | Very High | High |
| **Zero-Copy** | ❌ No | ✅ Yes | ✅ Yes |
| **Memory Efficiency** | Medium | High | High |
| **Type Safety** | ✅ Yes | ✅ Yes | Partial |
| **Lifetime Safety** | ✅ Automatic | ✅ Compiler-enforced | Manual, risky |
| **Backward Compatible** | ✅ 100% | 80% (extensions) | ✅ 100% |
| **Reusable Results** | ✅ Yes | ❌ No | ✅ Yes |
| **Visitor Pattern Support** | ✅ Yes | ✅ Yes | ✅ Yes |
| **Streaming Render** | ❌ Hard | ✅ Natural | Possible |
| **Recommended** | MVP now | High-perf use case | Alternative if needed |

---

## Architectural Considerations

### Problem 1: Rendering Architecture

**Current Model**:
```csharp
Block -> (IMarkdownRenderer).Render()
```

Renderer visits block tree, each block implements rendering logic.

**With MemoryDocument**:
```csharp
RefBlock -> needs different signature
```

Would need to adapt all `IMarkdownRenderer` implementations:
```csharp
interface IMarkdownRenderer
{
    // Current
    void Render(Block block);
    
    // New - would need both signatures
    void Render(RefBlock block);  // Or use different interface
}
```

This is complex because existing custom renderers wouldn't work automatically.

### Problem 2: Extension System

**Current Model**:
```csharp
public interface IBlockParser
{
    bool TryContinue(BlockProcessor processor, Block block);
}
```

For ref structs:
```csharp
public interface IBlockParser
{
    // INVALID - can't use ref struct in interface
    bool TryContinue(ref RefBlockProcessor processor, RefBlock block);
}
```

**Solution**: Would need separate extension interfaces:
```csharp
public interface IRefBlockParser
{
    bool TryContinue(ref RefBlockProcessor processor, RefBlock block);
}
```

All extensions would need dual implementations.

### Problem 3: Trivia Tracking

Current trivia model stores `List<StringSlice>` in blocks.

With ref structs:
```csharp
public ref struct RefBlock
{
    public Span<RefStringView>? TriviaBefore;  // Can't use List<T>
}
```

Would need to use Span instead of List, requiring pre-allocation or different storage model.

---

## When Each Approach Makes Sense

### Use Strategy 2 (String Conversion) If:

✅ You value **simplicity** and **compatibility**
- Just want Span API for convenience
- Happy with one string allocation
- Extensibility matters
- Multiple rendering paths possible

**Typical Workloads**:
- Web services (allocation cost << I/O time)
- Document processing where output is primary goal
- Systems with extensions needing persistent AST

### Use MemoryDoc (This Strategy) If:

✅ You have **extreme performance requirements** AND **only need HTML output**
- Parsing is the bottleneck (profiling proves it)
- Processing millions of documents
- Memory constraints are tight (embedded, mobile)
- Can live with stack-only AST limitation

**Typical Workloads**:
- High-frequency markdown rendering (blog comments, real-time chat)
- Embedded systems with limited resources
- Specialized pipelines (HTML output only, no AST reuse)

### Use Strategy 3 (Owned Wrapper) If:

⚠️ You need zero-copy AND persistent AST, but understand complexity
- Moderate performance requirements
- Need flexible rendering
- Can handle complex lifetime semantics

**Typical Workloads**:
- Hybrid systems (some HTML, some AST usage)
- When Strategy 2 profiling shows clear bottleneck AND simplicity is acceptable cost

---

## Detailed Implementation Sketch

### Core Type Definitions

```csharp
/// <summary>
/// A stack-only markdown document parsed from a Span{char} source.
/// This document lives only as long as the source span remains valid.
/// </summary>
public ref struct MemoryDocument
{
    private Span<char> _source;
    private RefBlock[] _blocks;
    public int LineCount { get; private set; }
    
    // Constructor is internal; created only by RefMarkdownParser
    internal MemoryDocument(Span<char> source)
    {
        _source = source;
        _blocks = new RefBlock[4];
    }
    
    public Span<RefBlock> Blocks => _blocks.AsSpan();
    
    /// <summary>
    /// Materializes this memory document into a persistent MarkdownDocument.
    /// This allocates strings for all trivia and content, breaking the zero-copy guarantee.
    /// </summary>
    public MarkdownDocument Materialize()
    {
        var doc = new MarkdownDocument();
        foreach (var block in _blocks)
        {
            doc.Add(MaterializeBlock(ref block));
        }
        return doc;
    }
    
    private Block MaterializeBlock(ref RefBlock refBlock)
    {
        // Convert RefBlock back to persistent Block
        // Materialize all strings from Span<char> source
        // ...implementation...
    }
}

/// <summary>
/// Stack-based view of a substring within a span.
/// Similar to StringSlice but backed by Span{char} instead of string.
/// </summary>
public ref struct RefStringView
{
    private Span<char> _data;
    public int Start { get; set; }
    public int End { get; set; }
    
    public RefStringView(Span<char> data, int start, int end)
    {
        _data = data;
        Start = start;
        End = end;
    }
    
    public char this[int index] => _data[index];
    public int Length => End - Start + 1;
    
    public ReadOnlySpan<char> AsSpan()
    {
        return _data.Slice(Start, Length);
    }
    
    public string ToString()
    {
        return new string(AsSpan());
    }
}

/// <summary>
/// Stack-based block parser processor.
/// Processes blocks line-by-line from Span{char} source.
/// </summary>
public ref struct RefBlockProcessor
{
    private Span<char> _source;
    private RefBlock[] _openedBlocks;
    private RefLineReader _lineReader;
    
    public RefStringView CurrentLine { get; set; }
    
    public void ProcessLine(RefStringView line)
    {
        // Implementation similar to current BlockProcessor
        // But works with ref structs instead of classes
    }
}

/// <summary>
/// Stack-based line reader for Span{char} sources.
/// Replaces LineReader; yields RefStringView instead of StringSlice.
/// </summary>
public ref struct RefLineReader
{
    private Span<char> _text;
    public int SourcePosition { get; set; }
    
    public RefLineReader(Span<char> text)
    {
        _text = text;
        SourcePosition = 0;
    }
    
    public RefStringView ReadLine()
    {
        // Similar logic to LineReader
        // Returns RefStringView instead of StringSlice
        // ...implementation...
    }
}

public ref struct RefBlock
{
    public int Type { get; set; }               // BlockType enum
    public Span<RefBlock> Children { get; set; }
    public RefStringView Content { get; set; }
    // Note: No List<T> for trivia; would need Span or different storage
}
```

### Parser Entry Point

```csharp
public static class RefMarkdownParser
{
    /// <summary>
    /// Parses markdown from a span into a stack-based MemoryDocument.
    /// The document is valid only while the source span remains unchanged.
    /// </summary>
    public static MemoryDocument Parse(Span<char> text, MarkdownPipeline? pipeline = null)
    {
        if (text.IsEmpty)
            return default;
        
        pipeline ??= Markdown.DefaultPipeline;
        
        var document = new MemoryDocument(text);
        var lineReader = new RefLineReader(text);
        var processor = new RefBlockProcessor();
        
        // Parse line by line
        while (true)
        {
            RefStringView line = lineReader.ReadLine();
            if (line.AsSpan().IsEmpty && lineReader.SourcePosition >= text.Length)
                break;
            
            processor.ProcessLine(line);
        }
        
        return document;
    }
}

public static class Markdown
{
    /// <summary>
    /// Converts markdown from a span to HTML.
    /// Returns only the rendered HTML; no persistent AST.
    /// This is the most efficient for pure rendering use cases.
    /// </summary>
    public static string ToHtml(Span<char> markdown, MarkdownPipeline? pipeline = null)
    {
        pipeline ??= DefaultPipeline;
        
        var memoryDoc = RefMarkdownParser.Parse(markdown, pipeline);
        
        var writer = new StringWriter();
        var renderer = new HtmlRenderer(writer);
        pipeline.Setup(renderer);
        
        RefMarkdownRenderer.Render(memoryDoc, renderer);
        writer.Flush();
        
        return writer.ToString();
    }
    
    /// <summary>
    /// Parses markdown from a span and materializes to a persistent MarkdownDocument.
    /// This allocates strings for all content, but allows AST reuse and extension.
    /// Use this only if you need to access/reuse the AST after this call.
    /// </summary>
    public static MarkdownDocument Parse(Span<char> markdown, MarkdownPipeline? pipeline = null)
    {
        var memoryDoc = RefMarkdownParser.Parse(markdown, pipeline);
        return memoryDoc.Materialize();
    }
}
```

---

## Challenges in Detail

### Challenge 1: Generic Ref Struct Constraints

```csharp
// INVALID - ref struct can't derive from class
public ref struct RefBlock : Block { }

// INVALID - ref struct can't implement interface
public ref struct RefBlockProcessor : IBlockParser { }

// SOLUTION - New type hierarchies, separate interfaces
public ref struct RefBlock { /* ... */ }

public interface IRefBlockParser
{
    bool TryContinue(ref RefBlockProcessor processor, RefBlock block);
}
```

This means all extension points need parallel versions.

### Challenge 2: Allocation Still Happens

While parsing is zero-copy, rendering still allocates:
- HTML string output (necessary)
- Temporary buffers for rendering state
- Any string operations in extensions

**Not Addressed**: Allocation during parsing itself

### Challenge 3: Stack Size Limits

```csharp
// This could cause StackOverflowException on very large documents
Span<char> largeMarkdown = new Span<char>(
    File.ReadAllText("10MB_document.md").ToCharArray()
);

var doc = RefMarkdownParser.Parse(largeMarkdown);  // ❌ Might overflow stack
```

Solutions:
- Document stack size requirements
- Provide hybrid approach (string for >10MB documents)
- Use ArrayPool<RefBlock> for large trees

### Challenge 4: Streaming Render Only

Once you call SomeOtherRenderer.Render(doc), it doesn't work:

```csharp
var memoryDoc = RefMarkdownParser.Parse(span);

// This doesn't work - Need RefMarkdownRenderer
someCustomRenderer.Render(memoryDoc);  // ❌ Type mismatch

// Have to use specific renderer
RefMarkdownRenderer.Render(memoryDoc, renderer);  // ✅
```

This limits flexibility.

---

## Implementation Phases (If Pursuing This)

### Phase 1: Core Stack-Based Types (15-20 hours)
- RefStringView struct
- RefLineReader struct
- Basic RefBlock and RefContainerBlock
- RefBlockProcessor struct

### Phase 2: Parser Logic (15-20 hours)
- RefMarkdownParser
- Implement line-by-line parsing for ref structs
- Port critical parsers (heading, paragraph, list, etc.)

### Phase 3: Rendering (10-15 hours)
- RefMarkdownRenderer
- Adapt HtmlRenderer to work with ref structs
- Streaming render implementation

### Phase 4: Materialization & Bridge (5-10 hours)
- MemoryDocument.Materialize()
- Conversion from RefBlock tree to persistent Block tree
- New Markdown.ToHtml(span) and Markdown.Parse(span) APIs

### Phase 5: Extensions & Polish (10+ hours)
- Update extension interfaces to support both paths
- Documentation
- Examples
- Testing

**Total Estimate**: 50-70 hours of focused work

---

## Recommendation

### Pragmatic Path Forward

**Immediate (1 week)**:
1. ✅ Implement Strategy 2 (string conversion overloads) - **4 hours**
2. ✅ Get user feedback on whether zero-copy is actually needed
3. ✅ Establish performance baseline with Strategy 2

**If Profiling Shows Need (subsequent month)**:
Either:
- **Option A (Likely)**: Add Strategy 4 (MemoryPool pooling) to Strategy 2
  - Effort: 8-10 hours
  - Benefit: Reduce GC pressure ~15-20%
  - Safety: Zero new complexity

- **Option B (If Strategy 2 profiling shows it's bottleneck)**:
  - Pursue MemoryDoc (this strategy)
  - Effort: 50+ hours
  - Benefit: Zero-copy, 30-50% faster, less memory
  - Cost: Significant architecture, code duplication, maintenance burden

### Current Recommendation: Start with Strategy 2

**Why**:
1. **Information before commitment**: Strategy 2 gives real performance data
2. **Low risk**: 4-hour implementation, no risk
3. **Users benefit immediately**: API is available
4. **Options remain open**: Can still do MemoryDoc later if profiling justifies

**When to Reconsider**:
- Real-world workloads with profiling showing parsing as >30% of time
- Memory constraints where zero-copy becomes critical
- User requests that justify 50+ hours of development

---

## Conclusion

This **MemoryDocument (ref struct) approach** is architecturally elegant and would deliver significant performance benefits, **but the implementation cost is high** (~50-70 hours) and the payoff is only justified if:

1. **True zero-copy is critical** (profiling proves it)
2. **HTML rendering is the primary use case** (AST persistence not needed)
3. **Code duplication burden is acceptable** (maintenance cost)

**Better sequence**:
1. **Now**: Strategy 2 (MVP, 4 hours)
2. **After profiling**: Strategy 4 if GC is issue, OR MemoryDoc if parsing is >30% of time
3. **Only then**: Full commitment to ref struct architecture

The type safety and compiler-enforced lifetime guarantees of ref structs are compelling, but they're not free—they come with architecture friction that only pays off at high-performance tiers.
