using EmbeddedCV.Core;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;

[TestClass]
public class ErrorHandlingTests
{
    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void Constructor_WithMissingModel_ThrowsFileNotFoundException()
    {
        _ = new OnnxYoloDetector("nonexistent-model.onnx");
    }

    [TestMethod]
    [ExpectedException(typeof(FileNotFoundException))]
    public void ProcessBatch_WithMissingInputFile_RejectsRunBeforeExecution()
    {
        using var detector = new OnnxYoloDetector(Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx"));
        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        runner.ProcessBatch(new List<string> { "nonexistent-image.jpg" }, EmbeddedCV.Core.Constraints.LoadCondition.Baseline);
    }

    [TestMethod]
    public void ProcessFrame_WithCorruptedImageFile_SkipsFrameAndContinuesLogging()
    {
        //Arrange: create a fake corrupted file (valid path, invalid image content)
                var corruptedPath = Path.Combine(Path.GetTempPath(), "corrupted-test.jpg");
        File.WriteAllText(corruptedPath, "this is not a real image file");

        using var detector = new OnnxYoloDetector(Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx"));
        var logger = new MetricsLogger();
        var runner = new DetectionPipelineRunner(detector, logger);

        //Act: Process the corrupted image
        runner.ProcessFrames(corruptedPath, frameNumber: 1);

        //Assert: Check that the logger has recorded a frame result with an error
        var result = logger.GetAllResults()[0];
        Assert.IsTrue(result.WasSkipped, "Expected corrupted frame to be marked as skipped, not crash the run");
        Assert.IsFalse(string.IsNullOrEmpty(result.SkippedReason));

        File.Delete(corruptedPath); // Clean up the temporary corrupted file for the next run
    }
}