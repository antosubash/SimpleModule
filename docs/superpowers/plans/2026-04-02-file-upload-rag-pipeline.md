# File Upload → RAG Pipeline Implementation Plan

> **For agentic workers:** REQUIRED: Use superpowers:subagent-driven-development (if subagents available) or superpowers:executing-plans to implement this plan. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Connect file uploads to RAG indexing via event-driven text extraction, supporting plain text, PDF, Office docs, and image OCR.

**Architecture:** Upload endpoint publishes `FileUploadedEvent` via background event bus → `RagFileIndexingHandler` downloads file, extracts text via `ITextExtractor`, indexes into vector store. Deletion publishes `FileDeletedEvent` → handler removes from vector store. Opt-in via `indexForRag` parameter on upload endpoint.

**Tech Stack:** .NET 10, Microsoft.Extensions.AI, Microsoft.Extensions.VectorData, UglyToad.PdfPig (PDF), DocumentFormat.OpenXml (Office), Ollama llama3.2-vision (OCR), xUnit.v3, FluentAssertions, NSubstitute

**Spec:** `docs/superpowers/specs/2026-04-02-file-upload-rag-pipeline-design.md`

---

## Chunk 1: Core Infrastructure

### File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Modify | `Directory.Packages.props` | Add UglyToad.PdfPig + DocumentFormat.OpenXml versions |
| Modify | `SimpleModule.slnx` | Add new Rag.Extraction project |
| Create | `framework/SimpleModule.Rag.Extraction/SimpleModule.Rag.Extraction.csproj` | New project for text extraction |
| Create | `framework/SimpleModule.Rag.Extraction/ITextExtractor.cs` | Extractor interface |
| Create | `framework/SimpleModule.Rag.Extraction/TextExtractionService.cs` | Resolves extractor by content type |
| Create | `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs` | DI registration |
| Modify | `framework/SimpleModule.Core/Rag/KnowledgeDocument.cs` | Add optional `Id` property |
| Modify | `framework/SimpleModule.Rag/IKnowledgeStore.cs` | Add `DeleteDocumentAsync` method |
| Modify | `framework/SimpleModule.Rag/VectorKnowledgeStore.cs` | Implement `DeleteDocumentAsync` + caller-supplied IDs |
| Create | `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileUploadedEvent.cs` | Upload event |
| Create | `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileDeletedEvent.cs` | Deletion event |

---

### Task 1: Add NuGet package versions

**Files:**
- Modify: `Directory.Packages.props`

- [ ] **Step 1: Add package versions**

Add to the `<ItemGroup>` in `Directory.Packages.props`, after the `<!-- RAG / Vector -->` section:

```xml
    <!-- Text Extraction -->
    <PackageVersion Include="UglyToad.PdfPig" Version="0.4.1" />
    <PackageVersion Include="DocumentFormat.OpenXml" Version="3.3.0" />
```

- [ ] **Step 2: Verify build**

Run: `dotnet build --no-restore 2>&1 | tail -3`
Expected: Build succeeded (no-restore is fine since we're only adding versions, not references yet)

- [ ] **Step 3: Commit**

```bash
git add Directory.Packages.props
git commit -m "Add UglyToad.PdfPig and DocumentFormat.OpenXml package versions"
```

---

### Task 2: Add optional Id to KnowledgeDocument

**Files:**
- Modify: `framework/SimpleModule.Core/Rag/KnowledgeDocument.cs`
- Test: `tests/SimpleModule.Core.Tests/Rag/KnowledgeDocumentTests.cs`

- [ ] **Step 1: Write test for optional Id**

Create `tests/SimpleModule.Core.Tests/Rag/KnowledgeDocumentTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Core.Rag;

namespace SimpleModule.Core.Tests.Rag;

public class KnowledgeDocumentTests
{
    [Fact]
    public void Id_Is_Null_By_Default()
    {
        var doc = new KnowledgeDocument("Title", "Content");
        doc.Id.Should().BeNull();
    }

    [Fact]
    public void Id_Can_Be_Set()
    {
        var doc = new KnowledgeDocument("Title", "Content") { Id = "custom-id" };
        doc.Id.Should().Be("custom-id");
    }

    [Fact]
    public void Existing_Constructor_Still_Works()
    {
        var meta = new Dictionary<string, string> { ["key"] = "value" };
        var doc = new KnowledgeDocument("Title", "Content", meta);
        doc.Title.Should().Be("Title");
        doc.Content.Should().Be("Content");
        doc.Metadata.Should().ContainKey("key");
        doc.Id.Should().BeNull();
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "KnowledgeDocumentTests" --no-build 2>&1 | tail -5`
Expected: FAIL — `Id` property doesn't exist

- [ ] **Step 3: Add Id property to KnowledgeDocument**

Modify `framework/SimpleModule.Core/Rag/KnowledgeDocument.cs`:

```csharp
namespace SimpleModule.Core.Rag;

/// <summary>
/// A document to be indexed in the RAG knowledge store.
/// </summary>
/// <param name="Title">Document title for display and citation.</param>
/// <param name="Content">The full text content to embed and search.</param>
/// <param name="Metadata">Optional key-value metadata for filtering.</param>
public sealed record KnowledgeDocument(
    string Title,
    string Content,
    Dictionary<string, string>? Metadata = null
)
{
    /// <summary>
    /// Optional caller-supplied document ID. When set, the knowledge store uses this
    /// as the record key instead of generating a random ID. Enables deletion by known key.
    /// </summary>
    public string? Id { get; init; }
}
```

- [ ] **Step 4: Run test to verify it passes**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "KnowledgeDocumentTests" -v quiet 2>&1 | tail -3`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Core/Rag/KnowledgeDocument.cs tests/SimpleModule.Core.Tests/Rag/KnowledgeDocumentTests.cs
git commit -m "Add optional Id property to KnowledgeDocument for caller-supplied keys"
```

---

### Task 3: Add DeleteDocumentAsync to IKnowledgeStore and VectorKnowledgeStore

**Files:**
- Modify: `framework/SimpleModule.Rag/IKnowledgeStore.cs`
- Modify: `framework/SimpleModule.Rag/VectorKnowledgeStore.cs`

- [ ] **Step 1: Add method to IKnowledgeStore**

Add to `framework/SimpleModule.Rag/IKnowledgeStore.cs` after `DeleteCollectionAsync`:

```csharp
    Task DeleteDocumentAsync(
        string collectionName,
        string documentId,
        CancellationToken cancellationToken = default
    );
```

- [ ] **Step 2: Update VectorKnowledgeStore to support caller-supplied IDs**

In `framework/SimpleModule.Rag/VectorKnowledgeStore.cs`, change the `IndexDocumentsAsync` method. Replace the line:

```csharp
                Id = Guid.NewGuid().ToString(),
```

with:

```csharp
                Id = doc.Id ?? Guid.NewGuid().ToString(),
```

- [ ] **Step 3: Implement DeleteDocumentAsync in VectorKnowledgeStore**

Add to `framework/SimpleModule.Rag/VectorKnowledgeStore.cs` after `DeleteCollectionAsync`:

```csharp
    public async Task DeleteDocumentAsync(
        string collectionName,
        string documentId,
        CancellationToken cancellationToken = default
    )
    {
        var collection = vectorStore.GetCollection<string, KnowledgeRecord>(collectionName);

        if (!await collection.CollectionExistsAsync(cancellationToken))
            return;

        await collection.DeleteAsync(documentId, cancellationToken);
    }
```

- [ ] **Step 4: Verify build**

Run: `dotnet build 2>&1 | tail -3`
Expected: Build succeeded with 0 errors

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Rag/IKnowledgeStore.cs framework/SimpleModule.Rag/VectorKnowledgeStore.cs
git commit -m "Add DeleteDocumentAsync and caller-supplied ID support to knowledge store"
```

---

### Task 4: Create FileStorage events

**Files:**
- Create: `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileUploadedEvent.cs`
- Create: `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileDeletedEvent.cs`

- [ ] **Step 1: Create FileUploadedEvent**

Create `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileUploadedEvent.cs`:

```csharp
using SimpleModule.Core.Events;

namespace SimpleModule.FileStorage.Contracts.Events;

public sealed record FileUploadedEvent(FileStorageId FileId, string ContentType) : IEvent;
```

- [ ] **Step 2: Create FileDeletedEvent**

Create `modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/FileDeletedEvent.cs`:

```csharp
using SimpleModule.Core.Events;

namespace SimpleModule.FileStorage.Contracts.Events;

public sealed record FileDeletedEvent(FileStorageId FileId) : IEvent;
```

- [ ] **Step 3: Verify build**

Run: `dotnet build modules/FileStorage/src/SimpleModule.FileStorage.Contracts 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add modules/FileStorage/src/SimpleModule.FileStorage.Contracts/Events/
git commit -m "Add FileUploadedEvent and FileDeletedEvent to FileStorage contracts"
```

---

### Task 5: Create ITextExtractor interface, TextExtractionService, and project scaffold

**Files:**
- Create: `framework/SimpleModule.Rag.Extraction/SimpleModule.Rag.Extraction.csproj`
- Create: `framework/SimpleModule.Rag.Extraction/ITextExtractor.cs`
- Create: `framework/SimpleModule.Rag.Extraction/TextExtractionService.cs`
- Create: `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs`
- Modify: `SimpleModule.slnx`

- [ ] **Step 1: Create project file**

Create `framework/SimpleModule.Rag.Extraction/SimpleModule.Rag.Extraction.csproj`:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net10.0</TargetFramework>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.Extensions.AI.Abstractions" />
    <PackageReference Include="UglyToad.PdfPig" />
    <PackageReference Include="DocumentFormat.OpenXml" />
    <ProjectReference Include="..\SimpleModule.Core\SimpleModule.Core.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 2: Create ITextExtractor**

Create `framework/SimpleModule.Rag.Extraction/ITextExtractor.cs`:

```csharp
namespace SimpleModule.Rag.Extraction;

public interface ITextExtractor
{
    bool CanExtract(string contentType);
    Task<string> ExtractAsync(Stream content, CancellationToken cancellationToken = default);
}
```

- [ ] **Step 3: Create TextExtractionService**

Create `framework/SimpleModule.Rag.Extraction/TextExtractionService.cs`:

```csharp
using Microsoft.Extensions.Logging;

namespace SimpleModule.Rag.Extraction;

public sealed partial class TextExtractionService(
    IEnumerable<ITextExtractor> extractors,
    ILogger<TextExtractionService> logger
)
{
    public async Task<string?> ExtractAsync(
        Stream content,
        string contentType,
        CancellationToken cancellationToken = default
    )
    {
        var extractor = extractors.FirstOrDefault(e => e.CanExtract(contentType));
        if (extractor is null)
        {
            LogUnsupportedContentType(logger, contentType);
            return null;
        }

        return await extractor.ExtractAsync(content, cancellationToken);
    }

    [LoggerMessage(
        Level = LogLevel.Warning,
        Message = "No text extractor found for content type '{ContentType}'"
    )]
    private static partial void LogUnsupportedContentType(ILogger logger, string contentType);
}
```

- [ ] **Step 4: Create RagExtractionExtensions (empty for now — extractors added in later tasks)**

Create `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;

namespace SimpleModule.Rag.Extraction;

public static class RagExtractionExtensions
{
    public static IServiceCollection AddRagExtraction(this IServiceCollection services)
    {
        services.AddScoped<TextExtractionService>();
        return services;
    }
}
```

- [ ] **Step 5: Add project to solution**

Add to `SimpleModule.slnx` inside the `/framework/` folder, after the Rag.StructuredRag entry:

```xml
    <Project Path="framework/SimpleModule.Rag.Extraction/SimpleModule.Rag.Extraction.csproj" />
```

- [ ] **Step 6: Verify build**

Run: `dotnet build framework/SimpleModule.Rag.Extraction 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 7: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/ SimpleModule.slnx
git commit -m "Scaffold SimpleModule.Rag.Extraction project with ITextExtractor and TextExtractionService"
```

---

## Chunk 2: Text Extractors

### File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Create | `framework/SimpleModule.Rag.Extraction/Extractors/PlainTextExtractor.cs` | text/plain, text/markdown, text/csv |
| Create | `framework/SimpleModule.Rag.Extraction/Extractors/PdfTextExtractor.cs` | application/pdf via PdfPig |
| Create | `framework/SimpleModule.Rag.Extraction/Extractors/OfficeTextExtractor.cs` | docx/xlsx/pptx via OpenXml |
| Create | `framework/SimpleModule.Rag.Extraction/Extractors/VisionOcrExtractor.cs` | images via Ollama vision |
| Modify | `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs` | Register all extractors |
| Create | `tests/SimpleModule.Core.Tests/Rag/Extraction/PlainTextExtractorTests.cs` | Tests for plain text |
| Create | `tests/SimpleModule.Core.Tests/Rag/Extraction/PdfTextExtractorTests.cs` | Tests for PDF |
| Create | `tests/SimpleModule.Core.Tests/Rag/Extraction/OfficeTextExtractorTests.cs` | Tests for Office |
| Create | `tests/SimpleModule.Core.Tests/Rag/Extraction/TextExtractionServiceTests.cs` | Tests for service routing |

---

### Task 6: Implement PlainTextExtractor

**Files:**
- Create: `framework/SimpleModule.Rag.Extraction/Extractors/PlainTextExtractor.cs`
- Create: `tests/SimpleModule.Core.Tests/Rag/Extraction/PlainTextExtractorTests.cs`

- [ ] **Step 1: Write tests**

Create `tests/SimpleModule.Core.Tests/Rag/Extraction/PlainTextExtractorTests.cs`:

```csharp
using System.Text;
using FluentAssertions;
using SimpleModule.Rag.Extraction.Extractors;

namespace SimpleModule.Core.Tests.Rag.Extraction;

public class PlainTextExtractorTests
{
    private readonly PlainTextExtractor _extractor = new();

    [Theory]
    [InlineData("text/plain")]
    [InlineData("text/markdown")]
    [InlineData("text/csv")]
    public void CanExtract_Returns_True_For_Supported_Types(string contentType)
    {
        _extractor.CanExtract(contentType).Should().BeTrue();
    }

    [Theory]
    [InlineData("application/pdf")]
    [InlineData("image/png")]
    public void CanExtract_Returns_False_For_Unsupported_Types(string contentType)
    {
        _extractor.CanExtract(contentType).Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Returns_File_Content()
    {
        var content = "Hello, world!\nLine two.";
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes(content));

        var result = await _extractor.ExtractAsync(stream);

        result.Should().Be(content);
    }
}
```

Note: the test project `tests/SimpleModule.Core.Tests` needs additions to its `.csproj`:

```xml
<PackageReference Include="NSubstitute" />
<ProjectReference Include="..\..\framework\SimpleModule.Rag.Extraction\SimpleModule.Rag.Extraction.csproj" />
```

- [ ] **Step 2: Build and run test to verify it fails**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "PlainTextExtractorTests" 2>&1 | tail -5`
Expected: FAIL — class doesn't exist

- [ ] **Step 3: Implement PlainTextExtractor**

Create `framework/SimpleModule.Rag.Extraction/Extractors/PlainTextExtractor.cs`:

```csharp
namespace SimpleModule.Rag.Extraction.Extractors;

public sealed class PlainTextExtractor : ITextExtractor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "text/plain",
        "text/markdown",
        "text/csv",
    };

    public bool CanExtract(string contentType) => SupportedTypes.Contains(contentType);

    public async Task<string> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        using var reader = new StreamReader(content, leaveOpen: true);
        return await reader.ReadToEndAsync(cancellationToken);
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "PlainTextExtractorTests" -v quiet 2>&1 | tail -3`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/Extractors/PlainTextExtractor.cs tests/SimpleModule.Core.Tests/
git commit -m "Add PlainTextExtractor for text/plain, text/markdown, text/csv"
```

---

### Task 7: Implement PdfTextExtractor

**Files:**
- Create: `framework/SimpleModule.Rag.Extraction/Extractors/PdfTextExtractor.cs`
- Create: `tests/SimpleModule.Core.Tests/Rag/Extraction/PdfTextExtractorTests.cs`

- [ ] **Step 1: Write tests**

Create `tests/SimpleModule.Core.Tests/Rag/Extraction/PdfTextExtractorTests.cs`:

```csharp
using FluentAssertions;
using SimpleModule.Rag.Extraction.Extractors;
using UglyToad.PdfPig;
using UglyToad.PdfPig.Writer;

namespace SimpleModule.Core.Tests.Rag.Extraction;

public class PdfTextExtractorTests
{
    private readonly PdfTextExtractor _extractor = new();

    [Fact]
    public void CanExtract_Returns_True_For_Pdf()
    {
        _extractor.CanExtract("application/pdf").Should().BeTrue();
    }

    [Fact]
    public void CanExtract_Returns_False_For_Non_Pdf()
    {
        _extractor.CanExtract("text/plain").Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Extracts_Text_From_Pdf()
    {
        // Create a simple PDF in memory using PdfPig writer
        using var pdfStream = new MemoryStream();
        var builder = new PdfDocumentBuilder();
        var page = builder.AddPage(595, 842); // A4
        var font = builder.AddStandard14Font(UglyToad.PdfPig.Fonts.Standard14Fonts.Standard14Font.Helvetica);
        page.AddText("Hello from PDF", 12, new UglyToad.PdfPig.Core.PdfPoint(72, 720), font);
        var pdfBytes = builder.Build();
        pdfStream.Write(pdfBytes);
        pdfStream.Position = 0;

        var result = await _extractor.ExtractAsync(pdfStream);

        result.Should().Contain("Hello from PDF");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "PdfTextExtractorTests" 2>&1 | tail -5`
Expected: FAIL — class doesn't exist

- [ ] **Step 3: Implement PdfTextExtractor**

Create `framework/SimpleModule.Rag.Extraction/Extractors/PdfTextExtractor.cs`:

```csharp
using System.Text;
using UglyToad.PdfPig;

namespace SimpleModule.Rag.Extraction.Extractors;

public sealed class PdfTextExtractor : ITextExtractor
{
    public bool CanExtract(string contentType) =>
        contentType.Equals("application/pdf", StringComparison.OrdinalIgnoreCase);

    public Task<string> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        using var document = PdfDocument.Open(content);
        var sb = new StringBuilder();

        foreach (var page in document.GetPages())
        {
            if (sb.Length > 0)
                sb.AppendLine();
            sb.Append(page.Text);
        }

        return Task.FromResult(sb.ToString());
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "PdfTextExtractorTests" -v quiet 2>&1 | tail -3`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/Extractors/PdfTextExtractor.cs tests/SimpleModule.Core.Tests/Rag/Extraction/PdfTextExtractorTests.cs
git commit -m "Add PdfTextExtractor using UglyToad.PdfPig"
```

---

### Task 8: Implement OfficeTextExtractor

**Files:**
- Create: `framework/SimpleModule.Rag.Extraction/Extractors/OfficeTextExtractor.cs`
- Create: `tests/SimpleModule.Core.Tests/Rag/Extraction/OfficeTextExtractorTests.cs`

- [ ] **Step 1: Write tests**

Create `tests/SimpleModule.Core.Tests/Rag/Extraction/OfficeTextExtractorTests.cs`:

```csharp
using System.Text;
using DocumentFormat.OpenXml;
using DocumentFormat.OpenXml.Packaging;
using DocumentFormat.OpenXml.Spreadsheet;
using DocumentFormat.OpenXml.Wordprocessing;
using FluentAssertions;
using SimpleModule.Rag.Extraction.Extractors;

namespace SimpleModule.Core.Tests.Rag.Extraction;

public class OfficeTextExtractorTests
{
    private readonly OfficeTextExtractor _extractor = new();

    [Theory]
    [InlineData("application/vnd.openxmlformats-officedocument.wordprocessingml.document")]
    [InlineData("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")]
    [InlineData("application/vnd.openxmlformats-officedocument.presentationml.presentation")]
    public void CanExtract_Returns_True_For_Office_Types(string contentType)
    {
        _extractor.CanExtract(contentType).Should().BeTrue();
    }

    [Fact]
    public void CanExtract_Returns_False_For_Non_Office()
    {
        _extractor.CanExtract("text/plain").Should().BeFalse();
    }

    [Fact]
    public async Task ExtractAsync_Extracts_Text_From_Docx()
    {
        using var stream = new MemoryStream();
        using (var doc = WordprocessingDocument.Create(stream, WordprocessingDocumentType.Document))
        {
            var mainPart = doc.AddMainDocumentPart();
            mainPart.Document = new Document(
                new Body(
                    new DocumentFormat.OpenXml.Wordprocessing.Paragraph(
                        new DocumentFormat.OpenXml.Wordprocessing.Run(
                            new DocumentFormat.OpenXml.Wordprocessing.Text("Hello from Word")
                        )
                    )
                )
            );
        }

        stream.Position = 0;
        var result = await _extractor.ExtractAsync(stream);

        result.Should().Contain("Hello from Word");
    }

    [Fact]
    public async Task ExtractAsync_Extracts_Text_From_Xlsx()
    {
        using var stream = new MemoryStream();
        using (var doc = SpreadsheetDocument.Create(stream, SpreadsheetDocumentType.Workbook))
        {
            var workbookPart = doc.AddWorkbookPart();
            workbookPart.Workbook = new Workbook();
            var sheets = workbookPart.Workbook.AppendChild(new Sheets());

            var worksheetPart = workbookPart.AddNewPart<WorksheetPart>();
            worksheetPart.Worksheet = new Worksheet(
                new SheetData(
                    new Row(
                        new Cell
                        {
                            DataType = CellValues.InlineString,
                            InlineString = new InlineString(new DocumentFormat.OpenXml.Spreadsheet.Text("Hello from Excel")),
                        }
                    )
                )
            );

            sheets.Append(new Sheet
            {
                Id = workbookPart.GetIdOfPart(worksheetPart),
                SheetId = 1,
                Name = "Sheet1",
            });
        }

        stream.Position = 0;
        var result = await _extractor.ExtractAsync(stream);

        result.Should().Contain("Hello from Excel");
    }
}
```

- [ ] **Step 2: Run test to verify it fails**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "OfficeTextExtractorTests" 2>&1 | tail -5`
Expected: FAIL — class doesn't exist

- [ ] **Step 3: Implement OfficeTextExtractor**

Create `framework/SimpleModule.Rag.Extraction/Extractors/OfficeTextExtractor.cs`:

```csharp
using System.Text;
using DocumentFormat.OpenXml.Packaging;

namespace SimpleModule.Rag.Extraction.Extractors;

public sealed class OfficeTextExtractor : ITextExtractor
{
    public bool CanExtract(string contentType) =>
        contentType.StartsWith(
            "application/vnd.openxmlformats-officedocument",
            StringComparison.OrdinalIgnoreCase
        );

    public Task<string> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        // Copy to MemoryStream because OpenXml needs a seekable stream
        using var ms = new MemoryStream();
        content.CopyTo(ms);
        ms.Position = 0;

        var text = ExtractFromWordprocessing(ms)
            ?? ExtractFromSpreadsheet(ms)
            ?? ExtractFromPresentation(ms)
            ?? string.Empty;

        return Task.FromResult(text);
    }

    private static string? ExtractFromWordprocessing(MemoryStream ms)
    {
        try
        {
            ms.Position = 0;
            using var doc = WordprocessingDocument.Open(ms, false);
            var body = doc.MainDocumentPart?.Document.Body;
            return body?.InnerText;
        }
#pragma warning disable CA1031 // Intentional: try each format, fall through on mismatch
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private static string? ExtractFromSpreadsheet(MemoryStream ms)
    {
        try
        {
            ms.Position = 0;
            using var doc = SpreadsheetDocument.Open(ms, false);
            var sb = new StringBuilder();

            var workbookPart = doc.WorkbookPart;
            if (workbookPart is null)
                return null;

            foreach (var worksheetPart in workbookPart.WorksheetParts)
            {
                var sheetData = worksheetPart.Worksheet.GetFirstChild<DocumentFormat.OpenXml.Spreadsheet.SheetData>();
                if (sheetData is null)
                    continue;

                foreach (var row in sheetData.Elements<DocumentFormat.OpenXml.Spreadsheet.Row>())
                {
                    foreach (var cell in row.Elements<DocumentFormat.OpenXml.Spreadsheet.Cell>())
                    {
                        var value = GetCellValue(cell, workbookPart);
                        if (!string.IsNullOrEmpty(value))
                        {
                            if (sb.Length > 0)
                                sb.Append('\t');
                            sb.Append(value);
                        }
                    }

                    if (sb.Length > 0)
                        sb.AppendLine();
                }
            }

            return sb.ToString();
        }
#pragma warning disable CA1031 // Intentional: try each format, fall through on mismatch
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }

    private static string GetCellValue(
        DocumentFormat.OpenXml.Spreadsheet.Cell cell,
        WorkbookPart workbookPart
    )
    {
        if (cell.InlineString is not null)
            return cell.InlineString.InnerText;

        var value = cell.CellValue?.InnerText ?? string.Empty;

        if (
            cell.DataType?.Value == DocumentFormat.OpenXml.Spreadsheet.CellValues.SharedString
            && int.TryParse(value, out var index)
        )
        {
            var sst = workbookPart.SharedStringTablePart?.SharedStringTable;
            if (sst is not null)
                return sst.ElementAt(index).InnerText;
        }

        return value;
    }

    private static string? ExtractFromPresentation(MemoryStream ms)
    {
        try
        {
            ms.Position = 0;
            using var doc = PresentationDocument.Open(ms, false);
            var sb = new StringBuilder();

            var presentationPart = doc.PresentationPart;
            if (presentationPart is null)
                return null;

            foreach (var slidePart in presentationPart.SlideParts)
            {
                var text = slidePart.Slide.InnerText;
                if (!string.IsNullOrWhiteSpace(text))
                {
                    if (sb.Length > 0)
                        sb.AppendLine();
                    sb.Append(text);
                }
            }

            return sb.ToString();
        }
#pragma warning disable CA1031 // Intentional: try each format, fall through on mismatch
        catch
        {
            return null;
        }
#pragma warning restore CA1031
    }
}
```

- [ ] **Step 4: Run tests**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "OfficeTextExtractorTests" -v quiet 2>&1 | tail -3`
Expected: PASS

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/Extractors/OfficeTextExtractor.cs tests/SimpleModule.Core.Tests/Rag/Extraction/OfficeTextExtractorTests.cs
git commit -m "Add OfficeTextExtractor for docx, xlsx, pptx via OpenXml"
```

---

### Task 9: Implement VisionOcrExtractor

**Files:**
- Create: `framework/SimpleModule.Rag.Extraction/Extractors/VisionOcrExtractor.cs`

The VisionOcrExtractor calls Ollama's vision model, which requires a running server. We test it via integration test later. For now, just implement it.

- [ ] **Step 1: Implement VisionOcrExtractor**

Create `framework/SimpleModule.Rag.Extraction/Extractors/VisionOcrExtractor.cs`:

```csharp
using Microsoft.Extensions.AI;
using Microsoft.Extensions.Logging;

namespace SimpleModule.Rag.Extraction.Extractors;

public sealed partial class VisionOcrExtractor(
    IChatClient chatClient,
    ILogger<VisionOcrExtractor> logger
) : ITextExtractor
{
    private static readonly HashSet<string> SupportedTypes = new(StringComparer.OrdinalIgnoreCase)
    {
        "image/png",
        "image/jpeg",
        "image/webp",
        "image/gif",
        "image/tiff",
    };

    public bool CanExtract(string contentType) => SupportedTypes.Contains(contentType);

    public async Task<string> ExtractAsync(
        Stream content,
        CancellationToken cancellationToken = default
    )
    {
        using var ms = new MemoryStream();
        await content.CopyToAsync(ms, cancellationToken);
        var imageBytes = ms.ToArray();

        var mediaType = "image/png"; // Default; actual type checked via CanExtract
        var message = new ChatMessage(
            ChatRole.User,
            [
                new TextContent(
                    "Extract all visible text from this image. Return only the extracted text, nothing else. If there is no text, return an empty string."
                ),
                new DataContent(imageBytes, mediaType),
            ]
        );

        try
        {
            var response = await chatClient.GetResponseAsync(
                [message],
                cancellationToken: cancellationToken
            );
            return response.Text ?? string.Empty;
        }
#pragma warning disable CA1031 // OCR extraction is best-effort; failures should not crash the pipeline
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogOcrFailed(logger, ex);
            return string.Empty;
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "Vision OCR extraction failed")]
    private static partial void LogOcrFailed(ILogger logger, Exception exception);
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build framework/SimpleModule.Rag.Extraction 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/Extractors/VisionOcrExtractor.cs
git commit -m "Add VisionOcrExtractor for image OCR via Ollama vision model"
```

---

### Task 10: Register all extractors and write TextExtractionService tests

**Files:**
- Modify: `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs`
- Create: `tests/SimpleModule.Core.Tests/Rag/Extraction/TextExtractionServiceTests.cs`

- [ ] **Step 1: Write tests for TextExtractionService**

Create `tests/SimpleModule.Core.Tests/Rag/Extraction/TextExtractionServiceTests.cs`:

```csharp
using System.Text;
using FluentAssertions;
using Microsoft.Extensions.Logging.Abstractions;
using NSubstitute;
using SimpleModule.Rag.Extraction;

namespace SimpleModule.Core.Tests.Rag.Extraction;

public class TextExtractionServiceTests
{
    [Fact]
    public async Task ExtractAsync_Returns_Null_For_Unsupported_Type()
    {
        var service = new TextExtractionService([], NullLogger<TextExtractionService>.Instance);

        var result = await service.ExtractAsync(Stream.Null, "application/octet-stream");

        result.Should().BeNull();
    }

    [Fact]
    public async Task ExtractAsync_Delegates_To_Matching_Extractor()
    {
        var extractor = Substitute.For<ITextExtractor>();
        extractor.CanExtract("text/plain").Returns(true);
        extractor
            .ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("extracted text");

        var service = new TextExtractionService(
            [extractor],
            NullLogger<TextExtractionService>.Instance
        );
        using var stream = new MemoryStream(Encoding.UTF8.GetBytes("content"));

        var result = await service.ExtractAsync(stream, "text/plain");

        result.Should().Be("extracted text");
    }

    [Fact]
    public async Task ExtractAsync_Skips_Non_Matching_Extractors()
    {
        var pdfExtractor = Substitute.For<ITextExtractor>();
        pdfExtractor.CanExtract("text/plain").Returns(false);

        var textExtractor = Substitute.For<ITextExtractor>();
        textExtractor.CanExtract("text/plain").Returns(true);
        textExtractor
            .ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>())
            .Returns("found");

        var service = new TextExtractionService(
            [pdfExtractor, textExtractor],
            NullLogger<TextExtractionService>.Instance
        );

        var result = await service.ExtractAsync(Stream.Null, "text/plain");

        result.Should().Be("found");
        await pdfExtractor.DidNotReceive().ExtractAsync(Arg.Any<Stream>(), Arg.Any<CancellationToken>());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "TextExtractionServiceTests" 2>&1 | tail -5`
Expected: Should pass since `TextExtractionService` already exists — this validates the implementation

- [ ] **Step 3: Update RagExtractionExtensions to register all extractors**

Replace `framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs`:

```csharp
using Microsoft.Extensions.DependencyInjection;
using SimpleModule.Rag.Extraction.Extractors;

namespace SimpleModule.Rag.Extraction;

public static class RagExtractionExtensions
{
    public static IServiceCollection AddRagExtraction(this IServiceCollection services)
    {
        services.AddScoped<TextExtractionService>();
        services.AddSingleton<ITextExtractor, PlainTextExtractor>();
        services.AddSingleton<ITextExtractor, PdfTextExtractor>();
        services.AddSingleton<ITextExtractor, OfficeTextExtractor>();
        services.AddScoped<ITextExtractor, VisionOcrExtractor>();
        return services;
    }
}
```

- [ ] **Step 4: Run all extraction tests**

Run: `dotnet test tests/SimpleModule.Core.Tests --filter "Extraction" -v quiet 2>&1 | tail -5`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add framework/SimpleModule.Rag.Extraction/RagExtractionExtensions.cs tests/SimpleModule.Core.Tests/Rag/Extraction/TextExtractionServiceTests.cs
git commit -m "Register all text extractors and add TextExtractionService tests"
```

---

## Chunk 3: Event Handlers & Wiring

### File Map

| Action | Path | Responsibility |
|--------|------|---------------|
| Modify | `modules/FileStorage/src/SimpleModule.FileStorage/Endpoints/Files/UploadEndpoint.cs` | Publish FileUploadedEvent when indexForRag=true |
| Modify | `modules/FileStorage/src/SimpleModule.FileStorage/FileStorageService.cs` | Publish FileDeletedEvent on delete |
| Create | `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileIndexingHandler.cs` | Handle upload → extract → index |
| Create | `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileDeletedHandler.cs` | Handle delete → remove from store |
| Modify | `modules/Rag/src/SimpleModule.Rag.Module/RagModule.cs` | Register handlers |
| Modify | `modules/Rag/src/SimpleModule.Rag.Module/SimpleModule.Rag.Module.csproj` | Add references |
| Modify | `template/SimpleModule.Host/Program.cs` | Add `AddRagExtraction()` |
| Modify | `template/SimpleModule.Host/SimpleModule.Host.csproj` | Add reference to Rag.Extraction |

---

### Task 11: Publish FileUploadedEvent from upload endpoint

**Files:**
- Modify: `modules/FileStorage/src/SimpleModule.FileStorage/Endpoints/Files/UploadEndpoint.cs`

- [ ] **Step 1: Update upload endpoint to accept indexForRag and publish event**

Replace `modules/FileStorage/src/SimpleModule.FileStorage/Endpoints/Files/UploadEndpoint.cs`:

```csharp
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Routing;
using SimpleModule.Core;
using SimpleModule.Core.Authorization;
using SimpleModule.Core.Events;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.Contracts.Events;

namespace SimpleModule.FileStorage.Endpoints.Files;

public class UploadEndpoint : IEndpoint
{
    public void Map(IEndpointRouteBuilder app) =>
        app.MapPost(
                "/",
                async Task<IResult> (
                    IFormFile? file,
                    string? folder,
                    bool? indexForRag,
                    IFileStorageContracts files,
                    IEventBus eventBus
                ) =>
                {
                    if (file is null || file.Length == 0)
                    {
                        return TypedResults.BadRequest("A file is required.");
                    }

                    await using var stream = file.OpenReadStream();
                    var storedFile = await files.UploadFileAsync(
                        stream,
                        file.FileName,
                        file.ContentType,
                        folder
                    );

                    if (indexForRag == true)
                    {
                        eventBus.PublishInBackground(
                            new FileUploadedEvent(storedFile.Id, storedFile.ContentType)
                        );
                    }

                    return TypedResults.Created($"/api/files/{storedFile.Id}", storedFile);
                }
            )
            .RequirePermission(FileStoragePermissions.Upload)
            .DisableAntiforgery();
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build modules/FileStorage/src/SimpleModule.FileStorage 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add modules/FileStorage/src/SimpleModule.FileStorage/Endpoints/Files/UploadEndpoint.cs
git commit -m "Publish FileUploadedEvent from upload endpoint when indexForRag is true"
```

---

### Task 12: Publish FileDeletedEvent from FileStorageService

**Files:**
- Modify: `modules/FileStorage/src/SimpleModule.FileStorage/FileStorageService.cs`

- [ ] **Step 1: Add IEventBus to constructor and publish on delete**

In `modules/FileStorage/src/SimpleModule.FileStorage/FileStorageService.cs`:

Add `IEventBus eventBus` to the primary constructor parameters and add `using SimpleModule.Core.Events;` and `using SimpleModule.FileStorage.Contracts.Events;`.

In the `DeleteFileAsync` method, after the successful `db.SaveChangesAsync()` call and before the storage deletion try/catch, add:

```csharp
        eventBus.PublishInBackground(new FileDeletedEvent(id));
```

- [ ] **Step 2: Verify build**

Run: `dotnet build modules/FileStorage/src/SimpleModule.FileStorage 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 3: Run existing FileStorage tests to check nothing broke**

First, add `<PackageReference Include="NSubstitute" />` to `modules/FileStorage/tests/SimpleModule.FileStorage.Tests/SimpleModule.FileStorage.Tests.csproj` if not already present.

Run: `dotnet test modules/FileStorage/tests/SimpleModule.FileStorage.Tests -v quiet 2>&1 | tail -5`
Expected: Tests may fail because `FileStorageService` now requires `IEventBus` — if so, update the test to provide `NSubstitute.Substitute.For<IEventBus>()` to the constructor call.

- [ ] **Step 4: Fix test if needed and verify all pass**

Run: `dotnet test modules/FileStorage/tests/SimpleModule.FileStorage.Tests -v quiet 2>&1 | tail -5`
Expected: All PASS

- [ ] **Step 5: Commit**

```bash
git add modules/FileStorage/src/SimpleModule.FileStorage/FileStorageService.cs modules/FileStorage/tests/
git commit -m "Publish FileDeletedEvent on file deletion"
```

---

### Task 13: Create RagFileIndexingHandler

**Files:**
- Create: `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileIndexingHandler.cs`
- Modify: `modules/Rag/src/SimpleModule.Rag.Module/SimpleModule.Rag.Module.csproj`

- [ ] **Step 1: Add project references to Rag.Module**

Add to `modules/Rag/src/SimpleModule.Rag.Module/SimpleModule.Rag.Module.csproj` `<ItemGroup>`:

```xml
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Rag\SimpleModule.Rag.csproj" />
    <ProjectReference Include="..\..\..\..\framework\SimpleModule.Rag.Extraction\SimpleModule.Rag.Extraction.csproj" />
    <ProjectReference Include="..\..\..\..\modules\FileStorage\src\SimpleModule.FileStorage.Contracts\SimpleModule.FileStorage.Contracts.csproj" />
```

- [ ] **Step 2: Implement RagFileIndexingHandler**

Create `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileIndexingHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.Core.Rag;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.Contracts.Events;
using SimpleModule.Rag.Extraction;

namespace SimpleModule.Rag.Module.Handlers;

public sealed partial class RagFileIndexingHandler(
    IFileStorageContracts fileStorage,
    IKnowledgeStore knowledgeStore,
    TextExtractionService extractionService,
    ILogger<RagFileIndexingHandler> logger
) : IEventHandler<FileUploadedEvent>
{
    private const string CollectionName = "uploaded-documents";

    public async Task HandleAsync(
        FileUploadedEvent @event,
        CancellationToken cancellationToken
    )
    {
        try
        {
            var file = await fileStorage.GetFileByIdAsync(@event.FileId);
            if (file is null)
            {
                LogFileNotFound(logger, @event.FileId);
                return;
            }

            using var stream = await fileStorage.DownloadFileAsync(@event.FileId);
            if (stream is null)
            {
                LogFileNotFound(logger, @event.FileId);
                return;
            }

            var text = await extractionService.ExtractAsync(
                stream,
                @event.ContentType,
                cancellationToken
            );

            if (string.IsNullOrWhiteSpace(text))
            {
                LogExtractionEmpty(logger, file.FileName, @event.ContentType);
                return;
            }

            var document = new KnowledgeDocument(
                file.FileName,
                text,
                new Dictionary<string, string>
                {
                    ["source"] = "file-upload",
                    ["fileId"] = @event.FileId.ToString(),
                    ["fileName"] = file.FileName,
                }
            )
            {
                Id = @event.FileId.ToString(),
            };

            await knowledgeStore.IndexDocumentsAsync(
                CollectionName,
                [document],
                cancellationToken
            );

            LogFileIndexed(logger, file.FileName, @event.FileId);
        }
#pragma warning disable CA1031 // Handler must not throw — runs in background event dispatcher
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogIndexingFailed(logger, ex, @event.FileId);
        }
    }

    [LoggerMessage(Level = LogLevel.Warning, Message = "File {FileId} not found for RAG indexing")]
    private static partial void LogFileNotFound(ILogger logger, FileStorageId fileId);

    [LoggerMessage(
        Level = LogLevel.Debug,
        Message = "No text extracted from '{FileName}' (type: {ContentType})"
    )]
    private static partial void LogExtractionEmpty(
        ILogger logger,
        string fileName,
        string contentType
    );

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Indexed file '{FileName}' ({FileId}) into RAG"
    )]
    private static partial void LogFileIndexed(ILogger logger, string fileName, FileStorageId fileId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to index file {FileId} into RAG"
    )]
    private static partial void LogIndexingFailed(
        ILogger logger,
        Exception exception,
        FileStorageId fileId
    );
}
```

- [ ] **Step 3: Verify build**

Run: `dotnet build modules/Rag/src/SimpleModule.Rag.Module 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 4: Commit**

```bash
git add modules/Rag/src/SimpleModule.Rag.Module/
git commit -m "Add RagFileIndexingHandler for file upload → RAG indexing"
```

---

### Task 14: Create RagFileDeletedHandler

**Files:**
- Create: `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileDeletedHandler.cs`

- [ ] **Step 1: Implement RagFileDeletedHandler**

Create `modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileDeletedHandler.cs`:

```csharp
using Microsoft.Extensions.Logging;
using SimpleModule.Core.Events;
using SimpleModule.FileStorage.Contracts;
using SimpleModule.FileStorage.Contracts.Events;

namespace SimpleModule.Rag.Module.Handlers;

public sealed partial class RagFileDeletedHandler(
    IKnowledgeStore knowledgeStore,
    ILogger<RagFileDeletedHandler> logger
) : IEventHandler<FileDeletedEvent>
{
    private const string CollectionName = "uploaded-documents";

    public async Task HandleAsync(FileDeletedEvent @event, CancellationToken cancellationToken)
    {
        try
        {
            await knowledgeStore.DeleteDocumentAsync(
                CollectionName,
                @event.FileId.ToString(),
                cancellationToken
            );
            LogDocumentRemoved(logger, @event.FileId);
        }
#pragma warning disable CA1031 // Handler must not throw — runs in background event dispatcher
        catch (Exception ex)
#pragma warning restore CA1031
        {
            LogDeletionFailed(logger, ex, @event.FileId);
        }
    }

    [LoggerMessage(
        Level = LogLevel.Information,
        Message = "Removed RAG document for file {FileId}"
    )]
    private static partial void LogDocumentRemoved(ILogger logger, FileStorageId fileId);

    [LoggerMessage(
        Level = LogLevel.Error,
        Message = "Failed to remove RAG document for file {FileId}"
    )]
    private static partial void LogDeletionFailed(
        ILogger logger,
        Exception exception,
        FileStorageId fileId
    );
}
```

- [ ] **Step 2: Verify build**

Run: `dotnet build modules/Rag/src/SimpleModule.Rag.Module 2>&1 | tail -3`
Expected: Build succeeded

- [ ] **Step 3: Commit**

```bash
git add modules/Rag/src/SimpleModule.Rag.Module/Handlers/RagFileDeletedHandler.cs
git commit -m "Add RagFileDeletedHandler to remove indexed documents on file deletion"
```

---

### Task 15: Register handlers in RagModule and wire up host

**Files:**
- Modify: `modules/Rag/src/SimpleModule.Rag.Module/RagModule.cs`
- Modify: `template/SimpleModule.Host/Program.cs`
- Modify: `template/SimpleModule.Host/SimpleModule.Host.csproj`

- [ ] **Step 1: Register event handlers in RagModule.ConfigureServices**

In `modules/Rag/src/SimpleModule.Rag.Module/RagModule.cs`, add usings:

```csharp
using SimpleModule.Core.Events;
using SimpleModule.FileStorage.Contracts.Events;
using SimpleModule.Rag.Module.Handlers;
```

Add to `ConfigureServices` after the existing registrations:

```csharp
        services.AddScoped<IEventHandler<FileUploadedEvent>, RagFileIndexingHandler>();
        services.AddScoped<IEventHandler<FileDeletedEvent>, RagFileDeletedHandler>();
```

- [ ] **Step 2: Add Rag.Extraction reference to Host**

Add to `template/SimpleModule.Host/SimpleModule.Host.csproj` `<ItemGroup>`:

```xml
    <ProjectReference Include="..\..\framework\SimpleModule.Rag.Extraction\SimpleModule.Rag.Extraction.csproj" />
```

- [ ] **Step 3: Add AddRagExtraction() to Program.cs**

In `template/SimpleModule.Host/Program.cs`, add using:

```csharp
using SimpleModule.Rag.Extraction;
```

Add after `builder.Services.AddStructuredRag(builder.Configuration);`:

```csharp
builder.Services.AddRagExtraction();
```

- [ ] **Step 4: Verify full solution build**

Run: `dotnet build 2>&1 | tail -5`
Expected: Build succeeded with 0 errors

- [ ] **Step 5: Run all tests**

Run: `dotnet test 2>&1 | tail -10`
Expected: All tests pass. If any FileStorage tests fail due to the new `IEventBus` parameter, fix them (add mock).

- [ ] **Step 6: Commit**

```bash
git add modules/Rag/src/SimpleModule.Rag.Module/RagModule.cs template/SimpleModule.Host/Program.cs template/SimpleModule.Host/SimpleModule.Host.csproj
git commit -m "Wire up RAG extraction: register handlers, add extraction to host"
```

---

## Chunk 4: Integration Test

### Task 16: End-to-end smoke test

This is a manual verification step. Start the app, upload a text file with `indexForRag=true`, and verify the agent can find the content.

- [ ] **Step 1: Start the app**

```bash
rm -f template/SimpleModule.Host/app.db
dotnet run --project template/SimpleModule.Host &
sleep 15
```

- [ ] **Step 2: Acquire a token**

Use the PKCE login flow (same Python script from earlier testing) to get a Bearer token. Save to `/tmp/sm_token.txt`.

- [ ] **Step 3: Upload a text file with indexForRag=true**

```bash
TOKEN=$(cat /tmp/sm_token.txt)
echo "SimpleModule is a modular monolith framework for .NET. It supports compile-time module discovery via Roslyn source generators." > /tmp/test-doc.txt
curl -s -k -X POST "https://localhost:5001/api/files/?indexForRag=true" \
  -H "Authorization: Bearer $TOKEN" \
  -F "file=@/tmp/test-doc.txt;type=text/plain" | python3 -m json.tool
```

Expected: 201 Created with file metadata

- [ ] **Step 4: Wait for background indexing and verify via agent**

Wait a few seconds for the background event handler to process, then query an agent that uses the `uploaded-documents` collection. If no agent is configured for that collection yet, test via the `product-search` agent by temporarily changing its `RagCollectionName` or by checking server logs for "Indexed file" message.

```bash
# Check logs for successful indexing
grep "Indexed file" <server-log-file>
```

Expected: Log line showing the file was indexed

- [ ] **Step 5: Stop the app and commit any test fixes**

```bash
pkill -f "SimpleModule.Host"
```
