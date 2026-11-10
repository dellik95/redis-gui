using Moq;
using RedisGUI.Domain.Abstraction.Cryptography;
using RedisGUI.Domain.Connection;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Unit tests for RedisConnection entity
/// </summary>
[TestFixture]
public class RedisConnectionTests
{
	private readonly Mock<IPasswordEncryptor> mockPasswordEncryptor;

	public RedisConnectionTests()
	{
		mockPasswordEncryptor = new Mock<IPasswordEncryptor>();
		mockPasswordEncryptor
			.Setup(x => x.EncryptPassword(It.IsAny<string>()))
			.Returns((string password) => $"encrypted_{password}");
	}

	[Test]
	public void Create_WithValidParametersAndNoCredentials_ShouldReturnAnonymousConnection()
	{
		// Arrange
		ConnectionName name = new("TestConnection");
		ConnectionHost host = new("localhost");
		ConnectionPort port = new(6379);
		DatabaseNumber database = new(0);

		// Act
		Result<RedisConnection> result = RedisConnection.Create(name, host, port, database, ConnectionOwnerId.None());

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeOfType<AnonymousRedisConnection>();
		result.Value.ConnectionName.Should().Be(name);
		result.Value.ServerHost.Should().Be(host);
		result.Value.ServerPort.Should().Be(port);
		result.Value.DatabaseNumber.Should().Be(database);
	}

	[Test]
	public void Create_WithValidParametersAndCredentials_ShouldReturnConnectionWithCredentials()
	{
		// Arrange
		ConnectionName name = new("TestConnection");
		ConnectionHost host = new("localhost");
		ConnectionPort port = new(6379);
		DatabaseNumber database = new(0);
		ConnectionCredentials? credentials = ConnectionCredentials.Create("user", "password", mockPasswordEncryptor.Object).Value;

		// Act
		Result<RedisConnection> result = RedisConnection.Create(name, host, port, database, ConnectionOwnerId.None(), credentials);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().BeOfType<RedisConnectionWithCredentials>();
		result.Value.ConnectionName.Should().Be(name);
		result.Value.ServerHost.Should().Be(host);
		result.Value.ServerPort.Should().Be(port);
		result.Value.DatabaseNumber.Should().Be(database);
		((RedisConnectionWithCredentials)result.Value).ConnectionCredentials.Should().Be(credentials);
	}

	[Test]
	public void UpdateConnectionDetails_WithValidParameters_ShouldUpdateProperties()
	{
		// Arrange
		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Original"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value;

		ConnectionName newName = new("Updated");
		ConnectionHost newHost = new("redis.example.com");
		ConnectionPort newPort = new(6380);
		DatabaseNumber newDatabase = new(1);

		// Act
		Result result = connection.UpdateConnectionDetails(newName, newHost, newPort, newDatabase);

		// Assert
		result.IsSuccess.Should().BeTrue();
		connection.ConnectionName.Should().Be(newName);
		connection.ServerHost.Should().Be(newHost);
		connection.ServerPort.Should().Be(newPort);
		connection.DatabaseNumber.Should().Be(newDatabase);
	}

	[Test]
	public void UpdateAvailability_ShouldChangeIsAvailableProperty()
	{
		// Arrange
		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value;

		// Act
		connection.UpdateAvailability(true);

		// Assert
		connection.IsAvailable.Should().BeTrue();

		// Act
		connection.UpdateAvailability(false);

		// Assert
		connection.IsAvailable.Should().BeFalse();
	}

	[Test]
	public void BuildConnectionString_ForAnonymousConnection_ShouldReturnCorrectFormat()
	{
		// Arrange
		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None()).Value;

		// Act
		string connectionString = connection.BuildConnectionString();

		// Assert
		connectionString.Should().Contain("localhost:6379");
		connectionString.Should().Contain("allowAdmin=True");
	}

	[Test]
	public void BuildConnectionString_ForConnectionWithCredentials_ShouldIncludeCredentials()
	{
		// Arrange
		ConnectionCredentials? credentials = ConnectionCredentials.Create("user", "password", mockPasswordEncryptor.Object).Value;
		RedisConnection connection = RedisConnection.Create(
			new ConnectionName("Test"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			ConnectionOwnerId.None(),
			credentials).Value;

		// Act
		string connectionString = connection.BuildConnectionString();

		// Assert
		connectionString.Should().Contain("user");
		connectionString.Should().Contain("encrypted_password");
	}
}
