using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ExecuteTransaction;
using RedisGUI.Domain.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Application.Tests.Redis.Validators;

[TestFixture]
public class ExecuteTransactionCommandValidatorTests
{
    private ExecuteTransactionCommandValidator validator = null!;

    [SetUp]
    public void Setup()
    {
        validator = new ExecuteTransactionCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var operations = new List<RedisOperation>
        {
            new RedisOperation("SET", "key1", "value1")
        };
        var command = new ExecuteTransactionCommand(Guid.NewGuid(), operations);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyOperations_ReturnsFailure()
    {
        // Arrange
        var operations = new List<RedisOperation>();
        var command = new ExecuteTransactionCommand(Guid.NewGuid(), operations);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Operations");
    }

    [Test]
    public void Validate_TooManyOperations_ReturnsFailure()
    {
        // Arrange
        var operations = Enumerable.Range(0, 101)
            .Select(i => new RedisOperation("SET", $"key{i}", "value"))
            .ToList();
        var command = new ExecuteTransactionCommand(Guid.NewGuid(), operations);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Operations");
    }
}



