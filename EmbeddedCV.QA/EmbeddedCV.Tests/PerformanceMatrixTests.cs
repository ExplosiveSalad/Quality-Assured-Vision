using System.Diagnostics;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Constraints;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Reporting;


namespace EmbeddedCV.Tests;

[TestClass]
public class PerformanceMatrixTests
{
    private const int FramesPerCell = 10;
    private static string GetModelPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");
    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestInitialize]
    [TestCleanup]
    public void ResetProcessorAffinity()
    {
        var process = Process.GetCurrentProcess();
        long fullMask = (1L << Environment.ProcessorCount) - 1;
        process.ProcessorAffinity = (IntPtr)fullMask;
    }

    [TestMethod]
    public void PerformanceMatrix_AllProfileAndLoadCombinations_CompleteAndReportNfrResults()
    {
        using var detector = new OnnxYoloDetector(GetModelPath());

        var images = Enumerable.Range(0, FramesPerCell)
            .Select(i => GetSampleImagePath(i % 2 == 0 ? "bus.jpg" : "zidane.jpg"))
            .ToList();

        var profiles = new[] { ResourceConstraintProfile.Baseline, ResourceConstraintProfile.Constrained };
        var loads = new[] { LoadCondition.Baseline, LoadCondition.HighLoad };

        foreach (var profile in profiles)
        {
            foreach (var load in loads)
            {
                var simulator = new ResourceConstraintSimulator();
                simulator.Apply(profile);
                detector.DetectFrame(images[0]); // Warm-up under this profile, not recorded

                var logger = new MetricsLogger();
                var runner = new DetectionPipelineRunner(detector, logger, simulator);

                var wall = Stopwatch.StartNew();
                runner.ProcessBatch(images, load);
                wall.Stop();

                var report = new SummaryReportGenerator().Generate(logger.GetAllResults());
                string Nfr(string id)
                {
                    var n = report.NfrResults.First(r => r.RequirementId == id);
                    return $"{(n.Passed ? "PASS" : "FAIL")} ({n.ActualValue})";
                }

                var throughput = FramesPerCell / (wall.Elapsed.TotalMilliseconds / 1000.0);
                Console.WriteLine(
                    $"{profile.Name} | {load} | avg latency: {report.AverageLatencyMs:F1}ms | " +
                    $"Wall {wall.Elapsed.TotalMilliseconds:F0}ms | throughput: {throughput:F1} FPS | " +
                    $"NFR-01 {Nfr("NFR-01")} | NFR-02 {Nfr("NFR-02")} | NFR-03 {Nfr("NFR-03")} | " +
                    $"skipped {report.SkippedFrames}");

                //Assert completion only. Latency/FPS thresholds are reported, not asserted,
                //because absolute timings vary based on the host machine and would make CI unreliable.
                Assert.AreEqual(FramesPerCell, report.TotalFrames);
                Assert.AreEqual(0, report.SkippedFrames);
            }
        }
    }
}
