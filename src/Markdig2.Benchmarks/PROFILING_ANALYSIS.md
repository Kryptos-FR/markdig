# Phase 4.2: Allocation Profiling & Analysis

## Problem Statement

Benchmark results from Phase 4.1 show **WORSE allocation performance** on real-world documents:

| Document Type | Markdig Alloc | Markdig2 Alloc | Ratio | Issue |
|--------------|---------------|----------------|-------|-------|
| Reddit Post | 25.52 KB | 37.84 KB | **1.48x MORE** | ❌ Critical |
| Blog Post | 52.18 KB | 71.2 KB | **1.36x MORE** | ❌ Critical |
| Tech Doc | 111.82 KB | 184.3 KB | **1.65x MORE** | ❌ Critical |
| Large Doc | 10,091 KB | 10,858 KB | **1.08x MORE** | ⚠️ Significant |
| Medium Doc | 271.33 KB | 241.42 KB | 0.89x less | ✅ Good |
| README | 68.88 KB | 64.87 KB | 0.94x less | ✅ Good |
| Small Doc | 5.24 KB | 5.31 KB | 1.01x more | ⚠️ Minor |
| Parsing (Phase 3) | 24.84 KB | 13.74 KB | 0.55x less | ✅ Excellent |

**Time Performance:** Still good (30-40% faster on most tests)
**Memory Performance:** REGRESSED on complex documents ❌

## Initial Hypotheses

### Hypothesis 1: Inline Array Allocations
**Theory:** The inline parsing creates many `List<Inline>` allocations during parsing.
- Each leaf block requires a temporary `List<Inline>` during parsing
- These get converted to arrays and stored in the document
- More complex documents = more leaf blocks = more temporary lists

**Evidence:** 
- Reddit post has many paragraphs with inline formatting
- Blog post has extensive inline content (emphasis, links, code spans)
- Tech doc has the most complex inline structure

**Test:** Run `AllocationProfilingBenchmark` to isolate inline parsing allocations.

### Hypothesis 2: Block Array Resizing
**Theory:** `List<Block>` grows dynamically during parsing, causing reallocations.
- Each reallocation copies the entire array
- Complex documents have many blocks

**Evidence:**
- Problem scales with document complexity
- Medium doc (fewer blocks) performs better

**Test:** Profile block parsing phase separately.

### Hypothesis 3: String Builder in Rendering
**Theory:** HTML rendering uses `StringBuilder` which allocates backing arrays.
- Initial capacity may be too small
- Repeated resizing causes allocations

**Evidence:**
- Problem appears in full pipeline (ToHtml) but less in parse-only

**Test:** Profile rendering phase separately.

### Hypothesis 4: Char Array Conversion
**Theory:** Converting `string` to `char[]` for span processing allocates.
- `_redditPost.ToCharArray()` creates a copy
- This is done once per benchmark iteration

**Evidence:**
- Only affects span-based API
- String API wouldn't have this problem

**Test:** Compare string vs span API allocations.

### Hypothesis 5: Inline Children Not Optimized
**Theory:** Links, emphasis, and images allocate for child inline storage.
- Complex inline structures (emphasis containing links, etc.) require child arrays
- Original Markdig may use more efficient storage

**Evidence:**
- Documents with complex inline nesting show worse performance
- Tech doc has very complex inline structures

**Test:** Analyze inline child storage patterns.

## Profiling Plan

### Step 1: Run AllocationProfilingBenchmark
```bash
dotnet run -c Release -- --filter '*AllocationProfilingBenchmark*'
```

This will isolate:
- Full pipeline allocations
- Parse-only allocations
- Block parsing allocations
- Inline parsing allocations
- Rendering-only allocations
- Char array creation overhead

### Step 2: Analyze Memory Diagnoser Output
Look for:
- Which phase contributes most allocations
- Gen0/Gen1/Gen2 collection patterns
- Allocation hotspots

### Step 3: Source Code Analysis
Examine:
- List<T> initialization and capacity settings
- Array conversions and copies
- StringBuilder initial capacity
- Inline child storage patterns

### Step 4: Compare with Markdig Implementation
Understand how original Markdig achieves better allocation profile:
- Object pooling?
- Pre-sized collections?
- Different data structures?
- Lazy initialization?

## Findings

### Finding 1: [To be filled after profiling]

**What:** 

**Where:** 

**Why:** 

**Impact:** 

**Fix:** 

---

### Finding 2: [To be filled after profiling]

**What:** 

**Where:** 

**Why:** 

**Impact:** 

**Fix:** 

---

## Action Items for Phase 4.3

Based on profiling results, we will:

1. [ ] Optimize allocation hotspot #1
2. [ ] Optimize allocation hotspot #2  
3. [ ] Optimize allocation hotspot #3
4. [ ] Re-benchmark after each fix
5. [ ] Document final allocation improvements

## Success Criteria

Phase 4.2 is complete when:
- [x] Allocation profiling benchmarks created
- [ ] Profiling benchmarks executed
- [ ] Top 3 allocation sources identified
- [ ] Root causes documented
- [ ] Optimization plan created for Phase 4.3

Target for Phase 4.3:
- Reddit Post: Reduce from 37.84 KB to <26 KB (match or beat Markdig)
- Blog Post: Reduce from 71.2 KB to <53 KB
- Tech Doc: Reduce from 184.3 KB to <112 KB
