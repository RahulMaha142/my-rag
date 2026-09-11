# Personal Knowledge Base / RAG Notes — Development Plan

## Goal

Build a simple local web application that lets you:

> Upload a document → index it → ask a question → retrieve relevant chunks → send them to a local LLM → display an answer with sources.

The project is intended to practice:

- C# / .NET
- ASP.NET Core
- Document ingestion
- Text chunking
- Embeddings
- Vector databases
- Retrieval-Augmented Generation (RAG)
- Local LLMs
- Semantic search
- RAG evaluation

---

## Target Architecture

```text
                    ┌─────────────────────┐
                    │     Web Browser     │
                    │                     │
                    │ Upload + Chat UI    │
                    └──────────┬──────────┘
                               │
                               ▼
                    ┌─────────────────────┐
                    │    ASP.NET Core     │
                    │       Web API       │
                    └──────────┬──────────┘
                               │
             ┌─────────────────┼─────────────────┐
             │                 │                 │
             ▼                 ▼                 ▼
       Document Service   Embedding Service   Chat Service
             │                 │                 │
             ▼                 ▼                 ▼
          Chunking          Embedding          Local LLM
             │                 │                 │
             └────────────┬────┘                 │
                          ▼                      │
                    ┌─────────────┐              │
                    │ Vector DB   │◄─────────────┘
                    └─────────────┘
```

## Initial Technology Stack

- **C# / .NET**
- **ASP.NET Core**
- **Blazor** or a simple HTML/JavaScript frontend
- **SQLite** for document metadata
- **Qdrant** for vector storage
- **Ollama** for local LLM and embedding models
- **Git**

### Important principle

Do not start with a large AI framework.

Initially, implement the core RAG pipeline yourself so that you understand what is happening:

```text
C#
 ↓
HTTP
 ↓
Embedding model
 ↓
Vector database
 ↓
LLM
```

Frameworks such as Semantic Kernel can be introduced later.

---

# Development Roadmap

## Milestone 1 — Basic Web Application

### Goal

Get a basic ASP.NET Core application running.

### Tasks

- Install the .NET SDK.
- Create the ASP.NET Core application.
- Create a test project.
- Initialize Git.
- Run the application locally.
- Confirm the browser can communicate with the application.

### Deliverable

A working web application that displays something like:

```text
Hello, RAG
```

### Suggested Git commit

```text
Initial ASP.NET Core application
```

---

# Milestone 2 — Document Ingestion

### Goal

Upload and display text documents.

Start with only:

- `.txt`
- `.md`

Do not implement PDFs yet.

### Features

Create an upload page.

```text
┌─────────────────────────────┐
│ Upload Document             │
│                             │
│ [ Choose File ]             │
│                             │
│ [ Upload ]                  │
└─────────────────────────────┘
```

### API

Eventually support:

```http
POST   /api/documents
GET    /api/documents
GET    /api/documents/{id}
DELETE /api/documents/{id}
```

### Initial model

```csharp
Document
{
    Id
    FileName
    Content
    CreatedAt
}
```

### Deliverable

Upload a `.txt` or `.md` file and display its extracted text.

### Suggested Git commit

```text
Add document upload
```

---

# Milestone 3 — Store Documents in SQLite

### Goal

Persist uploaded documents rather than keeping them only in memory.

### Tasks

- Add SQLite.
- Add Entity Framework Core.
- Create a `Document` entity.
- Create the database.
- Save uploaded documents.
- List existing documents.
- Delete documents.

### Deliverable

Restarting the application does not delete uploaded documents.

### Suggested Git commit

```text
Store documents in SQLite
```

---

# Milestone 4 — Text Chunking

### Goal

Split documents into smaller pieces suitable for retrieval.

The pipeline becomes:

```text
Document
    ↓
Text
    ↓
Chunks
```

For the initial implementation, use:

```text
Chunk size: 500 tokens
Overlap:    100 tokens
```

Do not obsess over optimal values yet.

### Suggested model

```csharp
public class TextChunk
{
    public Guid Id { get; set; }
    public Guid DocumentId { get; set; }
    public string Text { get; set; }
    public int ChunkIndex { get; set; }
}
```

### Suggested abstraction

```csharp
public interface ITextChunker
{
    IEnumerable<TextChunk> Chunk(string text);
}
```

### Deliverable

Upload a document and display:

```text
Document
 ├── Chunk 1
 ├── Chunk 2
 ├── Chunk 3
 ├── Chunk 4
 └── ...
```

### Suggested Git commit

```text
Add text chunking
```

---

# Milestone 5 — Local Embeddings

### Goal

Generate an embedding vector for every chunk.

Conceptually:

```text
"Dependency injection allows..."
              │
              ▼
        Embedding model
              │
              ▼
[0.021, -0.381, 0.772, ...]
```

### Create an abstraction

```csharp
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
}
```

The first implementation should use a local model.

For example:

```text
OllamaEmbeddingService
```

Later, it could be replaced with another provider without changing the rest of the application.

### Deliverable

For each text chunk, generate and store an embedding.

### Suggested Git commit

```text
Add local embedding generation
```

---

# Milestone 6 — Add Qdrant

### Goal

Store embeddings in a vector database.

The pipeline becomes:

```text
Document
    ↓
Chunk
    ↓
Embedding
    ↓
Qdrant
```

Conceptually, a vector record might contain:

```text
ID: 123

Vector:
[0.021, -0.381, 0.772, ...]

Payload:
{
    documentId: "...",
    fileName: "csharp.md",
    chunkIndex: 17,
    text: "Dependency injection..."
}
```

### Deliverable

All document chunks are embedded and indexed in Qdrant.

### Suggested Git commit

```text
Add Qdrant vector storage
```

---

# Milestone 7 — Semantic Search Without an LLM

## This is an important milestone.

Do not build chat yet.

First prove that semantic retrieval works.

### Pipeline

```text
User question
      ↓
Embedding model
      ↓
Query vector
      ↓
Qdrant
      ↓
Top K chunks
      ↓
Display results
```

For example:

```text
Search

[ What is dependency injection?          ] [Search]


Results
─────────────────────────────────────────

0.87
csharp.md — Chunk 17

"Dependency injection is a technique..."


0.81
architecture.md — Chunk 4

"Services can be registered..."


0.76
notes.md — Chunk 22

"The DI container..."
```

### Experiment

Document:

> Dependency injection allows objects to receive their dependencies from an external source.

Question:

> How do classes get the services they need?

A good semantic search system should recognize that these concepts are related even though the wording differs.

### Deliverable

A working semantic search page showing:

- Query
- Similarity score
- Document
- Chunk
- Retrieved text

### Suggested Git commit

```text
Implement semantic search
```

---

# Milestone 8 — Add the Local LLM

### Goal

Use the retrieved chunks as context for a local LLM.

The pipeline becomes:

```text
                    User question
                         │
                         ▼
                  Generate embedding
                         │
                         ▼
                    Vector search
                         │
                         ▼
                   Top 5 chunks
                         │
                         ▼
                  Construct prompt
                         │
                         ▼
                     Local LLM
                         │
                         ▼
                      Answer
```

### Initial prompt

```text
You are a helpful assistant.

Answer the user's question using only the
provided context.

If the context doesn't contain the answer,
say that you don't know.

Context:

[Chunk 1]

[Chunk 2]

[Chunk 3]

Question:

{question}
```

### Create an abstraction

```csharp
public interface ILlmService
{
    Task<string> GenerateAsync(string prompt);
}
```

### Deliverable

Ask a question and receive an answer generated by the local LLM.

### Suggested Git commit

```text
Add local LLM
```

---

# Milestone 9 — Build the RAG Service

At this point, combine the individual components.

The central pipeline should look approximately like:

```csharp
public class RagService
{
    private readonly IEmbeddingService _embeddings;
    private readonly IVectorStore _vectors;
    private readonly ILlmService _llm;

    public async Task<Answer> AskAsync(string question)
    {
        var embedding =
            await _embeddings.EmbedAsync(question);

        var results =
            await _vectors.SearchAsync(embedding, 5);

        var prompt =
            BuildPrompt(question, results);

        var answer =
            await _llm.GenerateAsync(prompt);

        return new Answer(answer, results);
    }
}
```

### Deliverable

A chat page:

```text
┌─────────────────────────────────────────┐
│ Ask a question                          │
│                                         │
│ [ What does dependency injection do? ]  │
│                              [Ask]      │
└─────────────────────────────────────────┘

Answer
─────────────────────────────────────────

Dependency injection allows...

Sources
─────────────────────────────────────────

csharp.md — Chunk 17
architecture.md — Chunk 4
```

### Suggested Git commit

```text
Implement basic RAG pipeline
```

---

# Milestone 10 — Add Source Citations

### Goal

Make every answer traceable to retrieved documents.

Instead of only displaying:

```text
Dependency injection is...
```

display:

```text
Answer
─────────────────────────────

Dependency injection allows dependencies
to be supplied to an object rather than
instantiated internally.

Sources
─────────────────────────────

📄 csharp.md
   Chunk 17

📄 architecture.md
   Chunk 4
```

Eventually, make each source clickable so the user can inspect the original chunk.

### Deliverable

Every generated answer shows the source chunks used as context.

### Suggested Git commit

```text
Add RAG source citations
```

---

# Milestone 11 — PDF Support

Only add PDFs after TXT/Markdown ingestion works reliably.

The ingestion pipeline becomes:

```text
TXT       ─┐
Markdown  ─┤
PDF       ─┼──→ Text extraction → Chunking
DOCX      ─┤
HTML      ─┘
```

Add one format at a time.

PDF extraction deserves its own milestone because poor extraction can significantly reduce RAG quality.

### Suggested Git commit

```text
Add PDF document ingestion
```

---

# Milestone 12 — Document Management

Improve the document library.

Features:

- List documents
- Upload documents
- Delete documents
- Show document size
- Show chunk count
- Show indexing status
- Re-index a document
- View extracted text
- View chunks

Possible UI:

```text
Documents

┌──────────────────────────────────────────────────┐
│ csharp.md          14 chunks    Indexed    [View]│
│ rag-notes.pdf      87 chunks    Indexed    [View]│
│ architecture.md    22 chunks    Indexed    [View]│
└──────────────────────────────────────────────────┘
```

---

# Milestone 13 — Improve Retrieval

Once the basic system works, experiment with retrieval.

Things to investigate:

- Chunk size
- Chunk overlap
- Top K
- Metadata filtering
- Similarity thresholds
- Hybrid search
- Reranking
- Query rewriting

Example:

```text
Chunk size:       [ 500 ]
Overlap:          [ 100 ]

Top K:            [ 5 ]

Similarity:
  ○ cosine
  ○ dot product

Temperature:      [ 0.2 ]
```

The purpose is to understand how retrieval choices affect answer quality.

---

# Milestone 14 — RAG Evaluation

This is where the project becomes significantly more advanced.

Create a small test dataset:

```json
{
  "question": "What does the refund policy allow?",
  "expected_sources": [
    "refunds.md"
  ],
  "expected_answer": "..."
}
```

Run your RAG system against the questions.

Evaluate:

### Retrieval

Did the correct document/chunk get retrieved?

### Generation

Did the LLM produce an answer supported by the retrieved context?

### Groundedness

Did the model invent information?

### Failure handling

What happens when the answer does not exist?

For example:

```text
Question:
What is the company's policy on moon travel?

Result:
I couldn't find sufficient information
in the knowledge base to answer that.
```

---

# Project Structure

Start with a single ASP.NET Core application rather than many projects.

```text
RagNotes/
│
├── RagNotes.sln
│
├── src/
│   └── RagNotes.Web/
│       │
│       ├── Components/
│       │
│       ├── Controllers/
│       │
│       ├── Data/
│       │
│       ├── Models/
│       │
│       ├── Services/
│       │   ├── Documents/
│       │   ├── Chunking/
│       │   ├── Embeddings/
│       │   ├── VectorStore/
│       │   └── Llm/
│       │
│       ├── Program.cs
│       └── appsettings.json
│
└── tests/
    └── RagNotes.Tests/
```

Avoid over-engineering the architecture at the beginning.

Refactor later when there is a concrete reason to do so.

---

# Core Interfaces

Establish these boundaries early.

## Document Service

```csharp
public interface IDocumentService
{
    Task<Document> AddAsync(...);
    Task<Document?> GetAsync(Guid id);
    Task<IReadOnlyList<Document>> GetAllAsync();
}
```

## Chunker

```csharp
public interface ITextChunker
{
    IReadOnlyList<TextChunk> Chunk(string text);
}
```

## Embedding Service

```csharp
public interface IEmbeddingService
{
    Task<float[]> EmbedAsync(string text);
}
```

## Vector Store

```csharp
public interface IVectorStore
{
    Task StoreAsync(...);

    Task<IReadOnlyList<SearchResult>>
        SearchAsync(float[] embedding, int topK);
}
```

## LLM Service

```csharp
public interface ILlmService
{
    Task<string> GenerateAsync(string prompt);
}
```

## RAG Service

The RAG service orchestrates the other components:

```text
Question
   ↓
Embedding
   ↓
Vector Search
   ↓
Retrieved Chunks
   ↓
Prompt
   ↓
Local LLM
   ↓
Answer + Sources
```

---

# Day 1 — Start Here

Do not start by implementing RAG.

First, get your local development environment working.

## Install

### .NET SDK

https://dotnet.microsoft.com/download

### Ollama

https://ollama.com/

### Qdrant

https://qdrant.tech/

### Verify .NET

```bash
dotnet --version
```

### Verify Ollama

```bash
ollama --version
```

Get a local LLM running through Ollama before writing application code.

The exact model should be selected based on your machine's RAM/GPU.

---

# Create the Application

Example commands:

```bash
dotnet new webapp -n RagNotes.Web
dotnet new xunit -n RagNotes.Tests

dotnet new sln -n RagNotes

dotnet sln add src/RagNotes.Web
dotnet sln add tests/RagNotes.Tests
```

Then run:

```bash
dotnet run
```

Open the application in your browser.

Your first Git commit should be:

```text
Initial ASP.NET Core application
```

---

# Recommended Git Progression

Keep each commit tied to a working feature.

```text
01  Initial ASP.NET Core application
        ↓
02  Add document upload
        ↓
03  Store documents in SQLite
        ↓
04  Add text chunking
        ↓
05  Add local embedding generation
        ↓
06  Add Qdrant vector storage
        ↓
07  Implement semantic search
        ↓
08  Build search UI
        ↓
09  Add local LLM
        ↓
10  Implement RAG
        ↓
11  Add source citations
        ↓
12  Add PDF ingestion
        ↓
13  Add document management
        ↓
14  Improve chunking
        ↓
15  Add metadata filtering
        ↓
16  Add RAG evaluation
```

The goal is to keep every step runnable.

---

# First Concrete Goal

The first milestone should be deliberately small:

> **Build a .NET web app with a page where you can upload a `.txt` or `.md` file and see the extracted text displayed on the page.**

Do not implement embeddings, Qdrant, or the LLM yet.

After that:

```text
Milestone 1
Upload document
      ↓
Milestone 2
Split document into chunks
      ↓
Milestone 3
Generate local embeddings
      ↓
Milestone 4
Store vectors
      ↓
Milestone 5
Semantic search
      ↓
Milestone 6
Local LLM
      ↓
Milestone 7
RAG
      ↓
Milestone 8
Citations + evaluation
```

This progression keeps the project understandable and gives you a working application at every stage.
