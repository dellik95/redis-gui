using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for ConnectionName value object
/// </summary>
public class ConnectionNameTests
{
	[Fact]
	public void Create_WithValidName_ShouldSucceed()
	{
		// Arrange
		var name = "MyRedisConnection";

		// Act
		var connectionName = new ConnectionName(name);

		// Assert
		connectionName.Value.Should().Be(name);
	}

	[Fact]
	public void Create_WithEmptyName_ShouldThrowException()
	{
		// Arrange
		var name = string.Empty;

		// Act
		Action act = () => new ConnectionName(name);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Create_WithNullName_ShouldThrowException()
	{
		// Arrange
		string name = null!;

		// Act
		Action act = () => new ConnectionName(name);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Create_WithNameTooLong_ShouldThrowException()
	{
		// Arrange
		var name = new string('a', ConnectionName.MaxLength + 1);

		// Act
		Action act = () => new ConnectionName(name);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Theory]
	[InlineData("Connection1")]
	[InlineData("My Redis Connection")]
	[InlineData("connection-123")]
	[InlineData("Connection_With_Underscores")]
	public void Create_WithVariousValidNames_ShouldSucceed(string name)
	{
		// Act
		var connectionName = new ConnectionName(name);

		// Assert
		connectionName.Value.Should().Be(name);
	}
}
