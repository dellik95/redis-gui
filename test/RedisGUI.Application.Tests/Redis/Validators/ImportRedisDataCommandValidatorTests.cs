using FluentAssertions;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ImportRedisData;
using RedisGUI.Domain.Redis;
using System;

namespace RedisGUI.Application.Tests.Redis.Validators;

[TestFixture]
public class ImportRedisDataCommandValidatorTests
{
	private ImportRedisDataCommandValidator validator = null!;

	[SetUp]
	public void Setup()
	{
		validator = new ImportRedisDataCommandValidator();
	}

	[Test]
	public void Validate_ValidCommand_ReturnsSuccess()
	{
		// Arrange
		var command = new ImportRedisDataCommand(
			Guid.NewGuid(),
			"[{\"Key\":\"test\",\"Value\":\"data\"}]",
			ImportFormat.Json,
			true);

		// Act
		var result = validator.Validate(command);

		// Assert
		result.IsValid.Should().BeTrue();
	}

	[TestCase("")]
	[TestCase(" ")]
	[TestCase(null)]
	public void Validate_EmptyData_ReturnsFailure(string? data)
	{
		// Arrange
		var command = new ImportRedisDataCommand(Guid.NewGuid(), data!, ImportFormat.Json, true);

		// Act
		var result = validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Data");
	}

	[Test]
	public void Validate_DataTooLarge_ReturnsFailure()
	{
		// Arrange
		var largeData = new string('x', 10 * 1024 * 1024 + 1); // 10MB + 1 byte
		var command = new ImportRedisDataCommand(Guid.NewGuid(), largeData, ImportFormat.Json, true);

		// Act
		var result = validator.Validate(command);

		// Assert
		result.IsValid.Should().BeFalse();
		result.Errors.Should().ContainSingle(e => e.PropertyName == "Data");
	}
}
