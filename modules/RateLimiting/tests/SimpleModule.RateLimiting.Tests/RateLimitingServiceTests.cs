using FluentAssertions;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;
using SimpleModule.Core.RateLimiting;
using SimpleModule.Database;
using SimpleModule.RateLimiting.Contracts;

namespace SimpleModule.RateLimiting.Tests;

public sealed class RateLimitingServiceTests : IDisposable
{
    private readonly RateLimitingDbContext _db;
    private readonly RateLimitingService _service;

    public RateLimitingServiceTests()
    {
        var dbOptions = new DbContextOptionsBuilder<RateLimitingDbContext>()
            .UseSqlite("Data Source=:memory:")
            .Options;
        var databaseOptions = Options.Create(
            new DatabaseOptions
            {
                ModuleConnections = new Dictionary<string, string>
                {
                    ["RateLimiting"] = "Data Source=:memory:",
                },
            }
        );
        _db = new RateLimitingDbContext(dbOptions, databaseOptions);
        _db.Database.OpenConnection();
        _db.Database.EnsureCreated();
        _service = new RateLimitingService(_db, NullLogger<RateLimitingService>.Instance);
    }

    [Fact]
    public async Task CreateRuleAsync_ShouldPersistRule()
    {
        var request = new CreateRateLimitRuleRequest
        {
            PolicyName = "test-fixed",
            PolicyType = RateLimitPolicyType.FixedWindow,
            Target = RateLimitTarget.Ip,
            PermitLimit = 100,
            WindowSeconds = 60,
        };

        var result = await _service.CreateRuleAsync(request);

        result.PolicyName.Should().Be("test-fixed");
        result.PermitLimit.Should().Be(100);
        result.Id.Value.Should().BeGreaterThan(0);
    }

    [Fact]
    public async Task GetAllRulesAsync_ShouldReturnAllRules()
    {
        await _service.CreateRuleAsync(new CreateRateLimitRuleRequest { PolicyName = "rule-1" });
        await _service.CreateRuleAsync(new CreateRateLimitRuleRequest { PolicyName = "rule-2" });

        var rules = await _service.GetAllRulesAsync();

        rules.Should().HaveCount(2);
    }

    [Fact]
    public async Task GetRuleByIdAsync_ShouldReturnRule_WhenExists()
    {
        var created = await _service.CreateRuleAsync(
            new CreateRateLimitRuleRequest { PolicyName = "find-me" }
        );

        var found = await _service.GetRuleByIdAsync(created.Id);

        found.Should().NotBeNull();
        found!.PolicyName.Should().Be("find-me");
    }

    [Fact]
    public async Task GetRuleByIdAsync_ShouldReturnNull_WhenNotFound()
    {
        var found = await _service.GetRuleByIdAsync(RateLimitRuleId.From(999));

        found.Should().BeNull();
    }

    [Fact]
    public async Task UpdateRuleAsync_ShouldModifyRule()
    {
        var created = await _service.CreateRuleAsync(
            new CreateRateLimitRuleRequest { PolicyName = "update-me", PermitLimit = 60 }
        );

        var updated = await _service.UpdateRuleAsync(
            created.Id,
            new UpdateRateLimitRuleRequest
            {
                PermitLimit = 200,
                PolicyType = RateLimitPolicyType.SlidingWindow,
                Target = RateLimitTarget.User,
            }
        );

        updated.PermitLimit.Should().Be(200);
        updated.PolicyType.Should().Be(RateLimitPolicyType.SlidingWindow);
        updated.Target.Should().Be(RateLimitTarget.User);
        updated.UpdatedAt.Should().NotBeNull();
    }

    [Fact]
    public async Task DeleteRuleAsync_ShouldRemoveRule()
    {
        var created = await _service.CreateRuleAsync(
            new CreateRateLimitRuleRequest { PolicyName = "delete-me" }
        );

        await _service.DeleteRuleAsync(created.Id);

        var found = await _service.GetRuleByIdAsync(created.Id);
        found.Should().BeNull();
    }

    [Fact]
    public async Task DeleteRuleAsync_ShouldThrow_WhenNotFound()
    {
        var act = () => _service.DeleteRuleAsync(RateLimitRuleId.From(999));

        await act.Should().ThrowAsync<Core.Exceptions.NotFoundException>();
    }

    [Fact]
    public async Task UpdateRuleAsync_ShouldThrow_WhenNotFound()
    {
        var act = () =>
            _service.UpdateRuleAsync(RateLimitRuleId.From(999), new UpdateRateLimitRuleRequest());

        await act.Should().ThrowAsync<Core.Exceptions.NotFoundException>();
    }

    public void Dispose()
    {
        _db.Database.CloseConnection();
        _db.Dispose();
        GC.SuppressFinalize(this);
    }
}
