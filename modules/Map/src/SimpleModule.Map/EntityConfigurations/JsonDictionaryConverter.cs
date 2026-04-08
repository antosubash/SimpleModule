using System.Text.Json;
using Microsoft.EntityFrameworkCore.ChangeTracking;
using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace SimpleModule.Map.EntityConfigurations;

/// <summary>
/// Maps a <see cref="Dictionary{TKey,TValue}"/> property to a JSON string column with
/// a <see cref="ValueComparer{T}"/> that does element-wise change tracking. Without
/// the comparer, EF Core only diff-checks the reference and never persists in-place
/// dictionary mutations.
/// </summary>
internal static class JsonDictionaryConverter
{
    private static readonly JsonSerializerOptions JsonOptions = new();

    public static PropertyBuilder<Dictionary<string, string>> HasJsonDictionaryConversion(
        this PropertyBuilder<Dictionary<string, string>> property
    )
    {
        return property.HasConversion(
            v => JsonSerializer.Serialize(v, JsonOptions),
            v =>
                string.IsNullOrEmpty(v)
                    ? new Dictionary<string, string>()
                    : JsonSerializer.Deserialize<Dictionary<string, string>>(v, JsonOptions)
                        ?? new Dictionary<string, string>(),
            new ValueComparer<Dictionary<string, string>>(
                (a, b) =>
                    (a == null && b == null)
                    || (a != null && b != null && a.Count == b.Count && !a.Except(b).Any()),
                v =>
                    v == null
                        ? 0
                        : v.Aggregate(0, (h, kv) => HashCode.Combine(h, kv.Key, kv.Value)),
                v =>
                    v == null ? new Dictionary<string, string>() : new Dictionary<string, string>(v)
            )
        );
    }
}
