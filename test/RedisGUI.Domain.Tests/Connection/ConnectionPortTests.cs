using RedisGUI.Domain.Connection;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for ConnectionPort value object
/// </summary>
public class ConnectionPortTests
{
	[Theory]
	[InlineData(1)]
	[InlineData(6379)]
	[InlineData(8080)]
	[InlineData(65535)]
	public void Create_WithValidPort_ShouldSucceed(int port)
	{
		// Act
		var connectionPort = new ConnectionPort(port);

		// Assert
		connectionPort.Value.Should().Be(port);
	}

	[Theory]
	[InlineData(-1)]
	[InlineData(-100)]
	public void Create_WithInvalidPort_ShouldThrowException(int port)
	{
		// Act
		Action act = () => new ConnectionPort(port);

		// Assert
		act.Should().Throw<Exception>();
	}

	[Fact]
	public void Create_WithPort0_ShouldSucceed()
	{
		// Port 0 is technically valid (means "any available port")
		// Act
		var connectionPort = new ConnectionPort(0);

		// Assert
		connectionPort.Value.Should().Be(0);
	}

	[Fact]
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
