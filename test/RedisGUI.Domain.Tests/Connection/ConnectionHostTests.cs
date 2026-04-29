using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for ConnectionHost value object
/// </summary>
public class ConnectionHostTests
{
	[TestCase("localhost")]
	[TestCase("127.0.0.1")]
	[TestCase("redis.example.com")]
	[TestCase("192.168.1.100")]
	[TestCase("my-redis.example.com")]
	public void Create_WithValidHost_ShouldSucceed(string host)
	{
		// Act
		var connectionHost = new ConnectionHost(host);

		// Assert
		connectionHost.Value.Should().Be(host);
	}

	[Test]
	public void Create_WithEmptyHost_ShouldThrowException()
	{
		// Arrange
		var host = string.Empty;

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Test]
	public void Create_WithNullHost_ShouldThrowException()
	{
		// Arrange
		string host = null!;

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Test]
	public void Create_WithHostTooLong_ShouldThrowException()
	{
		// Arrange
		var host = new string('a', ConnectionHost.MaxLength + 1);

		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}

	[TestCase("invalid host with spaces")]
	[TestCase("http://invalid")]
	public void Create_WithInvalidFormat_ShouldThrowException(string host)
	{
		// Act
		Action act = () => new ConnectionHost(host);

		// Assert
		act.Should().Throw<Exception>();
	}
}
