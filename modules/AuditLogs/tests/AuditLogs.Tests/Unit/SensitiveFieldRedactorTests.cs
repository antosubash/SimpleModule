using System.Text.Json;
using FluentAssertions;
using SimpleModule.AuditLogs.Enrichment;

namespace AuditLogs.Tests.Unit;

public class SensitiveFieldRedactorTests
{
    [Fact]
    public void Redact_RedactsPasswordFields()
    {
        var input = """{"name":"John","password":"secret123"}""";

        var result = SensitiveFieldRedactor.Redact(input);

        var doc = JsonDocument.Parse(result!);
        doc.RootElement.GetProperty("name").GetString().Should().Be("John");
        doc.RootElement.GetProperty("password").GetString().Should().Be("[REDACTED]");
    }

    [Fact]
    public void Redact_RedactsNestedSensitiveFields()
    {
        var input = """{"user":{"name":"John","token":"abc123"}}""";

        var result = SensitiveFieldRedactor.Redact(input);

        var doc = JsonDocument.Parse(result!);
        var user = doc.RootElement.GetProperty("user");
        user.GetProperty("name").GetString().Should().Be("John");
        user.GetProperty("token").GetString().Should().Be("[REDACTED]");
    }

    [Fact]
    public void Redact_HandlesArrays()
    {
        var input = """[{"name":"John","secret":"x"},{"name":"Jane","secret":"y"}]""";

        var result = SensitiveFieldRedactor.Redact(input);

        var doc = JsonDocument.Parse(result!);
        var arr = doc.RootElement.EnumerateArray().ToList();
        arr[0].GetProperty("name").GetString().Should().Be("John");
        arr[0].GetProperty("secret").GetString().Should().Be("[REDACTED]");
        arr[1].GetProperty("name").GetString().Should().Be("Jane");
        arr[1].GetProperty("secret").GetString().Should().Be("[REDACTED]");
    }

    [Fact]
    public void Redact_ReturnsNullForInvalidJson()
    {
        var result = SensitiveFieldRedactor.Redact("not json");

        result.Should().BeNull();
    }

    [Fact]
    public void Redact_ReturnsNullForNullInput()
    {
        var result = SensitiveFieldRedactor.Redact(null);

        result.Should().BeNull();
    }

    [Fact]
    public void Redact_LeavesNonSensitiveFieldsIntact()
    {
        var input = """{"name":"John","age":30}""";

        var result = SensitiveFieldRedactor.Redact(input);

        var doc = JsonDocument.Parse(result!);
        doc.RootElement.GetProperty("name").GetString().Should().Be("John");
        doc.RootElement.GetProperty("age").GetInt32().Should().Be(30);
    }

    [Theory]
    [InlineData("PASSWORD")]
    [InlineData("Password")]
    [InlineData("password")]
    public void Redact_IsCaseInsensitive(string fieldName)
    {
        var input = $$"""{"{{fieldName}}":"secret123"}""";

        var result = SensitiveFieldRedactor.Redact(input);

        var doc = JsonDocument.Parse(result!);
        doc.RootElement.GetProperty(fieldName).GetString().Should().Be("[REDACTED]");
    }
}
