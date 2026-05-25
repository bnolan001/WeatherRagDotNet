# WeatherRagDotNet

Local-first Retrieval-Augmented Generation (RAG) over Air Force weather documentation.  
All inference and retrieval runs offline — no cloud API calls.

---

## Requirements

- .NET 8 SDK
- Windows (DirectML requires Windows for GPU acceleration)
- GPU optional — CPU-only works with `GpuLayerCount: 0` and `Threads` tuned to core count
- Intel NPU/GPU acceleration optional via an OpenVINO-capable ONNX Runtime build (with DirectML/CPU fallback)

---

## Model placement

All model files live under the `models/` directory relative to where you run the API.  
Create the directory structure and place files as follows:

```
models/
  embedding/
    model.onnx        ← ONNX export of your sentence-transformer (e.g. all-MiniLM-L6-v2)
    vocab.txt         ← BERT WordPiece vocabulary for the embedding model
  llm/
    model.gguf        ← GGUF-format LLM for answer generation (e.g. Mistral-7B-Instruct)
```

### Recommended models

| Role | Model | Source |
|------|-------|--------|
| Embeddings | `all-MiniLM-L6-v2` | [Hugging Face](https://huggingface.co/sentence-transformers/all-MiniLM-L6-v2) — export to ONNX with `optimum-cli` |
| LLM | `Mistral-7B-Instruct-v0.3.Q4_K_M.gguf` | [Hugging Face](https://huggingface.co/TheBloke/Mistral-7B-Instruct-v0.3-GGUF) |

### Exporting the embedding model to ONNX

```bash
pip install optimum[exporters]
optimum-cli export onnx --model sentence-transformers/all-MiniLM-L6-v2 models/embedding/
# Copy the generated vocab.txt to models/embedding/vocab.txt
```

---

## Configuration

`appsettings.json` controls all paths and runtime settings:

```json
{
  "Rag": {
    "DocumentStorePath": "data/documents",
    "IndexStorePath":    "data/index"
  },
  "Embedding": {
    "ModelPath":        "models/embedding/model.onnx",
    "VocabPath":        "models/embedding/vocab.txt",
    "Dimensions":       384,
    "MaxSequenceLength": 512,
    "ProviderPriority": ["OpenVINO", "DirectML", "CPU"],
    "OpenVinoDeviceType": "AUTO",
    "DirectMlDeviceId": 0,
    "EnableCpuFallback": true,
    "GraphOptimizationLevel": "ORT_ENABLE_ALL",
    "IntraOpThreads": 0,
    "InterOpThreads": 0,
    "EnableMemoryPattern": true,
    "EnableCpuMemArena": true,
    "EnableWarmup": true
  },
  "VectorStore": {
    "PersistencePath":  "data/index/vectors.json",
    "TopK":             5,
    "MinScore":         0.3
  },
  "Inference": {
    "ModelPath":      "models/llm/model.gguf",
    "ContextSize":    4096,
    "MaxTokens":      1024,
    "Temperature":    0.2,
    "GpuLayerCount":  0,
    "Threads":        8
  }
}
```

**GPU acceleration:**  
- Set `GpuLayerCount` to the number of model layers to offload to GPU (e.g. `32` for full offload on a 7B model with ≥8 GB VRAM).  
- Embedding provider policy is hardware-aware and follows `Embedding:ProviderPriority` (default `OpenVINO -> DirectML -> CPU`).
- If OpenVINO is not present in the active ONNX Runtime build, startup logs show fallback to the next provider.
- Use profile-style settings per machine:
  - `intel-hybrid`: `["OpenVINO","DirectML","CPU"]`
  - `gpu-generic`: `["DirectML","CPU"]`
  - `cpu-only`: `["CPU"]`
- Keep the same embedding model/tokenizer to preserve answer quality while tuning provider/order settings for speed.

---

## Running the API

```powershell
cd src\WeatherRag.Api
dotnet run
```

The API starts at `http://localhost:5000` with Swagger UI at `http://localhost:5000/swagger`.

---

## Indexing documents

### Via the frontend

Open `WeatherRag.Frontend\index.html` in a browser (use Live Server or similar).  
Use the **Ingest Document** panel to upload a PDF — the API extracts, chunks, embeds, and indexes it automatically.

### Via curl / Swagger

```powershell
curl -X POST http://localhost:5000/api/ingest/upload `
  -F "file=@path\to\your\weather_publication.pdf"
```

Documents are stored in `data/documents/` and the vector index is persisted to `data/index/vectors.json`.  
The index reloads automatically on API startup.

---

## Querying

```powershell
curl -X POST http://localhost:5000/api/query `
  -H "Content-Type: application/json" `
  -d '{"query": "What are the TAF amendment criteria for ceiling changes?"}'
```

Responses include:
- `answer` — grounded weather assessment in Air Force briefing style
- `citations` — source document, page number, and relevance score for each supporting passage
- `isGrounded` — `true` when the answer is backed by indexed material

---

## Running tests

```powershell
dotnet test WeatherRagDotNet.slnx
```

---

## Data directory layout (runtime)

```
data/
  documents/    ← uploaded PDFs (path-validated; no writes outside this directory)
  index/
    vectors.json ← persisted chunk embeddings and metadata
```

---

## Offline guarantee

The application makes no outbound network calls.  All inference, embedding generation, tokenization, and PDF extraction run entirely on the local machine.  Do not configure cloud endpoints in `appsettings.json`.
