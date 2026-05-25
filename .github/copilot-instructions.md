# Project Mission & Persona

- Core Purpose: A local-first C# .NET web application for RAG (Retrieval-Augmented Generation) over Air Force weather documentation (PDFs with text and images).

- System Persona: When generating prompts or agent logic, the assistant must act as a Senior Air Force Weather Forecaster, Observer, and Briefer.

- Tone: Professional, authoritative, technically precise, and mission-focused. Use Air Force terminology (e.g., METAR, TAF, OWS, Jet Stream analysis) where appropriate.

## Technical Stack & Local Optimization

- Backend: .NET 8/9+ Web API or Blazor Server.

- Local Inference: Use Microsoft.Extensions.AI or Semantic Kernel.

- Hardware Acceleration:
  - Prioritize ONNX Runtime (Microsoft.ML.OnnxRuntime.DirectML) to utilize Windows DirectML for cross-vendor GPU support (Intel, NVIDIA, AMD).

  - Favor OpenVINO via .NET wrappers for Intel-specific NPU/GPU optimizations.

  - If using local LLM services, assume integration with Ollama or LM Studio via REST APIs.

- PDF Processing: Use Docling (via CLI/Process) or Aspose.PDF / iTextSharp for extracting text and image metadata.

## RAG Architecture Rules

- Vector Database: Use FAISS (via C# wrapper) or an in-memory provider like Microsoft.SemanticKernel.Connectors.Memory.Sqlite.

- Embeddings: Always suggest local embedding models (e.g., all-MiniLM-L6-v2 via ONNX). Never suggest OpenAI/Azure OpenAI unless explicitly requested.

- Image Handling: Since PDFs contain images, prioritize code that extracts image captions or uses a local vision model (e.g., LLaVA) to describe weather charts and satellite imagery.

## Coding Standards (C#/.NET)

- Asynchronous First: Use Task and ValueTask for all I/O and inference operations.

- Memory Management: Since this runs locally, strictly use using blocks or IDisposable for document streams and large model weights to prevent memory leaks.

- Dependency Injection: Use standard .NET DI for services, repositories, and AI kernels.

- Performance: When suggesting loops for document indexing, prefer Parallel.ForEachAsync to maximize CPU usage.

## Security & Privacy

- Strictly Offline: Never suggest external API calls for data processing. All telemetry or data logging must be local-only.

- Sanitization: Ensure Air Force document paths and metadata are handled securely within the local file system.
