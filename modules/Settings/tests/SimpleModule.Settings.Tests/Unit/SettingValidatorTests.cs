using System.Text.Json;
using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using NSubstitute;
using SimpleModule.Core.Settings;
using SimpleModule.Database;
using SimpleModule.Settings;
using SimpleModule.Settings.Contracts;
using Wolverine;
using ZiggyCreatures.Caching.Fusion;

namespace Settings.Tests.Unit;

/// <summary>
/// Tests for validation behavior covering all 11 SettingTypes, boundary conditions,
/// the Required flag, and edge cases that reveal bugs.
///
/// SettingValidator is internal — tests exercise it via SettingsService.SetSettingAsync,
/// which is the only path that calls Validate() in production.
/// </summary>
public sealed class SettingValidatorTests : IDisposable
{
    private readonly SettingsDbContext _db;
    private readonly FusionCache _cache;

    public SettingValidatorTests()
    {
        var options = new DbContextOptionsBuilder<SettingsDbContext>()
            .UseInMemoryDatabase(Guid.NewGuid().ToString())
            .Options;
        var dbOptions = Options.Create(
            new DatabaseOptions { DefaultConnection = "Data Source=:memory:" }
        );
        _db = new SettingsDbContext(options, dbOptions);
        _db.Database.EnsureCreated();
        _cache = new FusionCache(new FusionCacheOptions());
    }

    private SettingsService BuildService(params SettingDefinition[] definitions)
    {
        var registry = new SettingsDefinitionRegistry([.. definitions]);
        return new SettingsService(
            _db,
            registry,
            _cache,
            new Lazy<IMessageBus>(() => Substitute.For<IMessageBus>()),
            Options.Create(new SettingsModuleOptions()),
            NullLogger<SettingsService>.Instance
        );
    }

    private static JsonElement Json(string raw) => JsonSerializer.Deserialize<JsonElement>(raw);

    // -----------------------------------------------------------------------
    // Bool
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Bool_AcceptsTrue()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "b",
                DisplayName = "B",
                Type = SettingType.Bool,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("b", Json("true"), SettingScope.Application))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Bool_AcceptsFalse()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "b",
                DisplayName = "B",
                Type = SettingType.Bool,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("b", Json("false"), SettingScope.Application))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Bool_RejectsStringYes_ThrowsValidationException()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "b",
                DisplayName = "B",
                Type = SettingType.Bool,
                Scope = SettingScope.Application,
            }
        );
        var act = () => svc.SetSettingAsync("b", Json("\"yes\""), SettingScope.Application);
        var ex = await act.Should().ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*boolean*");
    }

    [Fact]
    public async Task Validate_Bool_RejectsNumber1_ThrowsValidationException()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "b",
                DisplayName = "B",
                Type = SettingType.Bool,
                Scope = SettingScope.Application,
            }
        );
        var act = () => svc.SetSettingAsync("b", Json("1"), SettingScope.Application);
        await act.Should().ThrowAsync<SettingValidationException>();
    }

    // -----------------------------------------------------------------------
    // Number
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Number_AcceptsInteger()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "n",
                DisplayName = "N",
                Type = SettingType.Number,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("n", Json("42"), SettingScope.Application))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Number_RejectsString()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "n",
                DisplayName = "N",
                Type = SettingType.Number,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("n", Json("\"not-a-number\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    [Theory]
    [InlineData("4")] // below min=5
    [InlineData("11")] // above max=10
    public async Task Validate_Number_RejectsOutOfRange(string valueJson)
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "n",
                DisplayName = "N",
                Type = SettingType.Number,
                Scope = SettingScope.Application,
                Min = 5,
                Max = 10,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("n", Json(valueJson), SettingScope.Application))
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    [Theory]
    [InlineData("5")] // at min
    [InlineData("10")] // at max
    [InlineData("7")] // in range
    public async Task Validate_Number_AcceptsAtBoundaryAndWithinRange(string valueJson)
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "n",
                DisplayName = "N",
                Type = SettingType.Number,
                Scope = SettingScope.Application,
                Min = 5,
                Max = 10,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("n", Json(valueJson), SettingScope.Application))
            .Should()
            .NotThrowAsync();
    }

    // -----------------------------------------------------------------------
    // Color
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Color_AcceptsValidHex()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "c",
                DisplayName = "C",
                Type = SettingType.Color,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("c", Json("\"#3b82f6\""), SettingScope.Application)
            )
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Color_RejectsInvalidFormat()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "c",
                DisplayName = "C",
                Type = SettingType.Color,
                Scope = SettingScope.Application,
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("c", Json("\"red\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*hex*");
    }

    [Fact]
    public async Task Validate_Color_Rejects3DigitShorthand()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "c",
                DisplayName = "C",
                Type = SettingType.Color,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("c", Json("\"#fff\""), SettingScope.Application))
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    // -----------------------------------------------------------------------
    // Email
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Email_AcceptsValidAddress()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "e",
                DisplayName = "E",
                Type = SettingType.Email,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("e", Json("\"user@example.com\""), SettingScope.Application)
            )
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Email_RejectsMissingAtSign()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "e",
                DisplayName = "E",
                Type = SettingType.Email,
                Scope = SettingScope.Application,
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("e", Json("\"not-an-email\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*email*");
    }

    /// <summary>
    /// Bug: when an Email definition also carries a Pattern field (as app.support_email does),
    /// the Email type check AND the Pattern check both fire, producing two error messages
    /// for the same constraint. The caller receives duplicate errors in the ValidationProblem.
    ///
    /// This test documents the CURRENT (buggy) behavior and will start failing
    /// when the duplication is fixed to produce only a single error.
    /// </summary>
    [Fact]
    public async Task Validate_Email_WithPattern_ProducesSingleError()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "e",
                DisplayName = "E",
                Type = SettingType.Email,
                Scope = SettingScope.Application,
                Pattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$",
            }
        );

        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("e", Json("\"not-an-email\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();

        // Pattern validation is skipped for typed validators (Email/Url/Color/DateTime),
        // so only the Email-format error fires.
        ex.Which.Errors.Should().HaveCount(1);
    }

    // -----------------------------------------------------------------------
    // Url
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Url_AcceptsAbsoluteHttps()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "u",
                DisplayName = "U",
                Type = SettingType.Url,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync(
                    "u",
                    Json("\"https://example.com/path\""),
                    SettingScope.Application
                )
            )
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Url_RejectsRelativePath()
    {
        // On Linux, Uri.TryCreate("/relative/path", UriKind.Absolute) returns true (file://),
        // so the validator additionally requires the scheme to be http or https.
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "u",
                DisplayName = "U",
                Type = SettingType.Url,
                Scope = SettingScope.Application,
            }
        );

        await svc.Invoking(s =>
                s.SetSettingAsync("u", Json("\"/relative/path\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    [Fact]
    public async Task Validate_Url_RejectsPlainString()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "u",
                DisplayName = "U",
                Type = SettingType.Url,
                Scope = SettingScope.Application,
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("u", Json("\"not-a-url\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*URL*");
    }

    // -----------------------------------------------------------------------
    // DateTime
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_DateTime_AcceptsIso8601()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "dt",
                DisplayName = "DT",
                Type = SettingType.DateTime,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("dt", Json("\"2025-01-15T14:30:00Z\""), SettingScope.Application)
            )
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_DateTime_RejectsArbitraryString()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "dt",
                DisplayName = "DT",
                Type = SettingType.DateTime,
                Scope = SettingScope.Application,
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("dt", Json("\"not-a-date\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*ISO 8601*");
    }

    // -----------------------------------------------------------------------
    // Select
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Select_AcceptsAllowedValue()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "s",
                DisplayName = "S",
                Type = SettingType.Select,
                Scope = SettingScope.User,
                AllowedValues = ["compact", "comfortable", "spacious"],
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("s", Json("\"compact\""), SettingScope.User, "u1")
            )
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Select_RejectsValueNotInList()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "s",
                DisplayName = "S",
                Type = SettingType.Select,
                Scope = SettingScope.User,
                AllowedValues = ["compact", "comfortable", "spacious"],
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("s", Json("\"enormous\""), SettingScope.User, "u1")
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*one of*");
    }

    // -----------------------------------------------------------------------
    // Required flag
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Required_RejectsNull()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "r",
                DisplayName = "R",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Required = true,
            }
        );
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("r", Json("null"), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>();
        ex.Which.Errors.Should().ContainMatch("*required*");
    }

    [Fact]
    public async Task Validate_Required_RejectsEmptyString()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "r",
                DisplayName = "R",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Required = true,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("r", Json("\"\""), SettingScope.Application))
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    [Fact]
    public async Task Validate_NotRequired_AllowsNull()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "r",
                DisplayName = "R",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Required = false,
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("r", Json("null"), SettingScope.Application))
            .Should()
            .NotThrowAsync("a non-required setting should accept null without error");
    }

    // -----------------------------------------------------------------------
    // Pattern (regex)
    // -----------------------------------------------------------------------

    [Fact]
    public async Task Validate_Pattern_AcceptsMatchingValue()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "p",
                DisplayName = "P",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Pattern = @"^\d{4}$",
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("p", Json("\"1234\""), SettingScope.Application))
            .Should()
            .NotThrowAsync();
    }

    [Fact]
    public async Task Validate_Pattern_RejectsNonMatchingValue()
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "p",
                DisplayName = "P",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Pattern = @"^\d{4}$",
            }
        );
        await svc.Invoking(s => s.SetSettingAsync("p", Json("\"abc\""), SettingScope.Application))
            .Should()
            .ThrowAsync<SettingValidationException>();
    }

    [Fact]
    public async Task Validate_MalformedPattern_ReturnsInvalidPatternError_NotException()
    {
        // If a definition is misconfigured with an invalid regex the validator must not throw
        // an unhandled exception — it must surface a clean validation error message.
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "p",
                DisplayName = "P",
                Type = SettingType.Text,
                Scope = SettingScope.Application,
                Pattern = "[unclosed",
            }
        );

        // Should throw SettingValidationException (clean error), not ArgumentException/RuntimeException
        var ex = await svc.Invoking(s =>
                s.SetSettingAsync("p", Json("\"some-value\""), SettingScope.Application)
            )
            .Should()
            .ThrowAsync<SettingValidationException>(
                "malformed regex must produce a clean validation error, not a 500"
            );

        ex.Which.Errors.Should().ContainMatch("*invalid pattern*");
    }

    // -----------------------------------------------------------------------
    // Text/MultilineText/Password/Json — permissive type checking
    // -----------------------------------------------------------------------

    [Theory]
    [InlineData(SettingType.Text)]
    [InlineData(SettingType.MultilineText)]
    [InlineData(SettingType.Password)]
    [InlineData(SettingType.Json)]
    public async Task Validate_StringTypes_AcceptAnyNonEmptyString(SettingType type)
    {
        var svc = BuildService(
            new SettingDefinition
            {
                Key = "x",
                DisplayName = "X",
                Type = type,
                Scope = SettingScope.Application,
            }
        );
        await svc.Invoking(s =>
                s.SetSettingAsync("x", Json("\"any string value\""), SettingScope.Application)
            )
            .Should()
            .NotThrowAsync();
    }

    public void Dispose()
    {
        _cache.Dispose();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
