using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Constraints;

namespace EmbeddedCV.Tests;

[TestClass]
public class ResourceConstraintTests
{
    private static string GetModelPath() =>
           Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");
    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestMethod]
    public void ProcessFrame_UnderConstrainedProfile_TakesLongerThanBaseline()
    {
        //Arrange
        using var detector = new OnnxYoloDetector(GetModelPath());
        var imagePath = GetSampleImagePath("bus.jpg");

        //Baseline run
        var baselineLogger = new MetricsLogger();
        var baselineSimulator = new ResourceConstraintSimulator();
        baselineSimulator.Apply(ResourceConstraintProfile.Baseline);
        var baselineRunner = new DetectionPipelineRunner(detector, baselineLogger, baselineSimulator);
        baselineRunner.ProcessFrames(imagePath, frameNumber: 1);

        //Constrained run
        var constrainedLogger = new MetricsLogger();
        var constrainedSimulator = new ResourceConstraintSimulator();
        constrainedSimulator.Apply(ResourceConstraintProfile.Constrained);
        var constrainedRunner = new DetectionPipelineRunner(detector, constrainedLogger, constrainedSimulator);
        constrainedRunner.ProcessFrames(imagePath, frameNumber: 1);

        //Assert
        var baselineTime = baselineLogger.GetAllResults()[0].ProcessingTimeMs;
        var constrainedTime = constrainedLogger.GetAllResults()[0].ProcessingTimeMs;

        Console.WriteLine($"Baseline: {baselineTime:F2}ms | Constrained: {constrainedTime:F2}ms");

        Assert.IsTrue(constrainedTime > baselineTime,
            "Expected constrained profile to take a bit longer than baseline.");
    }

    [TestCleanup]
    public void ResetProcessorAffinity()
    {
        var process = System.Diagnostics.Process.GetCurrentProcess();
        long fullMask = (1L << Environment.ProcessorCount) - 1;
        process.ProcessorAffinity = (IntPtr)fullMask;
    }
}
