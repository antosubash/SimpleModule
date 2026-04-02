using System.Security.Cryptography;
using System.Text;

namespace SimpleModule.Rag.StructuredRag;

public static class ContentHasher
{
    private const string DocumentSeparator = "\n---\n";

    public static string ComputeHash(IEnumerable<string> contents)
    {
        var combined = string.Join(DocumentSeparator, contents);
        var bytes = SHA256.HashData(Encoding.UTF8.GetBytes(combined));
        return Convert.ToHexString(bytes);
    }
}
