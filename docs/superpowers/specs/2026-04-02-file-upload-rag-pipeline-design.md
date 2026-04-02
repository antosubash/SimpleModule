# File Upload → RAG Pipeline Design

## Problem

Uploaded files are stored but never indexed into the RAG knowledge system. The RAG infrastructure works for static knowledge sources (like `ProductKnowledgeSource`), but there is no pipeline to extract text from uploaded documents and make them searchable by AI agents.

## Solution

Event-driven pipeline: file upload → event → text extraction → vector indexing. Users opt in per upload via an `indexForRag` parameter on the HTTP endpoint.

## Upload Flow Changes

The upload HTTP endpoint (`POST /api/files/`) accepts an optional `indexForRag` boolean parameter. The endpoint — not the contract interface — controls whether to publish a `FileUploadedEvent`. This keeps the RAG concept out of the `IFileStorageContracts` interface (which should not know about RAG).

After the upload endpoint saves the file, if `indexForRag` is true, it publishes `FileUploadedEvent` via `eventBus.PublishInBackground()`.

On file deletion, `FileStorageService` unconditionally publishes `FileDeletedEvent` so the RAG handler can clean up if the file was indexed. This is safe — the handler is a no-op if the file was never indexed.

`FileUploadedEvent` and `FileDeletedEvent` live in `FileStorage.Contracts` so the RAG handler can reference them without depending on the FileStorage implementation.

### Events

```csharp
// FileStorage.Contracts/Events/FileUploadedEvent.cs
public record FileUploadedEvent(FileStorageId FileId, string ContentType) : IEvent;

// FileStorage.Contracts/Events/FileDeletedEvent.cs
public record FileDeletedEvent(FileStorageId FileId) : IEvent;
```

## Text Extraction Layer

New framework project: `SimpleModule.Rag.Extraction`.

### Interface

```csharp
public interface ITextExtractor
{
    bool CanExtract(string contentType);
    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken);
}
```

### Implementations

| Extractor | Content Types | Dependency |
|-----------|--------------|------------|
| `PlainTextExtractor` | `text/plain`, `text/markdown`, `text/csv` | Built-in `StreamReader` |
| `PdfTextExtractor` | `application/pdf` | `UglyToad.PdfPig` |
| `OfficeTextExtractor` | `application/vnd.openxmlformats-officedocument.*` (docx, xlsx, pptx) | `DocumentFormat.OpenXml` |
| `VisionOcrExtractor` | `image/png`, `image/jpeg`, `image/webp`, `image/gif`, `image/tiff` | Ollama `llama3.2-vision` via `IChatClient` |

### TextExtractionService

Resolves the correct extractor by iterating registered `ITextExtractor` instances and calling `CanExtract(contentType)`. If no extractor matches the content type, logs a warning and returns null (no error for unsupported types).

### VisionOcrExtractor

Uses the existing `IChatClient` (Ollama) with a dedicated vision model. Sends the image bytes with a system prompt: "Extract all visible text from this image. Return only the extracted text, nothing else." Falls back to returning empty string if the model cannot extract text.

The vision model name comes from configuration (`AI:Ollama:VisionModel`, defaulting to `llama3.2-vision`).

## Event Handler & Indexing

`RagFileIndexingHandler` implements `IEventHandler<FileUploadedEvent>`, lives in `modules/Rag/src/SimpleModule.Rag.Module/Handlers/`.

**Handlers must be explicitly registered in `RagModule.ConfigureServices`** — the source generator does not auto-discover `IEventHandler<T>` implementations:

```csharp
services.AddScoped<IEventHandler<FileUploadedEvent>, RagFileIndexingHandler>();
services.AddScoped<IEventHandler<FileDeletedEvent>, RagFileDeletedHandler>();
```

### Indexing Flow

1. Receives `FileUploadedEvent` (file ID + content type)
2. Downloads file via `IFileStorageContracts.DownloadFileAsync(fileId)`
3. Gets file metadata via `IFileStorageContracts.GetFileByIdAsync(fileId)` for the filename
4. Calls `TextExtractionService.ExtractAsync(stream, contentType)` to get text
5. If text is empty/null, logs and returns (unsupported type or empty file)
6. Wraps in `KnowledgeDocument` with metadata: `source=file-upload`, `fileId`, `fileName`
7. Calls `IKnowledgeStore.IndexDocumentsAsync("uploaded-documents", [doc])` with a caller-supplied document ID (the file ID string) instead of a random GUID

### Vector Record ID Strategy

Currently `VectorKnowledgeStore.IndexDocumentsAsync` generates `Guid.NewGuid()` for each record. To support deletion by file ID, `KnowledgeDocument` gains an optional `Id` property. When set, `VectorKnowledgeStore` uses it as the vector record key instead of generating a GUID. This allows `RagFileDeletedHandler` to call `DeleteDocumentAsync("uploaded-documents", fileId.ToString())`.

### Deletion Flow

`RagFileDeletedHandler` implements `IEventHandler<FileDeletedEvent>`:
1. Receives `FileDeletedEvent` (file ID)
2. Calls `IKnowledgeStore.DeleteDocumentAsync("uploaded-documents", fileId.ToString())`
3. No-op if the document doesn't exist in the collection (file was never indexed)

New method on `IKnowledgeStore`:
```csharp
Task DeleteDocumentAsync(string collectionName, string documentId, CancellationToken cancellationToken = default);
```

### Error Handling

Both handlers catch all exceptions internally and log them — they never throw. This is required because `PublishInBackground` runs handlers in a background service where unhandled exceptions would be swallowed with minimal logging. Explicit catch-and-log ensures observability for:
- File deleted between upload and handler execution (`DownloadFileAsync` returns null)
- Ollama timeout during OCR extraction
- Embedding generation failure
- Vector store write failure

## Collection Strategy

- `"uploaded-documents"` — dedicated collection for all user-uploaded files
- Separate from module knowledge sources (e.g., `"product-knowledge"`)
- Agents access it by setting `RagCollectionName = "uploaded-documents"` on their `IAgentDefinition`
- A general-purpose agent could query multiple collections if needed in the future

## Project Structure

```
framework/SimpleModule.Rag.Extraction/              (new project)
  SimpleModule.Rag.Extraction.csproj
  ITextExtractor.cs
  TextExtractionService.cs
  Extractors/
    PlainTextExtractor.cs
    PdfTextExtractor.cs
    OfficeTextExtractor.cs
    VisionOcrExtractor.cs
  RagExtractionExtensions.cs                        (DI registration)

modules/FileStorage/src/SimpleModule.FileStorage.Contracts/
  Events/
    FileUploadedEvent.cs                            (new)
    FileDeletedEvent.cs                             (new)

modules/FileStorage/src/SimpleModule.FileStorage/
  FileStorageService.cs                             (modified — publish FileDeletedEvent)
  Endpoints/Files/UploadEndpoint.cs                 (modified — accept indexForRag, publish FileUploadedEvent)

modules/Rag/src/SimpleModule.Rag.Module/
  RagModule.cs                                      (modified — register event handlers)
  Handlers/
    RagFileIndexingHandler.cs                       (new)
    RagFileDeletedHandler.cs                        (new)

framework/SimpleModule.Rag/
  IKnowledgeStore.cs                                (modified — add DeleteDocumentAsync)
  VectorKnowledgeStore.cs                           (modified — implement DeleteDocumentAsync, support caller-supplied IDs)

framework/SimpleModule.Core/Rag/
  KnowledgeDocument.cs                              (modified — add optional Id property)
```

## Dependencies

New NuGet packages (added to `Directory.Packages.props`):
- `UglyToad.PdfPig` — PDF text extraction
- `DocumentFormat.OpenXml` — Office document parsing

## What This Does NOT Include

- Chunking large documents (full document text indexed as single vector — sufficient for the current scale)
- Re-indexing on file update (files are immutable in this system — upload creates new, delete removes)
- UI changes (the `indexForRag` flag is API-only for now)
- Multi-collection agent queries (agents query one collection at a time)
