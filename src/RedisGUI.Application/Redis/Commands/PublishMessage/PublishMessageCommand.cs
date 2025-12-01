using System;
using RedisGUI.Shared.Mediator;

namespace RedisGUI.Application.Redis.Commands.PublishMessage;

/// <summary>
/// Command to publish a message to a Redis channel.
/// </summary>
/// <param name="ConnectionId">The Redis connection identifier.</param>
/// <param name="Channel">The channel name to publish to.</param>
/// <param name="Message">The message to publish.</param>
public sealed record PublishMessageCommand(
	Guid ConnectionId,
	string Channel,
	string Message) : ICommand;
