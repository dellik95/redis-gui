using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System;

namespace RedisGUI.Domain.Tests.Redis;

/// <summary>
/// Tests for RedisValue domain entity
/// </summary>
[TestFixture]
public class RedisValueTests
{
	[Test]
	public void Create_ValidValue_ShouldCreateInstance()
	{
		// Arrange & Act
		Result<RedisValue> result = RedisValue.Create("key", "value", "String", null);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Key.Should().Be("key");
		result.Value.Value.Should().Be("value");
		result.Value.Type.Should().Be("String");
		result.Value.TimeToLive.Should().BeNull();
	}

	[Test]
	public void Create_WithTimeToLive_ShouldCreateInstance()
	{
		// Arrange
		TimeSpan ttl = TimeSpan.FromMinutes(5);

		// Act
		Result<RedisValue> result = RedisValue.Create("key", "value", "String", ttl);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.TimeToLive.Should().Be(ttl);
	}

	[Test]
	public void Create_EmptyKey_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<RedisValue> result = RedisValue.Create("", "value", "String", null);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.RedisValue.EmptyKey);
	}

	[Test]
	public void Create_WhitespaceKey_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<RedisValue> result = RedisValue.Create("   ", "value", "String", null);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.RedisValue.EmptyKey);
	}

	[Test]
	public void Create_EmptyType_ShouldReturnFailure()
	{
		// Arrange & Act
		Result<RedisValue> result = RedisValue.Create("key", "value", "", null);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.RedisValue.EmptyType);
	}

	[Test]
	public void Create_NullValue_ShouldUseEmptyString()
	{
		// Arrange & Act
		Result<RedisValue> result = RedisValue.Create("key", null!, "String", null);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Value.Should().Be(string.Empty);
	}
}

