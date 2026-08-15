using System.Globalization;
using Xunit.Abstractions;

namespace Yautbox.Benchmarks;

internal sealed record BenchmarkResult(
    string Provider,
    int Messages,
    int Pods,
    int WorkersPerPod,
    int BufferSize,
    int HandlerBatchSize,
    TimeSpan Elapsed)
{
    public double Throughput => Messages / Elapsed.TotalSeconds;
}

internal static class BenchmarkReport
{
    public static void Write(IReadOnlyCollection<BenchmarkResult> results, ITestOutputHelper output)
    {
        var lines = new List<string>
        {
            "## Yautbox processing benchmark",
            string.Empty,
            "| Provider | Messages | Pods | Workers / pod | Buffer | Handler batch | Elapsed | Messages/sec |",
            "|---|---:|---:|---:|---:|---:|---:|---:|"
        };

        lines.AddRange(results.Select(result => string.Create(
            CultureInfo.InvariantCulture,
            $"| {result.Provider} | {result.Messages:N0} | {result.Pods} | {result.WorkersPerPod} | {result.BufferSize:N0} | {result.HandlerBatchSize:N0} | {result.Elapsed} | {result.Throughput:N0} |")));

        lines.Add(string.Empty);
        lines.Add($"Runtime: {System.Runtime.InteropServices.RuntimeInformation.FrameworkDescription}; CPU: {Environment.ProcessorCount}; OS: {System.Runtime.InteropServices.RuntimeInformation.OSDescription}");

        var markdown = string.Join(Environment.NewLine, lines) + Environment.NewLine;
        output.WriteLine(markdown);
        Console.WriteLine(markdown);

        var reportPath = Environment.GetEnvironmentVariable("YAUTBOX_BENCHMARK_REPORT");
        if (!string.IsNullOrWhiteSpace(reportPath))
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(reportPath))!);
            File.WriteAllText(reportPath, markdown);
        }

        var summaryPath = Environment.GetEnvironmentVariable("GITHUB_STEP_SUMMARY");
        if (!string.IsNullOrWhiteSpace(summaryPath))
            File.AppendAllText(summaryPath, markdown);
    }
}
