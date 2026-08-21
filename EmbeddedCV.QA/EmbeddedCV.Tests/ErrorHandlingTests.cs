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
}