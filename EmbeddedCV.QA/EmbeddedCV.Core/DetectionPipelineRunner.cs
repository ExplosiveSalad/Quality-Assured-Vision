using System.Diagnostics;
using EmbeddedCV.Core.Detection;
using EmbeddedCV.Core.Logging;
using EmbeddedCV.Core.Constraints;

namespace EmbeddedCV.Core;
/*
 * Runs detection on a sequence of frames, measuring per-frame processing time and logging results.
 * Corresponds to FR-04, FR-05.
 */

public class DetectionPipelineRunner
{
    private readonly IDetector _detector;
    private readonly MetricsLogger _logger;
    private readonly ResourceConstraintSimulator? _constraintSimulator;

    public DetectionPipelineRunner(IDetector detector, MetricsLogger logger, ResourceConstraintSimulator? constraintSimulator = null)
    {
        _detector = detector;
        _logger = logger;
        _constraintSimulator = constraintSimulator;
    }

    public void ProcessFrames(string imagePath, int frameNumber)
    {
        var stopwatch = Stopwatch.StartNew();
        var frameResult = new FrameResult
        {
            FrameNumber = frameNumber,
            Timestamp = DateTime.UtcNow
        };

        try
        {
            frameResult.Detections = _detector.DetectFrame(imagePath);
            _constraintSimulator?.ApplySimulatedLatency();
        }
        catch (Exception ex)
        {
            //Corresponds to FR-10: skip corrupted/unreadable frames, log failure, continue.
            frameResult.WasSkipped = true;
            frameResult.SkippedReason = ex.Message;
        }

        stopwatch.Stop();
        frameResult.ProcessingTimeMs = stopwatch.Elapsed.TotalMilliseconds;

        _logger.LogFrame(frameResult);
    }
}
