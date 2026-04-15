using System.Globalization;
using System.Text;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using SimpleModule.AuditLogs.Contracts;

namespace SimpleModule.AuditLogs;

public sealed partial class AuditLogService
{
    private static readonly JsonSerializerOptions s_exportJsonOptions = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
    };

    public async Task<Stream> ExportAsync(AuditExportRequest request)
    {
        var query = BuildQuery(request);
        var entries = await query.OrderByDescending(e => e.Id).AsNoTracking().ToListAsync();

        return request.EffectiveFormat.Equals("json", StringComparison.OrdinalIgnoreCase)
            ? ExportAsJson(entries)
            : ExportAsCsv(entries);
    }

    private static MemoryStream ExportAsCsv(List<AuditEntry> entries)
    {
        var sb = new StringBuilder();
        sb.AppendLine(
            "Timestamp,Source,UserId,UserName,HttpMethod,Path,StatusCode,DurationMs,Module,EntityType,EntityId,Action,Changes"
        );
        foreach (var e in entries)
        {
            sb.Append(CultureInfo.InvariantCulture, $"{e.Timestamp:O},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Source},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.UserName)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.HttpMethod},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Path)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.StatusCode},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.DurationMs},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.Module)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityType)},");
            sb.Append(CultureInfo.InvariantCulture, $"{CsvEscape(e.EntityId)},");
            sb.Append(CultureInfo.InvariantCulture, $"{e.Action},");
            sb.AppendLine(CsvEscape(e.Changes));
        }
        return new MemoryStream(Encoding.UTF8.GetBytes(sb.ToString()));
    }

    private static MemoryStream ExportAsJson(List<AuditEntry> entries)
    {
        var json = JsonSerializer.SerializeToUtf8Bytes(entries, s_exportJsonOptions);
        return new MemoryStream(json);
    }

    private static string CsvEscape(string? value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (
            value.Contains('"', StringComparison.Ordinal)
            || value.Contains(',', StringComparison.Ordinal)
            || value.Contains('\n', StringComparison.Ordinal)
            || value.Contains('\r', StringComparison.Ordinal)
        )
            return $"\"{value.Replace("\"", "\"\"", StringComparison.Ordinal)}\"";
        return value;
    }
}
