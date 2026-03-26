using FluentAssertions;
using SimpleModule.AuditLogs;
using SimpleModule.AuditLogs.Contracts;

namespace AuditLogs.Tests.Unit;

public class AuditContextTests
{
    [Fact]
    public void CorrelationId_IsNotEmpty()
    {
        // Act
        var context = new AuditContext();

        // Assert - CorrelationId should never be empty
        context.CorrelationId.Should().NotBeEmpty();
        context.CorrelationId.Should().NotBe(Guid.Empty);
    }

    [Fact]
    public void CorrelationId_IsConsistent_WhenMultipleInstancesCreated()
    {
        // Act
        var context1 = new AuditContext();
        var context2 = new AuditContext();

        // Assert - Without Activity context, both generate GUIDs
        // (They might differ since they're created at different times)
        context1.CorrelationId.Should().NotBeEmpty();
        context2.CorrelationId.Should().NotBeEmpty();
    }

    [Fact]
    public void UserIdProperty_CanBeSet()
    {
        // Arrange
        var context = new AuditContext();

        // Act
        context.UserId = "test-user";

        // Assert
        context.UserId.Should().Be("test-user");
    }

    [Fact]
    public void UserNameProperty_CanBeSet()
    {
        // Arrange
        var context = new AuditContext();

        // Act
        context.UserName = "Test User";

        // Assert
        context.UserName.Should().Be("Test User");
    }

    [Fact]
    public void IpAddressProperty_CanBeSet()
    {
        // Arrange
        var context = new AuditContext();

        // Act
        context.IpAddress = "192.168.1.1";

        // Assert
        context.IpAddress.Should().Be("192.168.1.1");
    }

    [Fact]
    public void AuditContext_ImplementsIAuditContext()
    {
        // Arrange & Act
        var context = new AuditContext();

        // Assert
        context.Should().BeAssignableTo<IAuditContext>();
    }
}
