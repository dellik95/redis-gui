using FluentAssertions;
using Moq;
using NUnit.Framework;
using RedisGUI.Application.Redis.Commands.ExecuteLuaScript;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Redis.Commands;

[TestFixture]
public class ExecuteLuaScriptCommandHandlerTests
{
	private Mock<IRedisConnectionRepository> connectionRepositoryMock = null!;
	private Mock<IRedisScriptService> connectionServiceMock = null!;
	private ExecuteLuaScriptCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		connectionRepositoryMock = new Mock<IRedisConnectionRepository>();
		connectionServiceMock = new Mock<IRedisScriptService>();
		handler = new ExecuteLuaScriptCommandHandler(
			connectionRepositoryMock.Object,
			connectionServiceMock.Object);
	}

	[Test]
	public async Task Handle_SimpleScript_ExecutesSuccessfully()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var script = "return redis.call('GET', KEYS[1])";
		var keys = new List<string> { "mykey" }.AsReadOnly();
		var arguments = new List<string>().AsReadOnly();
		var command = new ExecuteLuaScriptCommand(connectionId, script, keys.ToArray(), arguments.ToArray());

		var connection = RedisConnection.Create(
				new ConnectionName("Test Connection"),
				new ConnectionHost("localhost"),
				new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.ExecuteLuaScriptAsync(connection, script, It.IsAny<string[]>(), It.IsAny<string[]>()))
			.ReturnsAsync(Result.Success(("myvalue", "sha1hash")));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Result.Should().Be("myvalue");
		result.Value.Success.Should().BeTrue();
	}

	[Test]
	public async Task Handle_ScriptWithArguments_ExecutesSuccessfully()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var script = "return redis.call('SET', KEYS[1], ARGV[1])";
		var keys = new List<string> { "mykey" }.AsReadOnly();
		var arguments = new List<string> { "newvalue" }.AsReadOnly();
		var command = new ExecuteLuaScriptCommand(connectionId, script, keys.ToArray(), arguments.ToArray());

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		connectionServiceMock
			.Setup(x => x.ExecuteLuaScriptAsync(connection, script, It.IsAny<string[]>(), It.IsAny<string[]>()))
			.ReturnsAsync(Result.Success(("OK", "sha1hash")));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Result.Should().Be("OK");
	}

	[Test]
	public async Task Handle_ScriptError_ReturnsFailure()
	{
		// Arrange
		var connectionId = Guid.NewGuid();
		var script = "invalid lua script";
		var command = new ExecuteLuaScriptCommand(
			connectionId,
			script,
			Array.Empty<string>(),
			Array.Empty<string>());

		var connection = RedisConnection.Create(
			new ConnectionName("Test Connection"),
			new ConnectionHost("localhost"),
			new ConnectionPort(6379),
			new DatabaseNumber(0),
			null).Value;

		connectionRepositoryMock
			.Setup(x => x.GetConnectionByIdAsync(connectionId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(connection));

		var error = new Error("Redis.ScriptError", "Syntax error in script");
		connectionServiceMock
			.Setup(x => x.ExecuteLuaScriptAsync(connection, script, It.IsAny<string[]>(), It.IsAny<string[]>()))
			.ReturnsAsync(Result.Failure<(string, string)>(error));

		// Act
		var result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(error);
	}
}




