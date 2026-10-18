# Quality Assured Vision

Testing non-functional requirements (latency, frame rate, memory, reliability) in a software-simulated embedded computer vision pipeline.

ENSE707 Software Quality Assurance, Assessment 2 (final project), AUT.

**Authors:** Mohammed Yaacoub Abou Chlih (21138726), Matty Luriz (23206450)

## What this project is

Computer vision systems are usually tested for detection accuracy, not for whether results arrive in time. This project builds a YOLOv8n detection pipeline in C#/.NET and a QA package around it, to show that a system can be functionally correct and still fail its real-time requirements.

Embedded hardware is simulated in software: a **Constrained** profile pins the process to one CPU core and adds 50ms of latency per frame, and a **HighLoad** condition processes frames concurrently.

## Key finding

All 16 MSTest methods pass, but two test cases (TC-09 and TC-16) record NFR failures: no frame reached 15 FPS, and the 100ms latency target is only met under Baseline profile with Baseline load. NFR thresholds are reported but not asserted, because absolute timings depend on the host machine.

| Profile | Load | Mean latency | NFR-01 (<100ms) | NFR-02 (15 FPS) |
|---|---|---|---|---|
| Baseline | Baseline | 78.3 ms | Pass | Fail |
| Baseline | HighLoad | 381.6 ms | Fail | Fail |
| Constrained | Baseline | 1773.0 ms | Fail | Fail |
| Constrained | HighLoad | 6078.9 ms | Fail | Fail |

Memory stays at about 180 to 238 MB against a 512 MB limit (NFR-03). Release decision: **conditionally ready** as a QA prototype, not ready as a real-time deployment. Full results are in the report.

## Repository layout

| Path | Contents |
|---|---|
| `EmbeddedCV.Core/` | Detector (`OnnxYoloDetector`), `DetectionPipelineRunner`, `ResourceConstraintSimulator`, `MetricsLogger`, `SummaryReportGenerator` |
| `EmbeddedCV.MAUI/` | .NET MAUI (Windows) GUI: pick a resource profile and load condition, run, copy the NFR report |
| `EmbeddedCV.QA/EmbeddedCV.Tests/` | MSTest suite (16 tests, 7 classes) and sample images |
| `.github/workflows/ci.yml` | GitHub Actions: build and run all tests on a Windows runner |

## Requirements

- Windows 10/11
- .NET 9 SDK
- Visual Studio 2022 with the .NET MAUI workload (only needed for the GUI)

Main packages: YoloDotNet 4.2.0, YoloDotNet.ExecutionProvider.Cpu, ONNX Runtime, SkiaSharp, MSTest.

## Run the tests

```
dotnet test EmbeddedCV.QA/EmbeddedCV.Tests/EmbeddedCV.Tests.csproj
```

Or open the solution in Visual Studio and use Test Explorer. The suite uses the real YOLOv8n model (`yolov8n.onnx`) and does not use mocks. It takes a few minutes because TC-16 runs all four profile and load combinations.

The tests run sequentially (`[assembly: DoNotParallelize]`). Constrained mode changes process-wide CPU affinity, and running tests in parallel leaked that setting between tests (DEF-02).

## Run the GUI

1. Open the solution in Visual Studio.
2. Set `EmbeddedCV.MAUI` as the startup project and target Windows Machine.
3. Select a resource profile (Baseline or Constrained) and a load condition (Baseline or HighLoad).
4. Click **Run Test**. The results box shows latency, throughput, memory and NFR pass/fail, and can be copied.

## Requirements and tests

- 11 functional requirements (FR-01 to FR-11) and 9 non-functional requirements (NFR-01 to NFR-09).
- 16 automated test cases (TC-01 to TC-16) plus 5 manual usability tests (UT-01 to UT-05).
- Defect register DEF-01 to DEF-08. Traceability matrix, root cause analysis and metrics are in the report.

## Continuous integration

Every push and pull request to `main` builds `EmbeddedCV.Tests` and runs all 16 tests on `windows-latest`, and uploads the `.trx` results as an artifact. The MAUI project is excluded because it needs the Windows desktop workload. The workflow can also be started manually from the Actions tab.

## Known limitations

- Constraints are simulated (CPU only, no memory cap), not measured on embedded hardware.
- Results come from one machine and two sample images, so detection accuracy is not verified.
- Thread-safety of the detector under HighLoad is not guaranteed.
- DEF-07 and DEF-08 (GUI defaults and option labels) are open, and DEF-01 (NFR-02) is under review.

## Licensing note

YOLOv8 models are released by Ultralytics under AGPL-3.0. Check those terms before reusing the model outside coursework.

## Final version

Tag `final-a2` on `main`.