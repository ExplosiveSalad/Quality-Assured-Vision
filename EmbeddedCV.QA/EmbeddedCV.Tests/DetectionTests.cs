using Microsoft.VisualStudio.TestTools.UnitTesting;
using EmbeddedCV.Core.Detection;

namespace EmbeddedCV.Tests;

[TestClass]
public class DetectionTests
{
    private static string GetModelPath() =>
        Path.Combine(AppContext.BaseDirectory, "Assets", "models", "yolov8n.onnx");

    private static string GetSampleImagePath(string fileName) =>
        Path.Combine(AppContext.BaseDirectory, "SampleData", fileName);

    [TestMethod]
    public void DetectFrame_OnBusImage_ReturnsDetections()
    {
        //Arrange
        var modelPath = GetModelPath();
        var imagePath = GetSampleImagePath("bus.jpg");
        using var detector = new OnnxYoloDetector(modelPath);

        //Act
        var results = detector.DetectFrame(imagePath);

        //Assert
        Assert.IsNotNull(results);
        Assert.IsTrue(results.Count > 0, "Expected at least one detection in bus.jpg");

        foreach (var r in results)
        {
            Console.WriteLine($"{r.Label} - {r.Confidence:P1} at ({r.X},{r.Y}) {r.Width}x{r.Height}");
        }
    }
}