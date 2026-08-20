using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Reporting;

namespace EmbeddedCV.Tests;

[TestClass]
public class SummaryReportTests
{
    private static string GetModelPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");
    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestInitialize]
    public void ResetProcessorAffinity()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long fullMask = (1L << Environment.ProcessorCount) - 1;
        process.ProcessorAffinity = (IntPtr)fullMask;
    }

    [TestMethod]
    public void Generate_AfterProcessingFrames_ProducesNfrPassFailSummary()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());

        var coldStartStopwatch = System.Diagnostics.Stopwatch.StartNew();
        detector.DetectFrame(GetSampleImagePath("bus.jpg"));
        coldStartStopwatch.Stop();
        var coldStartLatency = coldStartStopwatch.Elapsed.TotalMilliseconds;

        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        runner.ProcessFrames(GetSampleImagePath("bus.jpg"), 1);
        runner.ProcessFrames(GetSampleImagePath("zidane.jpg"), 2);

        //Act
        var reportGenerator = new SummaryReportGenerator();
        var report = reportGenerator.Generate(logger.GetAllResults(), coldStartLatency);

        //Assert
        Console.WriteLine($"Cold-start latency: {report.ColdStartLatencyMs:F2}ms");
        Console.WriteLine($"Total frames: {report.TotalFrames}, Skipped: {report.SkippedFrames}");
        Console.WriteLine($"Avg latency: {report.AverageLatencyMs:F2}ms, Max: {report.MaxLatencyMs:F2}ms, Min: {report.MinLatencyMs:F2}ms");
        Console.WriteLine($"Total detections: {report.TotalDetections}");

        foreach (var nfr in report.NfrResults)
        {
            Console.WriteLine($"{nfr.RequirementId}: {(nfr.Passed ? "Passed" : "Failed")} - {nfr.Description} (actual: {nfr.ActualValue}, threshold: {nfr.Threshold})");
        }

        Assert.AreEqual(2, report.TotalFrames);
        Assert.IsTrue(report.TotalDetections > 0);
    }
}
