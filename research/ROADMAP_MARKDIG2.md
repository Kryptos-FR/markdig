# Markdig2 In-Memory Implementation Roadmap

## Project Overview

**Goal**: Create a parallel implementation of Markdig using stack-based ref structs to validate zero-copy parsing approach

**Status**: Phase 2 - In Progress (2.2 Complete ✅)
**Target Framework**: .NET 10.0
**Approach**: Strategy 5 (MemoryDocument ref struct) from research analysis
**Repository**: New `src/Markdig2/` project (parallel to `src/Markdig/`)

---

## Execution Strategy

### Why Separate Project?
1. **Research/Proof of Concept**: Validate ref struct approach without disrupting production code
2. **Code Duplication Acceptable**: Easier to experiment and iterate
3. **Clear Comparison**: Can benchmark Markdig vs Markdig2 side-by-side
4. **Low Risk**: Separate project means no impact on existing users
5. **Gradual Integration**: Can merge back if validation successful

### Scope Limitations (Intentional)
- ❌ **Skip**: Full extension system (IBlockParser, IInlineParser interfaces)
- ❌ **Skip**: Custom renderer implementations
- ❌ **Skip**: Trivia/source position tracking (for Phase 1)
- ✅ **Focus**: HTML rendering pipeline + core block/inline parsing
- ✅ **Focus**: Performance comparison
- ✅ **Focus**: Memory usage measurement

---

## Project Structure

```
src/
├── Markdig/                    (Original, unchanged)
│   ├── Helpers/
│   ├── Parsers/
│   ├── Renderers/
│   ├── Syntax/
│   └── ...
│
└── Markdig2/                   (NEW - Ref struct variant)
    ├── Markdig2.csproj
    ├── Globals.cs              (Global usings/constants)
    │
    ├── Helpers/
    │   ├── RefLineReader.cs     (Stack-based line reading)
    │   ├── RefStringView.cs     (Span<char> backed text view)
    │   ├── CharHelper.cs        (Reuse from Markdig.Helpers)
    │   └── ...
    │
    ├── Parsers/
    │   ├── RefMarkdownParser.cs (Entry point, public API)
    │   ├── RefBlockProcessor.cs (Stack-based block parsing)
    │   ├── RefInlineProcessor.cs
    │   ├── RefMardownParserContext.cs
    │   └── ...
    │
    ├── Syntax/                 (AST Types - mostly ref structs)
    │   ├── RefMarkdownDocument.cs
    │   ├── Block.cs              (regular struct, not ref struct)
    │   ├── BlockType.cs          (enum)
    │   ├── Inline.cs             (regular struct, not ref struct)
    │   ├── InlineType.cs         (enum)
    │   ├── RefContainerBlock.cs  (future)
    │   ├── RefLeafBlock.cs       (future)
    │   ├── RefParagraph.cs       (future)
    │   ├── RefHeading.cs         (future)
    │   ├── RefCodeBlock.cs       (future)
    │   ├── RefQuote.cs           (future)
    │   ├── RefListBlock.cs       (future)
    │   ├── RefListItem.cs        (future)
    │   ├── RefHTMLBlock.cs       (future)
    │   ├── RefThematicBreak.cs   (future)
    │   ├── RefInlineContainer.cs (future)
    │   ├── RefLiteral.cs         (future)
    │   ├── RefEmphasis.cs        (future)
    │   ├── RefLink.cs            (future)
    │   └── ...
    │
    ├── Renderers/
    │   ├── RefMarkdownRenderer.cs      (Base streaming renderer)
    │   ├── RefHtmlRenderer.cs          (HTML output)
    │   └── RefTextWriter.cs            (Simplified writer)
    │
    └── Markdown2.cs            (Public API - similar to Markdown.cs)

tests/
└── Markdig2.Tests/             (Test suite)
    ├── Markdig2.Tests.csproj
    ├── TestRefMarkdownParser.cs
    ├── TestRefHtmlRendering.cs
    ├── TestEquivalence.cs      (Compare Markdig vs Markdig2 output)
    └── ...
```

---

## Type Mapping: Markdig → Markdig2

### Core Parsing Types

| Markdig | Markdig2 | Backing | Lifetime |
|---------|----------|---------|----------|
| `MarkdownDocument` (class) | `RefMarkdownDocument` (ref struct) | `Span<char>` | Stack-only |
| `LineReader` (struct) | `RefLineReader` (ref struct) | `Span<char>` | Stack-only |
| `StringSlice` (struct) | `RefStringView` (ref struct) | `Span<char>` | Stack-only |
| `BlockProcessor` (class) | `RefBlockProcessor` (ref struct) | N/A | Stack-only |
| `InlineProcessor` (class) | `RefInlineProcessor` (ref struct) | N/A | Stack-only |

### AST Block Types (Typical)

| Markdig | Markdig2 | Notes |
|---------|----------|-------|
| `Block` (class) | `Block` (struct) | Discriminated type (using int discriminator); regular struct not ref struct |
| `ContainerBlock` (class) | `RefContainerBlock` (ref struct) | Variant with children |
| `LeafBlock` (class) | `RefLeafBlock` (ref struct) | Variant with content |
| `Paragraph` (class) | `RefParagraph` (ref struct) | Specialized leaf block |
| `Heading` (class) | `RefHeading` (ref struct) | Specialized container |
| `CodeBlock` (class) | `RefCodeBlock` (ref struct) | Specialized leaf |
| `ListBlock` (class) | `RefListBlock` (ref struct) | Container with items |

### Renderer Types

| Markdig | Markdig2 | Notes |
|---------|----------|-------|
| N/A | `RefMarkdownRenderer` (abstract) | Streaming visitor pattern |
| `HtmlRenderer` (class) | `RefHtmlRenderer` (ref struct) | HTML streaming output |
| `TextWriter` (framework) | `RefTextWriter` (struct wrapper) | Track and write output |

---

## Implementation Phases

### Phase 1: Foundation (1 week)
**Goal**: Core infrastructure for parsing from Span<char>

#### 1.1 Helper Types (~8 hours) ✅ COMPLETED
- [x] Create `Markdig2.csproj`
- [x] `RefStringView` struct
  - Span<char> backing, Start/End indices
  - Character access, subspan operations
  - String conversion (ToString)
- [x] `RefLineReader` struct
  - Read lines from Span<char>
  - Track source positions
  - Return RefStringView for each line
- [x] ~Reuse: Copy `CharHelper.cs` from Markdig.Helpers (slight modifications)
- [x] Comprehensive unit tests (44 tests, 100% pass)

**Output**: Can read lines from a span ✅

#### 1.2 AST Base Types (~12 hours) ✅ COMPLETED
- [x] `BlockType` enum with 11 markdown block types
- [x] `Block` struct (discriminated union pattern)
  - Type enum (Paragraph, Heading, CodeBlock, Quote, List, etc.)
  - Variant storage with Data1/Data2/Data3 fields
  - Content storage via ContentStart/ContentEnd indices (indices instead of RefStringView)
  - Children storage via FirstChildIndex/ChildCount (index-based, not Span<Block>)
- [x] Factory methods: CreateParagraph, CreateHeading, CreateCodeBlock, CreateQuote, etc.
  - Helper properties: IsLeafBlock, IsContainerBlock, HeadingLevel
  - GetContent(Span<char>) returns RefStringView
- [x] `RefMarkdownDocument` ref struct
  - Flat block array architecture with index-based relationships
  - Top-level block management
  - Child navigation (GetChildren)
- [x] Comprehensive unit tests (77 total tests, 100% pass)

**Output**: Can represent parsed block tree with index-based relationships ✅

Note: `Block` is a regular struct (not ref struct) because ref structs cannot be array elements or Span<T> type parameters. Index-based approach maintains zero-copy parsing via GetContent() pattern.

#### 1.3 Parser Entry Point (~10 hours) ✅ COMPLETED
- [x] `RefMarkdownParser.Parse(Span<char>)` → `RefMarkdownDocument`
  - Static entry point for parsing
  - Returns RefMarkdownDocument tied to source span
- [x] Basic line-by-line block detection
  - Paragraph detection (consecutive non-blank lines)
  - Blank line handling (empty or whitespace-only lines)
  - Content stored as indices (ContentStart/ContentEnd)
- [x] Comprehensive unit tests (92 total tests, 100% pass)
  - Empty documents, single/multi-line paragraphs
  - Blank line handling (leading, trailing, multiple)
  - Content indices validation
  - Line ending variations (Unix, Windows, Mac)

**Output**: Can parse simple markdown (paragraph + blank lines) ✅

**Phase 1 Bonus - Early Performance Validation:**
- Created Markdig2.Benchmarks project (early from Phase 4)
- Benchmarked Phase 1 parser vs original Markdig
- **Results: 7.5x faster, 38% less memory allocation, 37% less GC pressure**
- Validates zero-copy ref struct approach is highly promising
- See [Markdig2.Benchmarks/README.md](../src/Markdig2.Benchmarks/README.md) for details

**Phase 1 Subtotal**: ~30 hours
**Milestone**: Can parse and represent simple markdown structure

---

### Phase 2: Core Block Parsing (2 weeks)
**Goal**: Implement parsers for common block types

#### 2.1 Block Parser Suite (~15 hours) ✅ COMPLETE
Implement in `RefBlockProcessor`:
- [x] Heading parser (ATX style: `# Heading`)
- [x] Thematic break parser (`---`, `***`, `___`)
- [x] Fenced code block parser (`` ``` ``)
- [x] Indented code block parser
- [x] HTML block parser (basic)
- [x] Quote/blockquote parser
- [x] List parser (unordered + ordered)
  - [x] List items
  - [x] Nested lists
  - [x] Continuation
- [x] Paragraph parser (fallback)

**Output**: Can parse real-world markdown structure

#### 2.2 Inline Parsing Setup (~8 hours) ✅ COMPLETE
- [x] `RefInlineProcessor` ref struct
- [x] `Inline` struct with discriminated union
  - Literal, Emphasis, Strong, Code, Link, Image
  - Discriminated union approach (regular struct, not ref struct)
- [x] Basic inline parsing rules
  - Emphasis delimiters (`*`, `_`)
  - Code spans (backticks)
  - Links/images (basic)
  - Hard/soft line breaks
  - HTML inline
  - Autolinks

**Output**: Can parse inline content within blocks ✅

Note: `Inline` is a regular struct (not ref struct) because ref structs cannot be array elements or Span<T> type parameters. Index-based approach maintains zero-copy parsing via GetContent() pattern, consistent with the `Block` struct design.

#### 2.3 Unit Tests (~8 hours)
- [x] Test each block parser independently (51 test cases, 128 tests total)
- [x] Test inline parsing independently (54 test cases for inline parsers)
- [ ] Test block + inline integration

**Phase 2 Subtotal**: ~31 hours
**Milestone**: Can parse typical markdown documents

---

### Phase 3: Rendering Pipeline (1 week)
**Goal**: Convert parsed tree to HTML output

#### 3.1 Renderer Base (~8 hours)
- [ ] `RefMarkdownRenderer` abstract ref struct
  - Visitor pattern for tree traversal
  - Push/pop context on recursion
- [ ] `RefTextWriter` wrapper
  - Buffered string output
  - Track position for source maps (optional)

#### 3.2 HTML Renderer (~10 hours)
- [ ] `RefHtmlRenderer : RefMarkdownRenderer`
- [ ] Render each block type
  - Headings: `<h1>` ... `</h1>`
  - Paragraphs: `<p>` ... `</p>`
  - Code blocks: `<pre><code>` ... `</code></pre>`
  - Lists: `<ul>/<ol>`, `<li>`
  - Quotes: `<blockquote>`
- [ ] Render each inline type
  - Literals
  - Emphasis: `<em>`, `<strong>`
  - Code: `<code>`
  - Links: `<a href=...>`
  - Escaping/entities

#### 3.3 Public API (~5 hours)
- [ ] `Markdown2.cs` (parallel to `Markdown.cs`)
  - `ToHtml(Span<char>)` → `string`
  - `Parse(Span<char>)` → `RefMarkdownDocument`
  - `ToPlainText(Span<char>)` → `string` (optional)

#### 3.4 Rendering Tests (~7 hours)
- [ ] HTML output tests
- [ ] Equivalence tests: `Markdown.ToHtml(string)` vs `Markdown2.ToHtml(span)`
- [ ] Real-world markdown documents

**Phase 3 Subtotal**: ~30 hours
**Milestone**: Can parse markdown and render to HTML

---

### Phase 4: Performance & Optimization (1 week)
**Goal**: Measure, profile, and optimize

#### 4.1 Benchmarking Suite (~8 hours)
- [ ] Create `Markdig2.Benchmarks` project
- [ ] Benchmark vs original Markdig
  - Parse time
  - Render time
  - Total time
  - Memory allocation (GC.GetTotalMemory)
  - Allocations per parse (via BenchmarkDotNet)
- [ ] Test documents:
  - Small (< 1 KB)
  - Medium (10-100 KB)
  - Large (> 1 MB)
  - Real-world (Reddit posts, blog posts, docs)

#### 4.2 Profiling & Analysis (~8 hours)
- [ ] Memory profiling
  - Stack depth used
  - Heap allocations during parsing
  - GC pressure
- [ ] CPU profiling
  - Hot paths in parsing
  - Inline rendering opportunities
- [ ] Identify bottlenecks

#### 4.3 Optimization (~12 hours)
- [ ] Stack allocation improvements
  - Pre-allocate block arrays
  - Optimize ref struct layouts
- [ ] Rendering optimizations
  - Buffer pooling for output
  - Reduce string allocations
- [ ] Inline parsing optimizations

#### 4.4 Comparative Analysis (~4 hours)
- [ ] Document performance vs Markdig
- [ ] Memory usage comparison
- [ ] Performance profiles

**Phase 4 Subtotal**: ~32 hours
**Milestone**: Validated performance characteristics measured

---

### Phase 5: Feature Completeness (1 week)
**Goal**: Support more markdown features

#### 5.1 Extended Features (~12 hours)
- [ ] Autolinks
- [ ] Reference links (link definitions)
- [ ] Inline HTML
- [ ] Hard line breaks
- [ ] Soft line breaks
- [ ] Strikethrough (if common)
- [ ] Tables (basic, if time permits)

#### 5.2 Edge Cases & Spec Compliance (~10 hours)
- [ ] CommonMark spec tests
- [ ] Edge case handling
- [ ] Unicode support
- [ ] Escape sequences

#### 5.3 Documentation (~8 hours)
- [ ] Architecture documentation
- [ ] Known limitations
- [ ] Performance characteristics
- [ ] Comparison with Markdig

**Phase 5 Subtotal**: ~30 hours
**Milestone**: Feature-complete for common use cases

---

## Phase Timeline

```
Week 1 (Phase 1):    Foundation - Helpers, AST types, parser entry
                     Effort: 30 hours
                     ✓ Can parse and represent structure

Week 2 (Phase 2):    Blocks & Inlines - Parsers for common types
                     Effort: 31 hours
                     ✓ Can parse real markdown

Week 3 (Phase 3):    Rendering - Convert tree to HTML
                     Effort: 30 hours
                     ✓ Can render to HTML

Week 4 (Phase 4):    Performance - Measure and optimize
                     Effort: 32 hours
                     ✓ Know performance characteristics

Week 5 (Phase 5):    Completeness - Extended features & spec compliance
                     Effort: 30 hours
                     ✓ Feature-complete for common cases

Total Effort: ~150-160 hours (4 weeks for 1 FTE, or 8 weeks for 0.5 FTE)
```

---

## Implementation Checklist

### Pre-Development
- [x] Create `src/Markdig2/` directory
- [x] Create `Markdig2.csproj` (SDK format, net10.0)
- [x] Create `tests/Markdig2.Tests/` directory
- [ ] Create `benchmarks/Markdig2.Benchmarks/` directory
- [x] Add projects to solution file
- [ ] Create CI/CD pipeline (if needed)

### Phase 1: Foundation
- [x] ImplicitUsings enabled (replaces Globals.cs)
- [x] `Helpers/RefStringView.cs`
- [x] `Helpers/RefLineReader.cs`
- [x] `Helpers/CharHelper.cs` (copied from Markdig)
- [x] `Syntax/Block.cs` (discriminated union struct)
- [x] `Syntax/BlockType.cs` (enum)
- [x] `Parsers/RefMarkdownParser.cs` (entry point)
- [x] `Syntax/RefMarkdownDocument.cs`
- [x] Unit tests (Phase 1: 92 tests)

### Phase 2: Block & Inline Parsing
- [x] `Parsers/RefBlockProcessor.cs` (Phase 2.1 complete: Block parsers)
- [x] Block parser methods (Heading, Code, Quote, List, etc.) - 51 tests
- [x] `Syntax/InlineType.cs` (enum)
- [x] `Syntax/Inline.cs` (discriminated union struct)
- [x] `Parsers/RefInlineProcessor.cs` (Phase 2.2 complete)
- [x] Inline parser methods (code, links, images, emphasis, line breaks, HTML, autolinks)
- [ ] Integration tests (block + inline combined)

### Phase 3: Rendering
- [ ] `Renderers/RefMarkdownRenderer.cs`
- [ ] `Renderers/RefTextWriter.cs`
- [ ] `Renderers/RefHtmlRenderer.cs`
- [ ] `Markdown2.cs` public API
- [ ] Rendering tests
- [ ] Equivalence tests (vs Markdig)

### Phase 4: Performance
- [ ] `Markdig2.Benchmarks` project
- [ ] Benchmark suite setup
- [ ] Run benchmarks
- [ ] Profile and optimize
- [ ] Document results

### Phase 5: Completeness
- [ ] Extended feature implementations
- [ ] Edge case handling
- [ ] CommonMark spec tests
- [ ] Documentation
- [ ] Final testing

### Post-Completion
- [ ] Performance report
- [ ] Architectural comparison
- [ ] Integration plan (if proceeding)
- [ ] Cleanup/refactor feedback

---

## Key Design Decisions

### 1. Stack-Only AST
**Decision**: `RefMarkdownDocument` cannot outlive the source `Span<char>`
**Rationale**: Compiler-enforced safety; no complex lifetime management
**Implication**: Must render or materialize during parsing; can't cache

### 2. Discriminated Union for Block Types
**Decision**: Single `Block` (regular struct) with type enum + variant storage
**Rationale**: Simpler than separate types; can be stored in arrays (Block[])
**Alternative**: Parallel ref struct types (RefParagraph, RefHeading, etc.) - more type-safe but cannot be stored in arrays

```csharp
public struct Block  // Regular struct, not ref struct
{
    public BlockType Type;        // enum
    public int ContentStart;      // Index into source
    public int ContentEnd;        // Index into source
    public int FirstChildIndex;   // Index-based children
    public int ChildCount;
    // ... other shared fields
}

enum BlockType { Paragraph, Heading, CodeBlock, Quote, ... }
```

### 3. Simplified Extension System (Phase 1)
**Decision**: No custom parser interfaces for MVP
**Rationale**: Reduces complexity; can add later
**Scope**: Core block/inline parsing only

### 4. Streaming Rendering Only
**Decision**: Render directly to output (TextWriter), no separate renderer phase
**Rationale**: Natural for stack-based processing; minimizes allocations
**Implication**: Can't visit tree multiple times without reparsing

### 5. No Trivia Tracking (Phase 1)
**Decision**: Skip LinesBefore, LinesAfter, TriviaBefore, TriviaAfter
**Rationale**: Simplifies Phase 1; can add if time permits
**Scope**: Will track them if time allows (Phase 5)

### 6. Type Naming Convention
**Decision**: Prefix with "Ref" (`RefBlock`, `RefMarkdownParser`, etc.)
**Rationale**: Clear distinction in code; easier to keep in sync with Markdig; facilitates future merge
**Example**: `RefMarkdownDocument` vs `MarkdownDocument`

---

## Risk Mitigation

### Risk 1: Stack Overflow on Large Documents
**Mitigation**: 
- Test with progressively larger documents
- Document stack requirements
- Provide hybrid approach (fall back to string if needed)
- Consider ArrayPool for very large trees

### Risk 2: Performance Not Better Than Markdig
**Mitigation**:
- Benchmark early and often
- Profile against Markdig at Phase 2
- If no benefit, reevaluate approach
- Decision gate at Phase 4

### Risk 3: Code Duplication Becomes Unmaintainable
**Mitigation**:
- Extract common logic to shared assembly
- Document differences clearly
- Plan for eventual consolidation
- Code review checkpoints

### Risk 4: Ref Struct Limitations Cause Issues
**Mitigation**:
- Document constraints clearly
- Test edge cases thoroughly
- Have fallback to string-based approach

### Risk 5: Extension Authors Need Support
**Mitigation**:
- Phase 1 explicitly excludes extension points
- Document limitation clearly
- Plan extension system for Phase 2 of Markdig2 (post-research)

---

## Success Criteria

### Phase 1 (Foundation)
✅ Can read markdown from Span<char> without allocating strings
✅ Can represent parsed structure as ref structs on stack
✅ All tests pass
✅ No runtime errors with edge cases

### Phase 2 (Parsing)
✅ Can parse realistic markdown documents
✅ Output matches or exceeds spec compliance
✅ Tests demonstrate feature parity with Markdig for core features
✅ No stack overflow on documents up to 1 MB

### Phase 3 (Rendering)
✅ HTML output matches Markdig for same inputs
✅ Rendering completes without allocations
✅ Tests verify HTML spec compliance (basics)
✅ Public API is clean and intuitive

### Phase 4 (Performance)
✅ Benchmarks show memory allocation reduction vs Markdig
✅ Parse time competitive with or better than Markdig
✅ Stack usage is predictable and documented
✅ Performance profiles identify any unexpected hot paths

### Phase 5 (Completeness)
✅ All common markdown features supported
✅ CommonMark spec tests pass for supported features
✅ Known limitations clearly documented
✅ Integration plan drafted (if pursuing merge)

---

## Dependencies & Resources

### External Resources
- .NET 8.0+ SDK (required)
- BenchmarkDotNet (for performance testing)
- xUnit (test framework, optional - use Markdig's approach)

### Research Dependencies
- `research/ANALYSIS_SPAN_SUPPORT.md` - Architecture context
- `research/STRATEGY_MEMORY_DOCUMENT.md` - Ref struct approach details
- Original `src/Markdig/` codebase - Reference implementation

### Code Reuse
- Can copy helper methods from Markdig.Helpers
- Can reuse test patterns from Markdig.Tests
- Can leverage CommonMark spec tests

---

## Decisions & Gates

### Decision Gate: Phase 2 → Phase 3
**Question**: Is stack-based parsing viable for realistic documents?
**Success Criteria**: Parse 1 MB markdown without issues; no stack overflows
**If Failed**: Reevaluate approach, consider hybrid method

### Decision Gate: Phase 3 → Phase 4
**Question**: Can we render correctly to HTML?
**Success Criteria**: Output matches Markdig for 100+ test cases
**If Failed**: Debug rendering logic; possibly proceed to Phase 4 for profiling

### Decision Gate: Phase 4 → Phase 5
**Question**: Does the approach deliver performance benefits?
**Metric**: Memory allocation reduced by ≥20% on large docs (>100 KB)
**If Failed**: Document findings; discuss ROI of continued development
**If Passed**: Continue to completeness

### Final Decision: Integration with Markdig
**Question**: Should we merge Markdig2 into Markdig?
**Criteria**:
- Phase 5 complete with good performance
- Extension system viable for real-world use
- Community/maintainer interest
- Integration complexity acceptable

---

## Known Limitations (Intentional for Phase 1-5)

- ❌ Custom extension parsers (IBlockParser interface) - Phase 2 or later
- ❌ Multiple renderers - HTML only for Phase 1-3
- ❌ Trivia tracking - Phase 5 (if time)
- ❌ Source position tracking - Post-Phase 5
- ❌ Normalization mode - Post-Phase 5
- ❌ Ad-hoc inline parsing attributes - Phase 2+

---

## Next Steps

1. **Approve Roadmap** - Get sign-off on scope and timeline
2. **Create Project Structure** - Set up `Markdig2/` project
3. **Phase 1 Kickoff** - Start implementation
4. **Weekly Check-ins** - Assess progress vs Phase timeline
5. **Decision Gates** - Evaluate after each phase
6. **Final Review** - Compare with original Markdig before any integration discussions

---

## Appendix: Reference Documents

- **[research/STRATEGY_MEMORY_DOCUMENT.md](../research/STRATEGY_MEMORY_DOCUMENT.md)** - Detailed ref struct approach
- **[research/ANALYSIS_SPAN_SUPPORT.md](../research/ANALYSIS_SPAN_SUPPORT.md)** - Current architecture analysis
- **[research/EXECUTIVE_SUMMARY.md](../research/EXECUTIVE_SUMMARY.md)** - High-level overview
- **[research/IMPLEMENTATION_GUIDE_PHASE1.md](../research/IMPLEMENTATION_GUIDE_PHASE1.md)** - String conversion approach (for reference)

---

## Document History

- **2026-02-15**: Initial roadmap created
  - Scope: 5 phases over 4 weeks (150-160 hours)
  - Focus: Validation and performance measurement
  - Status: Planning
- **2026-02-15**: Phase 1.1 completed
  - RefStringView and RefLineReader implemented
  - 44 unit tests with 100% pass rate
  - Project targeting .NET 10.0
  - Status: Phase 1 in progress
- **2026-02-17**: Phase 1 and Phase 2.1, 2.2 completed
  - Phase 1: 92 total tests, all passing
  - Phase 2.1: Block parsers (Heading, Code, Quote, List, ThematicBreak, HTML, Indented code)
  - Phase 2.2: Inline parsers (Code spans, Links, Images, Emphasis, Strong, Line breaks, HTML tags, Autolinks)
  - Total tests: 146 passing (92 from Phase 1 + 51 block + 54 inline = 197 tests but some combined)
  - Inline struct uses discriminated union pattern (regular struct, not ref struct)
  - RefInlineProcessor fully implements basic inline parsing
  - Status: Phase 2.2 complete, moving to Phase 2.3 (block + inline integration)
