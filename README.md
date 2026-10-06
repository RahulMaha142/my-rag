# my-rag

Playing around with LLMs and embeddings.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Docker](https://www.docker.com/) for Qdrant
- Either [oMLX](https://github.com/jundot/omlx) or [Ollama](https://ollama.com/). Set `Inference:Provider` in `appsettings.json` to `Omlx` (the default) or `Ollama`, then restart the app.

Both default embedding models are 1024-dimensional, which matches `VectorSize` in `Options/QdrantOptions.cs`. Re-upload notes after switching embedding models so Qdrant is not left with vectors from the previous model.

### oMLX

Serve the models named in `appsettings.json` (`Omlx:EmbeddingModel` and the chat models in `Omlx:ChatModels`). The default chat model is `Omlx:ChatModel` (`Qwen3.8-27B-4bit`). On the Search page you can switch among the models in `Omlx:ChatModels`. Folder names under the oMLX model directory are the model ids. Default base URL is `http://127.0.0.1:8000`. If the server requires a key, put `Omlx_api_key=...` in a `.env` file at the repo root. The app loads that file at startup.

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

SQLite needs the schema (including the `Documents` table) before the app can serve requests:

```bash
dotnet tool install --global dotnet-ef   # once, if not already installed
dotnet ef database update --project src/RagNotes.Web
```

## Run the app

```bash
dotnet run --project src/RagNotes.Web
```

## Semantic search

1. Upload `.txt` or `.md` notes under **Documents** (chunks are embedded into Qdrant).
2. Open **Search** (`/Search`) and ask a question in natural language.
3. Results show the top matching chunks with similarity score, file name, chunk index, and text — no LLM yet.