# WeatherRagDotNet implementation plan

## Objective
Build a local-first .NET application for retrieval-augmented generation over Air Force weather PDFs. The solution must remain offline, use local inference, preserve document provenance for grounded answers, and reflect the persona and constraints defined in `.github/copilot-instructions.md`.

## Repository assessment
- The repository is currently a greenfield workspace.
- Existing content is limited to Copilot guidance under `.github\`.
- There is no solution file, no application code, no frontend, no tests, and no existing RAG or inference implementation.

## Confirmed architecture decisions
- **Backend:** ASP.NET Core Web API
- **Frontend:** Separate plain HTML/TypeScript/CSS project
- **Core projects:** Separate RAG engine and inference engine projects
- **Inference runtime:** LlamaSharp as the default local runtime
- **Embeddings:** Local ONNX model via ONNX Runtime DirectML
- **Vector store:** FAISS-based local vector storage
- **PDF strategy:** Pure-.NET PDF extraction path for the first milestone
- **Tone/persona:** Senior Air Force Weather Forecaster, Observer, and Briefer

## Proposed solution layout
Use a minimal multi-project structure so each responsibility is clear without introducing speculative abstractions.

```text
src\
  WeatherRag.Api\           ASP.NET Core Web API host and orchestration endpoints
  WeatherRag.Frontend\      Static HTML/TypeScript/CSS client
  WeatherRag.Rag\           Chunking, indexing, retrieval, citations
  WeatherRag.Inference\     LlamaSharp integration, prompt building, response generation
tests\
  WeatherRag.Tests\         Unit and integration tests
```

### Responsibility boundaries
- `WeatherRag.Api`
  - Hosts HTTP endpoints for ingest, index status, and query
  - Owns configuration binding, DI registration, and request validation
  - Coordinates calls into the RAG and inference projects
- `WeatherRag.Frontend`
  - Provides local operator workflows for upload/index/query
  - Calls the Web API only; no direct model or file-system logic
- `WeatherRag.Rag`
  - Handles PDF extraction, chunking, metadata capture, embedding generation requests, FAISS indexing, and retrieval
  - Returns retrieved context with citation-ready metadata
- `WeatherRag.Inference`
  - Owns LlamaSharp model/session integration
  - Builds the weather-briefer system prompt and answer-composition flow
  - Stays behind interfaces so multimodal or alternate local runtimes can be added later

## Multi-phase implementation schedule

This schedule turns the architecture into an execution sequence with explicit dependencies, outputs, and exit gates. Each phase should complete with a working checkpoint before the next phase begins.

### Phase 0: Resolve remaining design choices
**Purpose:** Eliminate the last decisions that affect implementation details before scaffolding starts.

**Dependencies**
- None

**Steps**
1. Select the first pure-.NET PDF library
2. Decide whether image caption generation is in scope for milestone one
3. Confirm where local models and indexed data will live on disk
4. Confirm whether the frontend will be hosted by the API or run as a separate local static app during development

**Outputs**
- Final PDF extraction choice
- Confirmed milestone-one scope for image handling
- Agreed local directory/configuration conventions

**Exit gate**
- No core implementation decision remains ambiguous enough to block scaffolding or service contracts

### Phase 1: Scaffold the solution and project boundaries
**Purpose:** Create the minimal runnable structure for the backend, frontend, retrieval, inference, and tests.

**Dependencies**
- Phase 0

**Steps**
1. Create the `.sln`
2. Create `WeatherRag.Api`, `WeatherRag.Frontend`, `WeatherRag.Rag`, `WeatherRag.Inference`, and `WeatherRag.Tests`
3. Wire project references so the API depends on RAG and Inference, while the frontend calls the API over HTTP only
4. Add configuration, options, logging, and DI registration
5. Add a health endpoint and a minimal frontend shell page

**Outputs**
- Buildable multi-project solution
- Minimal API startup path
- Minimal frontend page and API connectivity path

**Exit gate**
- The solution builds locally, the API starts, and the frontend can reach the health endpoint

### Phase 2: Establish the inference and embedding foundation
**Purpose:** Put the local model integration points in place before document ingestion and retrieval are implemented.

**Dependencies**
- Phase 1

**Steps**
1. Introduce shared options/contracts for inference and embeddings
2. Integrate LlamaSharp into `WeatherRag.Inference`
3. Add ONNX Runtime DirectML support for local embedding generation
4. Define model-loading, prompt-building, and resource-lifetime patterns
5. Implement the base Senior Air Force Weather Forecaster, Observer, and Briefer system prompt

**Outputs**
- Runnable local inference path
- Runnable local embedding path
- Configuration-driven model settings

**Exit gate**
- Sample text can be embedded locally and a simple prompt can be answered locally without external services

### Phase 3: Build text-first PDF ingestion
**Purpose:** Convert source weather PDFs into normalized, citation-ready retrieval records.

**Dependencies**
- Phase 0
- Phase 1
- Phase 2 for shared contracts, if embedding-trigger hooks are created during ingestion

**Steps**
1. Integrate the chosen pure-.NET PDF library
2. Add secure local document import and storage path validation
3. Extract page text and record page-level provenance
4. Capture image references and metadata, even if caption generation is deferred
5. Normalize and chunk extracted text for retrieval
6. Define chunk metadata for file name, page number, section cues, and extraction status

**Outputs**
- Importable PDF ingestion flow
- Stable chunking pipeline
- Citation-ready metadata model

**Exit gate**
- A sample weather PDF can be imported and chunked into provenance-preserving records with no silent failures

### Phase 4: Implement FAISS indexing and retrieval
**Purpose:** Turn chunked documents into a searchable local retrieval system.

**Dependencies**
- Phase 2
- Phase 3

**Steps**
1. Generate embeddings for each chunk
2. Persist vector data and retrieval metadata through the FAISS integration layer
3. Implement index build, reload, and update workflows
4. Implement similarity search and ranked result shaping
5. Return retrieval results with all metadata needed for downstream citation rendering

**Outputs**
- Local FAISS-backed retrieval store
- Indexing workflow for newly ingested documents
- Search API contracts for ranked retrieval

**Exit gate**
- Retrieval returns relevant chunks and citations for a known sample document set

### Phase 5: Implement grounded answer generation
**Purpose:** Compose retrieval results into operationally useful answers without losing grounding or offline guarantees.

**Dependencies**
- Phase 2
- Phase 4

**Steps**
1. Build the retrieval-plus-generation orchestration in the API layer
2. Inject retrieved passages into the forecaster/observer/briefer prompt
3. Define answer formatting rules for grounded summaries, citations, and uncertainty handling
4. Keep the inference boundary isolated from retrieval internals
5. Add image-enrichment hooks if milestone one includes caption generation

**Outputs**
- End-to-end query pipeline from retrieval to answer generation
- Citation-aware answer model
- Persona-aligned response formatting

**Exit gate**
- A query produces a grounded local answer with supporting citations and no external AI dependency

### Phase 6: Deliver the first usable frontend workflow
**Purpose:** Expose the ingestion and query capabilities through a simple local operator interface.

**Dependencies**
- Phase 1
- Phase 3
- Phase 5

**Steps**
1. Add document import/upload UI
2. Add indexing controls and status display
3. Add query input and answer rendering
4. Show citations, retrieval context summaries, and user-visible errors
5. Make the workflow usable without requiring direct API tooling

**Outputs**
- End-to-end local operator workflow
- Query results UI with source visibility
- Basic indexing and error feedback UX

**Exit gate**
- A user can ingest a PDF and run a question from the frontend without manual API calls

### Phase 7: Add validation, resilience, and documentation
**Purpose:** Turn the first working system into a supportable baseline.

**Dependencies**
- Phases 1 through 6

**Steps**
1. Add unit tests for configuration binding, prompt composition, chunking, and metadata mapping
2. Add retrieval tests for embedding-to-index-to-search flows
3. Add integration tests for ingest/query paths where practical
4. Validate offline-only assumptions and local file-path safety
5. Document setup, model placement, indexing operations, and known constraints

**Outputs**
- Baseline automated coverage for the critical path
- Reproducible local setup guidance
- Known limitations documented for future phases

**Exit gate**
- The critical ingest, retrieve, and answer workflow is covered by tests and documented clearly enough for repeatable local setup

## Recommended work breakdown inside each phase
- Start each phase by defining or updating the contracts it introduces
- Implement the narrowest working vertical slice first
- Add validation around that slice before broadening the scope
- Preserve a runnable checkpoint at the end of every phase

## Initial package and technology direction
- **API:** ASP.NET Core on .NET 8
- **Frontend:** Plain HTML/TypeScript/CSS served separately or as static assets, depending on hosting convenience
- **Inference:** LlamaSharp
- **Embeddings:** ONNX Runtime DirectML with a local MiniLM-class embedding model
- **Retrieval:** FAISS local vector index through a .NET-compatible wrapper or interop layer
- **Prompt layer:** Professional, concise, operationally focused Air Force weather briefing style
- **Parallelism:** Prefer async I/O and `Parallel.ForEachAsync` for indexing workloads

## Key implementation guardrails
- Keep the first milestone offline only
- Do not call cloud AI services
- Keep file-system access constrained to approved local paths
- Use DI for services and options
- Dispose document/model resources carefully
- Surface failures explicitly; do not hide ingestion or retrieval errors
- Keep the API, retrieval, and inference concerns separated

## Open decisions still to resolve
1. **PDF library choice**
   - Decide whether the first extractor should be a commercial library such as `Aspose.PDF` or a non-commercial-first option such as `iText`
   - This affects licensing, extraction quality, and image metadata support

2. **Image-aware scope for milestone one**
   - Decide whether image caption generation is part of the first milestone
   - If not, milestone one should still preserve image metadata/hooks so multimodal enrichment can be added later without reworking ingestion

## Success criteria
- The solution can ingest local weather PDFs
- The system can build a local FAISS index from extracted chunks
- A query returns a grounded answer with citations
- The app runs without external AI calls
- The frontend supports the first ingest-and-query workflow
- Tests cover the critical ingestion and retrieval path

## Execution order
1. Finalize the PDF library and first-milestone image scope
2. Scaffold the multi-project solution
3. Add local inference and embedding foundations
4. Implement PDF ingestion and chunking
5. Implement FAISS indexing and retrieval
6. Implement grounded answer generation
7. Connect the frontend workflow
8. Add tests and setup documentation
