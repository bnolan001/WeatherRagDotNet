# WeatherRagDotNet Copilot Instructions

## Build, test, and lint commands

Use solution/project-root commands unless a command explicitly changes directory.

| Purpose | Command |
|---|---|
| Restore .NET dependencies | `dotnet restore WeatherRagDotNet.slnx` |
| Build all projects | `dotnet build WeatherRagDotNet.slnx` |
| Run all tests | `dotnet test WeatherRagDotNet.slnx` |
| Run one test method | `dotnet test tests\WeatherRag.Tests\WeatherRag.Tests.csproj --filter "FullyQualifiedName~WeatherRag.Tests.Rag.OnnxSessionFactoryTests.BuildProviderPriority_DefaultsToOpenVinoDirectMlAndCpu"` |
| Run API locally | `dotnet run --project src\WeatherRag.Api\WeatherRag.Api.csproj` |
| Frontend TypeScript compile check | `cd WeatherRag.Frontend; npm ci; npx tsc -p tsconfig.json` |

There is no dedicated lint script configured in this repository; use the TypeScript compile check above for frontend static checks.

## High-level architecture

- **Composition root:** `src/WeatherRag.Api/Program.cs` wires `AddRagServices(...)` and `AddInferenceServices(...)`, enables Swagger/CORS, then eagerly loads persisted vectors (`IVectorStore.LoadAsync`) and runs embedding warmup (`IEmbeddingWarmupService.WarmupAsync`) before serving requests.
- **RAG ingestion flow:** `IngestController` accepts PDF uploads, writes them under `Rag:DocumentStorePath`, then calls `DocumentIngestionService` to extract pages (`PdfPigExtractor`), chunk text (`TextChunkingService`), generate embeddings (`OnnxEmbeddingService`), and persist to `InMemoryVectorStore` JSON (`VectorStore:PersistencePath`).
- **Query flow:** `QueryController` runs retrieval first (`IRetrievalService`), then generation (`IInferenceService`), and returns grounded response metadata (`answer`, citations, `isGrounded`, elapsed time).
- **Inference flow:** `LlamaSharpInferenceService` resolves model profiles from `Inference:Models`, caches `LLamaWeights` per model id, and builds prompts through `WeatherBrieferPromptBuilder`.
- **Frontend contract:** `WeatherRag.Frontend/src/app.ts` calls `/api/models`, `/api/ingest/upload`, `/api/query`, and `/api/health/index` against `http://localhost:5000`; UI assumes the API is running with CORS enabled.

## Key conventions (repository-specific)

- **Mission persona is mandatory in prompts:** generation prompts must preserve the Senior Air Force Weather Forecaster/Observer/Briefer role and the “insufficient data” fallback language in `WeatherBrieferPromptBuilder`.
- **Offline-first only:** keep all retrieval/inference local; do not introduce cloud AI endpoints unless explicitly requested.
- **Execution provider policy is ordered and normalized:** embedding provider selection is driven by `Embedding:ProviderPriority` with alias normalization (`DML` -> `DirectML`) and optional forced CPU fallback (`OnnxSessionFactory.BuildProviderPriority`).
- **DirectML safety constraint:** `OnnxEmbeddingService` serializes ONNX session `Run(...)` calls with a semaphore because shared-session DirectML inference is treated as non-thread-safe.
- **Path hardening is part of ingestion behavior:** ingestion sanitizes paths with `Path.GetFullPath` and rejects files outside `Rag:DocumentStorePath`; controllers also strip file names via `Path.GetFileName`.
- **Reindex behavior is replace-by-source:** ingest always removes existing chunks for the same source file before add/save; delete path uses the same remove/save persistence pattern.
- **Chunk provenance format matters across layers:** chunk IDs follow `{file}_p{page}_c{index}`, include page/source metadata, and attach page images only to the first chunk from that page.
