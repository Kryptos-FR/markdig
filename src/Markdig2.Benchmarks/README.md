# Markdig2 Benchmarks

## Phase 1 Baseline Performance

Early performance validation of ref struct parsing approach (Phase 1 - basic paragraph and blank line parsing only).

### Benchmark Results

**Environment:**
- .NET 10.0.1
- Intel Core i3-10110U CPU 2.10GHz
- Ubuntu 25.10

**Test Document:** 
- Basic markdown with paragraphs, blank lines, headings, and list markers (~800 characters)
- Tests basic block detection implemented in Phase 1

| Method                  | Mean     | Error     | StdDev    | Ratio | Gen0   | Allocated | Alloc Ratio |
|-------------------------|----------|-----------|-----------|-------|--------|-----------|-------------|
| Markdig (Original)      | 8.605 µs | 0.2360 µs | 0.6883 µs | 1.00  | 3.2501 | 6.66 KB   | 1.00        |
| Markdig2 (Ref Struct)   | 1.145 µs | 0.0228 µs | 0.0235 µs | 0.13  | 2.0370 | 4.16 KB   | 0.62        |

### Key Findings

**Performance Improvements (Phase 1):**
- ⚡ **7.5x faster** parsing (8.605 µs → 1.145 µs)
- 💾 **38% less memory** allocation (6.66 KB → 4.16 KB)
- 🗑️ **37% less GC pressure** (3.25 → 2.04 Gen0 collections per 1000 ops)

### Important Notes

1. **Phase 1 Limitations:** Currently only parses paragraphs and blank lines. Full Markdig supports all markdown features.

2. **Early Validation:** These results validate the zero-copy ref struct approach is viable and shows significant promise.

3. **Allocation Sources:** The 4.16 KB allocation in Markdig2 comes from:
   - `Block[]` array construction (List<Block> → array)
   - Some internal allocations during line parsing
   - Future optimization: Pre-sized arrays or ArrayPool

4. **Next Steps (Phase 2+):**
   - Add full block type parsing (headings, lists, code blocks, etc.)
   - Add inline parsing (emphasis, links, code spans)
   - Re-benchmark to ensure performance characteristics hold
   - Target: Maintain <2µs mean time and <5KB allocation for similar documents

### Running Benchmarks

```bash
cd src/Markdig2.Benchmarks
dotnet run -c Release
```

Or run specific benchmarks:
```bash
dotnet run -c Release -- --filter '*ParsingBenchmark*' --job short
```

### Benchmark History

- **2026-02-15** - Phase 1 baseline established
  - Basic paragraph/blank line parsing
  - 7.5x speed improvement, 38% memory reduction vs Markdig
