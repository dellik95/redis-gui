using Moq;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for RedisConnection entity
/// </summary>
public class RedisConnectionTests
{
	private readonly Mock<IPasswordEncryptor> _mockPasswordEncryptor;

	public RedisConnectionTests()
	{
		_mockPasswordEncryptor = new Mock<IPasswordEncryptor>();
		_mockPasswordEncryptor
			.Setup(x => x.EncryptPassword(It.IsAny<string>()))
			.Returns((string password) => $"encrypted_{password}");
	}

	[Fact]
	public void Create_WithValidParametersAndNoCredentials_ShouldReturnAnonymousConnection()
	{
		// Arrange
		var name = new ConnectionName("TestConnection");
		var host = new ConnectionHost("localhost");
		var port = new ConnectionPort(6379);
		var database = 0;

		// Act
		var result = RedisConnection.Create(name, host, port, database);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeOfType<AnonymousRedisConnection>();
		result.Value.ConnectionName.Should().Be(name);
		result.Value.ServerHost.Should().Be(host);
		result.Value.ServerPort.Should().Be(port);
		result.Value.DatabaseNumber.Should().Be(database);
	}

	[Fact]
	public void Create_WithValidParametersAndCredentials_ShouldReturnConnectionWithCredentials()
	{
		// Arrange
		var name = new ConnectionName("TestConnection");
		var host = new ConnectionHost("localhost");
		var port = new ConnectionPort(6379);
		var database = 0;
		var credentials = ConnectionCredentials.Create("user", "password", _mockPasswordEncryptor.Object).Value;

		// Act
		var result = RedisConnection.Create(name, host, port, database, credentials);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeOfType<RedisConnectionWithCredentials>();
		result.Value.ConnectionName.Should().Be(name);
		result.Value.ServerHost.Should().Be(host);
		result.Value.ServerPort.Should().Be(port);
		result.Value.DatabaseNumber.Should().Be(database);
		((RedisConnectionWithCredentials)result.Value).ConnectionCredentials.Should().Be(credentials);
	}

	[Fact]
	public void UpdateConnectionDetails_WithValidParameters_ShouldUpdateProperties()
	{
		// Arrange
		var connection = RedisConnection.Create(
			new ConnectionName("Original"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			0).Value;

		var newName = new ConnectionName("Updated");
		var newHost = new ConnectionHost("redis.example.com");
		var newPort = new ConnectionPort(6380);
		var newDatabase = 1;

		// Act
		var result = connection.UpdateConnectionDetails(newName, newHost, newPort, newDatabase);

		// Assert
		result.IsSuccess.Should().BeTrue();
		connection.ConnectionName.Should().Be(newName);
		connection.ServerHost.Should().Be(newHost);
		connection.ServerPort.Should().Be(newPort);
		connection.DatabaseNumber.Should().Be(newDatabase);
	}

	[Fact]
	public void UpdateAvailability_ShouldChangeIsAvailableProperty()
	{
		// Arrange
		var connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			0).Value;

		// Act
		connection.UpdateAvailability(true);

		// Assert
		connection.IsAvailable.Should().BeTrue();

		// Act
		connection.UpdateAvailability(false);

		// Assert
		connection.IsAvailable.Should().BeFalse();
	}

	[Fact]
	public void BuildConnectionString_ForAnonymousConnection_ShouldReturnCorrectFormat()
	{
		// Arrange
		var connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			0).Value;

		// Act
		var connectionString = connection.BuildConnectionString();

		// Assert
		connectionString.Should().Contain("localhost:6379");
		connectionString.Should().Contain("allowAdmin=True");
	}

	[Fact]
	public void BuildConnectionString_ForConnectionWithCredentials_ShouldIncludeCredentials()
	{
		// Arrange
		var credentials = ConnectionCredentials.Create("user", "password", _mockPasswordEncryptor.Object).Value;
		var connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			0,
			credentials).Value;

		// Act
		var connectionString = connection.BuildConnectionString();

		// Assert
		connectionString.Should().Contain("user");
		connectionString.Should().Contain("encrypted_password");
	}
}
