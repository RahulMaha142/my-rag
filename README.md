# my-rag

Playing around with LLMs and embeddings.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) for Qdrant
- Either [oMLX](https://github.com/jundot/omlx) or [Ollama](https://ollama.com/). Set `Inference:Provider` in `appsettings.json` to `Omlx` (the default) or `Ollama`, then restart the app.

Each embedding model uses its own Qdrant collection (`rag-notes__{model-id}`) with that model’s vector size from `EmbeddingModels` in `appsettings.json`. If an unsuffixed `rag-notes` collection already exists and the default model’s suffixed collection does not, the app keeps using `rag-notes` for the configured default embedding model only.

### oMLX

Serve the models named in `appsettings.json` (`Omlx:EmbeddingModels` and the chat models in `Omlx:ChatModels`). The default embedding model is `Omlx:EmbeddingModel` (`bge-m3-mlx-4bit`, 1024-d). `Qwen3-Embedding-8B-4bit-DWQ` is configured as a second option (4096-d). The default chat model is `Omlx:ChatModel` (`Qwen3.8-27B-4bit`). On the Search page you can switch among chat models and embedding models. Folder names under the oMLX model directory are the model ids. Default base URL is `http://127.0.0.1:8000`. If the server requires a key, put `Omlx_api_key=...` in a `.env` file at the repo root. The app loads that file at startup.

### Ollama

Install [Ollama](https://ollama.com/), then pull the models named under `Ollama` in `appsettings.json` (or change those names to models you already have):

```bash
ollama pull bge-m3
ollama pull granite3.3:8b
```

Default base URL is `http://localhost:11434`.

## Start Qdrant

```bash
docker compose up -d
```

Confirm the dashboard is available at http://localhost:6333/dashboard.

## Apply database migrations

SQLite needs the schema (including `Documents` and `DocumentEmbeddingIndexes`) before the app can serve requests. On startup the app runs migrations automatically; you can also apply them manually:

```bash
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef database update --project src/RagNotes.Web
```

## Run the app

```bash
dotnet run --project src/RagNotes.Web
```

## Semantic search

1. Upload `.txt` or `.md` notes under **Documents** and choose an embedding model. Chunks are embedded into that model’s Qdrant collection.
2. To add an existing document to another model (for example Qwen), open the document and use **Index into this model**, or on Documents use **Index missing documents**.
3. Open **Search** (`/Search`), pick the same embedding model you indexed with, and ask a question in natural language.
4. Results show the top matching chunks with similarity score, file name, chunk index, and text; the selected chat model answers from those sources.
