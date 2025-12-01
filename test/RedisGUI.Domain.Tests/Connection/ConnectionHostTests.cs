using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for ConnectionHost value object
/// </summary>
public class ConnectionHostTests
{
	[Theory]
	[InlineData("localhost")]
	[InlineData("127.0.0.1")]
	[InlineData("redis.example.com")]
	[InlineData("192.168.1.100")]
	[InlineData("my-redis.example.com")]
	public void Create_WithValidHost_ShouldSucceed(string host)
	{
		// Act
		var connectionHost = new ConnectionHost(host);

		// Assert
		connectionHost.Value.Should().Be(host);
	}

	[Fact]
	public void Create_WithEmptyHost_ShouldThrowException()
	{
		// Arrange
		var host = string.Empty;

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Create_WithNullHost_ShouldThrowException()
	{
		// Arrange
		string host = null!;

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Create_WithHostTooLong_ShouldThrowException()
	{
		// Arrange
		var host = new string('a', ConnectionHost.MaxLength + 1);

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Theory]
	[InlineData("invalid host with spaces")]
	[InlineData("http://invalid")]
	public void Create_WithInvalidFormat_ShouldThrowException(string host)
	{
		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}
}
