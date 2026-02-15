# Quick Reference: Span/Memory Support Decision Matrix

## TL;DR - Recommended Path

**Phase 1: Implement Strategy 2 (String Conversion)**
- Add simple overloads: `Parse(ReadOnlySpan<char>)`, `Parse(Memory<char>)`
- Delegate to existing `Parse(string)` after converting input
- **Effort**: 4 hours
- **Risk**: Low
- **Benefit**: Clean API, future-proof interface

**Only Consider Phase 2+ if:**
- Profiling shows span/memory parsing is a bottleneck
- Users report performance issues with zero-copy
- True zero-copy becomes critical requirement

---

## Strategy Comparison Matrix

| Aspect | Strategy 1: Parallel StringSpan | Strategy 2: String Conversion | Strategy 3: Owned Wrapper | Strategy 4: MemoryPool |
|--------|------|---------|---------|---------|
| **Implementation Effort** | Very High (20+ hours) | Low (4 hours) | Very High (30+ hours) | Medium (8-10 hours) |
| **Code Complexity** | High | Low | Very High | Medium |
| **Breaking Changes** | None | None | None | None |
| **Zero-Copy Support** | ✅ Full | ❌ No | ✅ Full | ❌ No |
| **Maintenance Burden** | High | Low | Very High | Medium |
| **Performance Benefit** | High (no copy) | Low (includes copy) | High (no copy) | Medium (reduced GC) |
| **Risk of Bugs** | Medium | Low | Very High | Low |
| **Backward Compatible** | ✅ Yes | ✅ Yes | ✅ Yes | ✅ Yes |
| **Can Be Done Later** | ✅ As replacement | ✅ No (different approach) | ✅ As replacement | ✅ Incremental opt-in |
| **Recommended For** | When zero-copy becomes 50%+ of use cases | MVP/Quick API surface | Specialized high-perf code | Benchmark-driven optimization |
| **When to Pick** | Never, in favor of Str. 4 | **NOW** ← Recommended | Only if Strategy 2 proves insufficient | After performance profiling |

---

## What Each Strategy Means

### Strategy 1: Parallel StringSpan<T>
Creates new variant of StringSlice that works with pluggable memory sources (string, Memory<char>, etc.)

**Pros**:
- Zero-copy if implemented with Owned<char> memory
- Type-safe at compile time
- Scales to future memory sources

**Cons**:
- Duplicate StringSlice logic (maintenance nightmare)
- Every parser method needs two versions
- Complex generic constraints
- Larger binary
- 20+ hours of work

**When**: Enterprise with high-volume parsing from pre-allocated buffers

---

### Strategy 2: String Conversion (⭐ RECOMMENDED)
Convert Span/Memory input to string, use existing infrastructure

**Pros**:
- 4 hours implementation
- Zero risk to existing code
- Simple to understand and maintain
- Users get expected API surface
- Can upgrade later

**Cons**:
- Allocates string (unavoidable for AST that references source)
- Not true zero-copy

**When**: Now, for all cases (it's the sensible baseline)

---

### Strategy 3: Owned Memory with Lifetime Guards
Track buffer lifetime through parsing, with guards preventing access after buffer release

**Pros**:
- True zero-copy
- AST can reference source without copies
- Potential for 40-60% performance uplift on large docs

**Cons**:
- Complex lifetime semantics (easy to misuse)
- High risk of bugs
- Requires unsafe code
- Difficult to test
- 30+ hours work
- Will likely be source of support issues

**When**: Never - Strategy 2 + 4 is better

---

### Strategy 4: MemoryPool Pooling
Use MemoryPool<char> internally to rent buffers, reducing GC spikes

**Pros**:
- Moderate implementation (from Strategy 2)
- Reduces GC pressure
- Could provide 10-20% throughput improvement
- Compatible with Strategy 2

**Cons**:
- Still requires string allocation for AST safety
- Overhead of pool rental/return
- Complex pool lifecycle management
- Less clear benefit than it appears

**When**: After Strategy 2 is done and profiling shows pool contention is an issue

---

## Decision Flow

```
Should we add Span/Memory support to MarkdownParser?
    │
    ├─ "Yes, we need it for API completeness"
    │   └─→ Use Strategy 2 (String Conversion) NOW
    │       └─→ Add overloads, convert input, done
    │
    ├─ "We need zero-copy for performance"
    │   ├─ "Do we have profiling data showing it's bottlenecked?"
    │   │   ├─ "No, just speculative"
    │   │   │   └─→ Use Strategy 2 NOW, optimize later with profiling
    │   │   │
    │   │   └─ "Yes, span parsing is 50%+ of CPU time"
    │   │       ├─ "Can users pre-convert to string for now?"
    │   │       │   └─→ Do that, document workaround, revisit
    │   │       │
    │   │       └─ "No, urgent performance requirement"
    │   │           └─→ Use Strategy 2, then evaluate Strategy 1 or 4
    │   │
    │   └─ "We have use case with pooled buffers"
    │       └─→ Start with Strategy 2, add Strategy 4 after analysis
    │
    └─ "Don't do it, no need for Span support"
        └─→ Skip entirely, it's low-impact API addition
```

---

## Implementation Timeline

### Immediate (0.5 day)
✅ **Phase 1: Strategy 2**
- Add overloads to MarkdownParser.Parse()
- Add overloads to Markdown.cs wrapper
- Create test suite
- Documentation
- **Total**: ~4 hours

### Short term (1-2 weeks, if needed)
⚠️ **Phase 2: Performance Profiling**
- Benchmark existing Strategy 2 implementation
- Profile real workloads with span input
- Identify actual performance gaps
- Document findings

### Medium term (if profiling justifies)
🔄 **Phase 2 Options**:
- **Option A**: Add Strategy 4 (MemoryPool pooling)
  - If pool contention is real problem
  - Moderate complexity, moderate benefit
  
- **Option B**: Evolve to Strategy 1 (StringSpan)
  - If zero-copy becomes critical
  - High complexity, high benefit
  - Would replace Strategy 2 API with generic
  
- **Option C**: Create specialized MemoryDocument
  - If specific, limited use case needs zero-copy
  - Alternative to generalizing entire codebase

---

## Risk Summary

| Risk | Probability | Severity | Mitigation |
|------|-------------|----------|-----------|
| String conversion is performance bottleneck | Low (unlikely) | High | Start with Strategy 2, measure first |
| Users misuse Span/Memory lifetime | Medium | High | Clear XML documentation, examples |
| Implementation bugs in new overloads | Very Low | Medium | Simple code + robust tests |
| Unmaintainable if multiple strategies co-exist | High | Medium | Commit to one strategy per component |
| AST trivia lifetime issues (Memory<char> backing) | Medium | High | Only support Strategy 2 initially |

---

## Comparison with Similar Libraries

### How do other markdown parsers handle this?

**Markdig**: Currently string-only
**Commonmark.NET**: String-only
**md4c (C)**: Pointer-based, custom lifetime management
**Rust md-rs**: Borrowed references with lifetime tracking

**Conclusion**: Most don't support Span/Memory. Markdig should be conservative (Strategy 2) and only optimize if proven necessary.

---

## FAQ

### Q: Won't the string conversion negate the benefits of the Span API?
**A**: Yes, if the main goal is zero-copy. The string conversion is a one-time cost, and the API is still valuable for:
- Convenience (doesn't require string.ToString() call)
- Future-proofing (can optimize to zero-copy later)
- Consistency with other .NET APIs

### Q: Can we do true zero-copy without lifetime guards?
**A**: Only if:
1. Input is guaranteed to outlive the AST (document)
2. Users understand and accept this constraint
3. We document it extensively

This is risky and not recommended for public API.

### Q: What's the impact on binary size?
**A**: Minimal. Adding overloads adds:
- ~5-10 KB of IL for overloads
- No significant impact on startup time
- No impact on runtime memory when using existing string path

### Q: Should we support `char*` pointers?
**A**: Yes, one overload for interop, but:
- Keep it simple (just take length, create string, delegate)
- Mark as `[RequiresUnsafeCode]`
- Document pointer lifetime requirement
- Not primary API (secondary convenience feature)

### Q: What about `IEnumerable<char>`?
**A**: No. Too inefficient. If users have IEnumerable, they should materialize to string or span first.

### Q: Can we lazy-materialize strings?
**A**: Maybe in Phase 2, but:
- Adds complexity
- Minimal gain (parser materializes immediately anyway)
- Not worth it for MVP

### Q: Will this affect the benchmark baseline?
**A**: No, unless we optimize. String path is unchanged and faster (no new method call overhead barely measurable).

---

## Success Metrics for Phase 1

- [ ] All new overloads compile cleanly
- [ ] All existing tests still pass
- [ ] New test file has >12 test cases with >80% coverage of new methods
- [ ] Documentation is clear on string conversion behavior
- [ ] Benchmark shows no regression on string path
- [ ] At least 3 usage examples in README
- [ ] Code review approved
- [ ] No new compiler warnings

## Success Metrics for Phase 2 (if pursued)
- [ ] Profiling data shows where time is spent
- [ ] Clear performance baseline established
- [ ] Decision made on Strategy 1 vs 4 vs neither
- [ ] Implementation effort estimated in detail
- [ ] Business case documented

---

## Contact / Questions

When implementing, refer back to this decision matrix. If circumstances change (new requirements, profiling data), refer to the full analysis in `ANALYSIS_SPAN_SUPPORT.md` for detailed technical discussion.
