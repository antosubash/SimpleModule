using FluentAssertions;
using SimpleModule.Core.Exceptions;

namespace SimpleModule.Core.Tests;

public class ValidationExceptionTests
{
    [Fact]
    public void DefaultConstructor_HasDefaultMessageAndEmptyErrors()
    {
        var ex = new ValidationException();

        ex.Message.Should().Be("One or more validation errors occurred.");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void MessageConstructor_SetsMessageAndEmptyErrors()
    {
        var ex = new ValidationException("Custom message");

        ex.Message.Should().Be("Custom message");
        ex.Errors.Should().BeEmpty();
    }

    [Fact]
    public void ErrorsConstructor_SetsDefaultMessageAndErrors()
    {
        var errors = new Dictionary<string, string[]>
        {
            ["Name"] = ["Name is required"],
            ["Email"] = ["Email is invalid", "Email already exists"],
        };

        var ex = new ValidationException(errors);

        ex.Message.Should().Be("One or more validation errors occurred.");
        ex.Errors.Should().HaveCount(2);
        ex.Errors["Name"].Should().ContainSingle("Name is required");
        ex.Errors["Email"].Should().HaveCount(2);
    }

    [Fact]
    public void InnerExceptionConstructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("inner");
        var ex = new ValidationException("outer", inner);

        ex.Message.Should().Be("outer");
        ex.InnerException.Should().BeSameAs(inner);
        ex.Errors.Should().BeEmpty();
    }
}

public class NotFoundExceptionTests
{
    [Fact]
    public void DefaultConstructor_HasDefaultMessage()
    {
        var ex = new NotFoundException();

        ex.Message.Should().Be("The requested resource was not found.");
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new NotFoundException("User not found");

        ex.Message.Should().Be("User not found");
    }

    [Fact]
    public void EntityAndIdConstructor_FormatsMessage()
    {
        var ex = new NotFoundException("Product", 42);

        ex.Message.Should().Be("Product with ID 42 not found");
    }

    [Fact]
    public void EntityAndStringIdConstructor_FormatsMessage()
    {
        var ex = new NotFoundException("User", "abc-123");

        ex.Message.Should().Be("User with ID abc-123 not found");
    }

    [Fact]
    public void InnerExceptionConstructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("db error");
        var ex = new NotFoundException("not found", inner);

        ex.Message.Should().Be("not found");
        ex.InnerException.Should().BeSameAs(inner);
    }
}

public class ConflictExceptionTests
{
    [Fact]
    public void DefaultConstructor_HasDefaultMessage()
    {
        var ex = new ConflictException();

        ex.Message.Should().Be("A conflict occurred.");
    }

    [Fact]
    public void MessageConstructor_SetsMessage()
    {
        var ex = new ConflictException("Duplicate entry");

        ex.Message.Should().Be("Duplicate entry");
    }

    [Fact]
    public void InnerExceptionConstructor_SetsMessageAndInnerException()
    {
        var inner = new InvalidOperationException("db constraint");
        var ex = new ConflictException("conflict", inner);

        ex.Message.Should().Be("conflict");
        ex.InnerException.Should().BeSameAs(inner);
    }
}
