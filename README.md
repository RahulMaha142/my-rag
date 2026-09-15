# my-rag

Playing around with LLMs and embeddings.

## Prerequisites

- [.NET 8 SDK](https://dotnet.microsoft.com/download)
- [Ollama](https://ollama.com/) with the embedding model pulled:
  ```bash
  ollama pull nomic-embed-text
  ```
- [Docker](https://www.docker.com/) for Qdrant

## Start Qdrant

```bash
docker compose up -d
```

Confirm the dashboard is available at http://localhost:6333/dashboard.

## Run the app

```bash
dotnet run --project src/RagNotes.Web
```

## Semantic search

1. Upload `.txt` or `.md` notes under **Documents** (chunks are embedded into Qdrant).
2. Open **Search** (`/Search`) and ask a question in natural language.
3. Results show the top matching chunks with similarity score, file name, chunk index, and text — no LLM yet.