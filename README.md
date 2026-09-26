# my-rag

Playing around with LLMs and embeddings.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/) with the models from `Options/OllamaOptions.cs` pulled:
  ```bash
  ollama pull bge-m3
  ollama pull granite3.3:8b
  ```
  Change `EmbeddingModel` / `ChatModel` there (and `VectorSize` in `Options/QdrantOptions.cs` to match the embedding dims).
- [Docker](https://www.docker.com/) for Qdrant

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