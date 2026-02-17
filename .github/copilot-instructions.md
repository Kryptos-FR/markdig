# Copilot Instructions for Markdig2 Development

## Project Overview
Markdig2 is a stack-based, low-allocation markdown parser and HTML renderer with CommonMark compliance. It uses ref structs for zero-copy parsing from `Span<char>`.

**Location**: `src/Markdig2/`  
**Tests**: `src/Markdig2.Tests/`  
**Benchmarks**: `src/Markdig2.Benchmarks/`  
**Roadmap**: `research/ROADMAP_MARKDIG2.md`

---

## Core Principles

1. **Follow the Roadmap**: Development is divided into 5 phases. Check off completed items in [ROADMAP_MARKDIG2.md](research/ROADMAP_MARKDIG2.md) upon completion.

2. **Tests First**: 
   - All new code must have unit tests
   - All existing tests in `Markdig2.Tests/` must pass
   - No test regressions allowed
   - Tests validate both correctness and CommonMark compliance

3. **CommonMark Compliance**: 
   - Final implementation must pass CommonMark specification tests
   - Document any intentional deviations
   - Use CommonMark spec as source of truth for expected behavior

4. **Stack-Based Architecture**:
   - Use ref structs for AST types and processors
   - Immutable data flows during parsing
   - Direct streaming render to output
   - No persistent caching of parsed trees (by design)

---

## Development Workflow

### Before Starting Work
1. Check [ROADMAP_MARKDIG2.md](research/ROADMAP_MARKDIG2.md) for current phase
2. Identify specific tasks to complete
3. Create/update unit test cases for the feature
4. Verify no tests are currently broken: `dotnet test src/Markdig2.Tests/`

### While Implementing
- Write tests alongside code (TDD approach preferred)
- Keep ref struct constraints in mind (no interfaces, no inheritance)
- Run tests frequently: `dotnet test src/Markdig2.Tests/`
- Reference `src/Markdig/` for comparison when needed

### After Completing a Feature
1. **Update tests**: All new code must have tests. Run full suite:
   ```
   dotnet test src/Markdig2.Tests/ --verbosity normal
   ```
2. **Update Roadmap**: Check off completed items in [ROADMAP_MARKDIG2.md](research/ROADMAP_MARKDIG2.md)
3. **Verify CommonMark compliance** (if applicable): Test against CommonMark spec
4. **No regressions**: Ensure all previous tests still pass

### After Completing a Phase
1. **Update Benchmarks**: Add test cases for all new features to `src/Markdig2.Benchmarks/`
2. **Run Benchmark Suite**:
   ```
   dotnet run -c Release -p src/Markdig2.Benchmarks/
   ```
3. **Document Metrics**: Compare against previous phase and original Markdig
   - Parse time
   - Memory allocation
   - GC pressure
4. **Update Roadmap**: Mark phase as complete and document performance results
5. **No Performance Regressions**: Ensure metrics are equivalent or better than previous phase

---

## Key Constraints

### Ref Struct Limitations
- ❌ Cannot implement interfaces
- ❌ Cannot inherit from classes
- ❌ Cannot be boxed
- ❌ Cannot be stored in class fields (only `Span<T>` and other ref structs)
- ✅ Can contain `Span<T>` and other ref structs
- ✅ Can be parameters and return values

### Design Rules
- AST types: Use discriminated union pattern (`Block` struct with type enum)
- Block arrays: Index-based relationships, not Span-based
- Content access: Via `GetContent(Span<char>)` returning `RefStringView`
- Rendering: Stream directly to output, no intermediate phases

### What NOT to Do
- ❌ Don't add extension interfaces (IBlockParser, IInlineParser) in Phase 1-5
- ❌ Don't add trivia tracking without explicit scope approval
- ❌ Don't materialize to persistent MarkdownDocument in core parser
- ❌ Don't cache or store RefMarkdownDocument long-term

---

## Testing Requirements

### Unit Tests
- Test independent parsing components (RefLineReader, RefStringView, etc.)
- Test block parsers individually (headings, code blocks, lists, etc.)
- Test inline parsers independently
- Test rendering output matches expected HTML

### Integration Tests
- Parse realistic markdown documents
- Verify round-trip: parse → render produces correct HTML
- Compare output against Markdig for equivalence (when possible)
- Test edge cases: empty documents, Unicode, line endings, escaping

### CommonMark Tests
- Use CommonMark spec test cases (when implementing features)
- Document any intentional spec deviations
- Target: 100% pass rate on supported features

### Running Tests
```bash
# Run all Markdig2 tests
dotnet test src/Markdig2.Tests/

# Run specific test class
dotnet test src/Markdig2.Tests/ --filter "TestClassName"

# Run with verbose output
dotnet test src/Markdig2.Tests/ --verbosity detailed
```

---

## Roadmap Phases (Quick Reference)

| Phase | Focus | Status | Tests |
|-------|-------|--------|-------|
| 1 | Helpers, AST types, parser entry | ✅ Complete | 92+ |
| 2 | Block & inline parsing | 🔄 In Progress | Add as you implement |
| 3 | Rendering pipeline & HTML output | ⬜ Not Started | Add as you implement |
| 4 | Performance & optimization | ⬜ Not Started | Benchmarks |
| 5 | Completeness & spec compliance | ⬜ Not Started | CommonMark suite |

**Current Phase**: See [ROADMAP_MARKDIG2.md - Phase 2](research/ROADMAP_MARKDIG2.md#phase-2-core-block-parsing-2-weeks)

---

## File Organization

```
src/Markdig2/
├── Helpers/              # RefStringView, RefLineReader, CharHelper
├── Parsers/              # RefMarkdownParser, RefBlockProcessor, RefInlineProcessor
├── Renderers/            # RefMarkdownRenderer, RefHtmlRenderer
├── Syntax/               # Block, RefMarkdownDocument, ref struct types
└── Markdown2.cs          # Public API entry point

src/Markdig2.Tests/
├── TestRefStringView.cs
├── TestRefLineReader.cs
├── TestRefBlockParser.cs
├── TestRefInlineParser.cs
└── TestRefHtmlRendering.cs
```

---

## Performance Considerations

- **Phase 1 Results**: 7.5x faster, 38% less memory allocation vs Markdig
- Test with documents of increasing size (1 KB, 100 KB, 1 MB)
- Watch for stack overflow on very large documents
- Benchmark frequently to ensure optimizations are effective

---

## Useful References

- **Architecture**: [STRATEGY_MEMORY_DOCUMENT.md](research/STRATEGY_MEMORY_DOCUMENT.md)
- **Current Analysis**: [ANALYSIS_SPAN_SUPPORT.md](research/ANALYSIS_SPAN_SUPPORT.md)
- **Comparison**: [ROADMAP_MARKDIG2.md - Type Mapping](research/ROADMAP_MARKDIG2.md#type-mapping-markdig--markdig2)
- **Benchmarks**: `src/Markdig2.Benchmarks/` (run at end of each phase)
- **Original Implementation**: `src/Markdig/` (reference only)
- **CommonMark Spec**: https://spec.commonmark.org/

---

## Common Gotchas

1. **Ref struct fields**: Can only contain `Span<T>` or other ref structs—no classes
2. **Array storage**: `Block[]` works (regular struct), but `RefBlock[]` doesn't work well for ref structs
3. **String allocation**: Each `.ToString()` on `RefStringView` allocates; cache if possible
4. **Stack limits**: Very large documents might overflow stack; test progressively
5. **Lifetime tracking**: `RefMarkdownDocument` is only valid as long as source `Span<char>` is valid

---

## Quick Checklist for New Features

- [ ] Feature described in [ROADMAP_MARKDIG2.md](research/ROADMAP_MARKDIG2.md)
- [ ] Unit tests written (TDD preferred)
- [ ] All existing tests still pass
- [ ] New code has unit tests (100% coverage of new paths)
- [ ] CommonMark compliance verified (if applicable)
- [ ] Roadmap item checked off upon completion
- [ ] No performance regressions from previous phase

---

## Getting Help

If you encounter ambiguity:
1. Refer to [ROADMAP_MARKDIG2.md](research/ROADMAP_MARKDIG2.md) for phase-specific guidance
2. Check [STRATEGY_MEMORY_DOCUMENT.md](research/STRATEGY_MEMORY_DOCUMENT.md) for architectural context
3. Look at existing tests in `src/Markdig2.Tests/` for patterns
4. Compare with `src/Markdig/` for reference implementation behavior
