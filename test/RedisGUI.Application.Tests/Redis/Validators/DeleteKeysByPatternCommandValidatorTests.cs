using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.DeleteKeysByPattern;
using System;

namespace RedisGUI.Application.Tests.Redis.Validators;

[TestFixture]
public class DeleteKeysByPatternCommandValidatorTests
{
    private DeleteKeysByPatternCommandValidator validator = null!;

    [SetUp]
    public void Setup()
    {
        validator = new DeleteKeysByPatternCommandValidator();
    }

    [Test]
    public void Validate_ValidCommand_ReturnsSuccess()
    {
        // Arrange
        var command = new DeleteKeysByPatternCommand(Guid.NewGuid(), "user:*");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeTrue();
    }

    [Test]
    public void Validate_EmptyConnectionId_ReturnsFailure()
    {
        // Arrange
        var command = new DeleteKeysByPatternCommand(Guid.Empty, "user:*");

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "ConnectionId");
    }

    [TestCase("")]
    [TestCase(" ")]
    [TestCase(null)]
    public void Validate_InvalidPattern_ReturnsFailure(string? pattern)
    {
        // Arrange
        var command = new DeleteKeysByPatternCommand(Guid.NewGuid(), pattern!);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Pattern");
    }

    [Test]
    public void Validate_PatternTooLong_ReturnsFailure()
    {
        // Arrange
        var longPattern = new string('*', 257);
        var command = new DeleteKeysByPatternCommand(Guid.NewGuid(), longPattern);

        // Act
        var result = validator.Validate(command);

        // Assert
        result.IsValid.Should().BeFalse();
        result.Errors.Should().ContainSingle(e => e.PropertyName == "Pattern");
    }
}



