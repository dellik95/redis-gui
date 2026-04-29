using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for ConnectionPort value object
/// </summary>
public class ConnectionPortTests
{
	[TestCase(1)]
	[TestCase(6379)]
	[TestCase(8080)]
	[TestCase(65535)]
	public void Create_WithValidPort_ShouldSucceed(int port)
	{
		// Act
		var connectionPort = new ConnectionPort(port);

		// Assert
		connectionPort.Value.Should().Be(port);
	}

	[TestCase(-1)]
	[TestCase(-100)]
	public void Create_WithInvalidPort_ShouldThrowException(int port)
	{
		// Act
		Action act = () => new ConnectionPort(port);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Test]
	public void Create_WithPort0_ShouldSucceed()
	{
		// Port 0 is technically valid (means "any available port")
		// Act
		var connectionPort = new ConnectionPort(0);

		// Assert
		connectionPort.Value.Should().Be(0);
	}

	[Test]
	public void DefaultRedisPort_ShouldBe6379()
	{
		// Arrange
		var expectedPort = 6379;

		// Act
		var connectionPort = new ConnectionPort(expectedPort);

		// Assert
		connectionPort.Value.Should().Be(expectedPort);
	}
}
