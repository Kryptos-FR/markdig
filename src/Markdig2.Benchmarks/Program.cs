// Copyright (c) Alexandre Mutel. All rights reserved.
// This file is licensed under the BSD-Clause 2 license.
// See the license.txt file in the project root for more information.

using BenchmarkDotNet.Running;
using BenchmarkDotNet.Configs;
using BenchmarkDotNet.Exporters;
using BenchmarkDotNet.Exporters.Csv;
using BenchmarkDotNet.Loggers;
using Markdig2.Benchmarks;

// Display available benchmark suites
if (args.Length == 0 || args.Contains("--help") || args.Contains("-h"))
{
    Console.WriteLine("Markdig2 Benchmark Suite");
    Console.WriteLine("========================");
    Console.WriteLine();
    Console.WriteLine("Available benchmark suites:");
    Console.WriteLine("  1. parsing        - Original Phase 3 comprehensive parsing benchmark");
    Console.WriteLine("  2. sizes          - Document size benchmarks (small, medium, large)");
    Console.WriteLine("  3. realworld      - Real-world document patterns (Reddit, blogs, READMEs)");
    Console.WriteLine("  4. all            - Run all benchmarks");
    Console.WriteLine();
    Console.WriteLine("Usage:");
    Console.WriteLine("  dotnet run -c Release -- <suite>");
    Console.WriteLine();
    Console.WriteLine("Examples:");
    Console.WriteLine("  dotnet run -c Release -- parsing");
    Console.WriteLine("  dotnet run -c Release -- sizes");
    Console.WriteLine("  dotnet run -c Release -- all");
    Console.WriteLine();
    Console.WriteLine("You can also use standard BenchmarkDotNet arguments:");
    Console.WriteLine("  dotnet run -c Release -- --filter '*Small*'");
    Console.WriteLine("  dotnet run -c Release -- --job short");
    Console.WriteLine();
    Console.WriteLine("Results are saved in timestamped folders under BenchmarkDotNet.Artifacts/");
    return;
}

// Create a timestamped artifacts path for this run
var timestamp = DateTime.Now.ToString("yyyy-MM-dd_HH-mm-ss");
var artifactsPath = Path.Combine("BenchmarkDotNet.Artifacts", timestamp);

// Create custom config with timestamped output path
var config = DefaultConfig.Instance
    .WithArtifactsPath(artifactsPath)
    .AddExporter(HtmlExporter.Default)
    .AddExporter(MarkdownExporter.GitHub)
    .AddExporter(CsvExporter.Default);

var suite = args[0].ToLowerInvariant();

Console.WriteLine($"Results will be saved to: {artifactsPath}");
Console.WriteLine();

switch (suite)
{
    case "parsing":
        Console.WriteLine("Running Phase 3 Parsing Benchmark...");
        BenchmarkRunner.Run<ParsingBenchmark>(config);
        break;

    case "sizes":
        Console.WriteLine("Running Document Size Benchmarks...");
        BenchmarkRunner.Run<DocumentSizeBenchmarks>(config);
        break;

    case "realworld":
        Console.WriteLine("Running Real-World Document Benchmarks...");
        BenchmarkRunner.Run<RealWorldBenchmarks>(config);
        break;

    case "all":
        Console.WriteLine("Running All Benchmark Suites...");
        Console.WriteLine("All results will be saved to the same timestamped folder.");
        Console.WriteLine();

        BenchmarkRunner.Run<ParsingBenchmark>(config);
        Console.WriteLine();

        BenchmarkRunner.Run<DocumentSizeBenchmarks>(config);
        Console.WriteLine();

        BenchmarkRunner.Run<RealWorldBenchmarks>(config);
        break;

    default:
        // Pass through to BenchmarkDotNet for filtering
        Console.WriteLine("Running with BenchmarkDotNet arguments...");
        BenchmarkSwitcher.FromAssembly(typeof(Program).Assembly).Run(args, config);
        break;
}

Console.WriteLine();
Console.WriteLine($"✅ Benchmark results saved to: {artifactsPath}");
Console.WriteLine($"   - HTML reports for easy viewing");
Console.WriteLine($"   - Markdown reports for documentation");
Console.WriteLine($"   - CSV exports for data analysis");
