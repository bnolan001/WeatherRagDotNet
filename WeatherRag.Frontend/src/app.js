"use strict";
const API_BASE = "http://localhost:5000";
function $(id) {
    return document.getElementById(id);
}
function formatElapsedMs(elapsedMs) {
    const safeElapsedMs = Number.isFinite(elapsedMs) && elapsedMs >= 0 ? Math.floor(elapsedMs) : 0;
    const totalSeconds = Math.floor(safeElapsedMs / 1000);
    const minutes = Math.floor(totalSeconds / 60);
    const seconds = totalSeconds % 60;
    const milliseconds = safeElapsedMs % 1000;
    return `${minutes}:${seconds.toString().padStart(2, "0")}.${milliseconds.toString().padStart(3, "0")}`;
}
let availableModels = [];
async function fetchModels() {
    try {
        const resp = await fetch(`${API_BASE}/api/models`);
        if (!resp.ok)
            throw new Error(`HTTP ${resp.status}`);
        const models = await resp.json();
        availableModels = models;
        const modelSelect = $("model-select");
        modelSelect.innerHTML = "";
        for (const model of models) {
            const option = document.createElement("option");
            option.value = model.id;
            option.textContent = model.displayName + (model.isDefault ? " (default)" : "");
            option.disabled = !model.isAvailable;
            if (!model.isAvailable) {
                option.textContent += " — unavailable";
            }
            modelSelect.appendChild(option);
        }
        const savedModelId = localStorage.getItem("selectedModelId");
        const savedModelExists = savedModelId && models.some(m => m.id === savedModelId && m.isAvailable);
        const defaultModel = models.find(m => m.isDefault && m.isAvailable);
        const firstAvailable = models.find(m => m.isAvailable);
        if (savedModelExists) {
            modelSelect.value = savedModelId;
        }
        else if (defaultModel) {
            modelSelect.value = defaultModel.id;
        }
        else if (firstAvailable) {
            modelSelect.value = firstAvailable.id;
        }
    }
    catch (err) {
        console.error("Failed to fetch models:", err);
        const modelSelect = $("model-select");
        modelSelect.innerHTML = '<option value="">Failed to load models</option>';
    }
}
$("model-select").addEventListener("change", (e) => {
    const select = e.target;
    localStorage.setItem("selectedModelId", select.value);
});
async function fetchStatus() {
    try {
        const resp = await fetch(`${API_BASE}/api/health/index`);
        if (!resp.ok)
            throw new Error(`HTTP ${resp.status}`);
        const data = await resp.json();
        $("status-chunks").textContent = `${data.chunkCount} chunks indexed`;
    }
    catch {
        $("status-chunks").textContent = "Unavailable — Is the API running?";
    }
}
$("btn-refresh-status").addEventListener("click", () => void fetchStatus());
const ingestForm = $("ingest-form");
ingestForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const fileInput = $("pdf-file");
    const files = Array.from(fileInput.files ?? []);
    if (files.length === 0)
        return;
    const statusEl = $("ingest-status");
    const progressEl = $("ingest-progress");
    const btn = $("btn-ingest");
    btn.disabled = true;
    statusEl.className = "feedback";
    statusEl.textContent = "";
    progressEl.style.display = "block";
    const results = [];
    let hasError = false;
    for (let i = 0; i < files.length; i++) {
        const file = files[i];
        statusEl.className = "feedback";
        statusEl.textContent = `Indexing ${i + 1} of ${files.length}: ${file.name}…`;
        const formData = new FormData();
        formData.append("file", file);
        try {
            const resp = await fetch(`${API_BASE}/api/ingest/upload`, {
                method: "POST",
                body: formData,
            });
            const data = await resp.json();
            if (!resp.ok)
                throw new Error(data.message ?? `HTTP ${resp.status}`);
            results.push(`✓ ${file.name}`);
        }
        catch (err) {
            hasError = true;
            results.push(`✗ ${file.name}: ${err instanceof Error ? err.message : "Ingestion failed."}`);
        }
    }
    statusEl.className = `feedback ${hasError ? "error" : "success"}`;
    statusEl.innerHTML = results.join("<br>");
    void fetchStatus();
    btn.disabled = false;
    progressEl.style.display = "none";
});
const queryForm = $("query-form");
queryForm.addEventListener("submit", async (e) => {
    e.preventDefault();
    const queryInput = $("query-input");
    const query = queryInput.value.trim();
    if (!query)
        return;
    const modelSelect = $("model-select");
    const selectedModelId = modelSelect.value || null;
    const btn = $("btn-query");
    const spinner = $("query-spinner");
    const answerSection = $("answer-section");
    const errorEl = $("query-error");
    const durationEl = $("query-duration");
    btn.disabled = true;
    spinner.style.display = "block";
    answerSection.style.display = "none";
    errorEl.style.display = "none";
    try {
        const resp = await fetch(`${API_BASE}/api/query`, {
            method: "POST",
            headers: { "Content-Type": "application/json" },
            body: JSON.stringify({ query, modelId: selectedModelId }),
        });
        const data = await resp.json();
        if (!resp.ok)
            throw new Error(data.message ?? `HTTP ${resp.status}`);
        $("answer-text").textContent = data.answer;
        const badge = $("grounding-badge");
        badge.textContent = data.isGrounded ? "GROUNDED — Based on indexed documents" : "UNGROUNDED — No document context";
        badge.className = `badge ${data.isGrounded ? "grounded" : "ungrounded"}`;
        durationEl.textContent = `Execution time: ${formatElapsedMs(data.elapsedMs)}`;
        const citationsList = $("citations-list");
        citationsList.innerHTML = "";
        for (const cit of data.citations ?? []) {
            const li = document.createElement("li");
            li.innerHTML = `<span>${cit.sourceFile} — Page ${cit.pageNumber}</span>${cit.sectionHint ? ` <em>${cit.sectionHint}</em>` : ""}<span class="citation-score">Score: ${(cit.score * 100).toFixed(0)}%</span>`;
            citationsList.appendChild(li);
        }
        answerSection.style.display = "block";
    }
    catch (err) {
        errorEl.textContent = `✗ ${err instanceof Error ? err.message : "Query failed."}`;
        errorEl.style.display = "block";
    }
    finally {
        btn.disabled = false;
        spinner.style.display = "none";
    }
});
void fetchModels();
void fetchStatus();
