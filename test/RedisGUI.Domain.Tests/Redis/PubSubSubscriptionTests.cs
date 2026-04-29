using RedisGUI.Domain.Redis;
using System;

namespace RedisGUI.Domain.Tests.Redis;

/// <summary>
/// Tests for PubSubSubscription domain entity
/// </summary>
[TestFixture]
public class PubSubSubscriptionTests
{
	[Test]
	public void Create_ValidSubscription_ShouldCreateInstance()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string channel = "test:channel";

		// Act
		PubSubSubscription subscription = PubSubSubscription.Create(connectionId, channel);

		// Assert
		subscription.Should().NotBeNull();
		subscription.ConnectionId.Should().Be(connectionId);
		subscription.Channel.Should().Be(channel);
		subscription.SubscriptionId.Should().NotBeEmpty();
		subscription.MessagesReceived.Should().Be(0);
		subscription.CreatedAt.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
	}

	[Test]
	public void Create_ShouldGenerateUniqueSubscriptionId()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		string channel = "test:channel";

		// Act
		PubSubSubscription subscription1 = PubSubSubscription.Create(connectionId, channel);
		PubSubSubscription subscription2 = PubSubSubscription.Create(connectionId, channel);

		// Assert
		subscription1.SubscriptionId.Should().NotBe(subscription2.SubscriptionId);
	}

	[Test]
	public void MessagesReceived_ShouldBeMutable()
	{
		// Arrange
		PubSubSubscription subscription = PubSubSubscription.Create(Guid.NewGuid(), "test:channel");

		// Act
		subscription.MessagesReceived = 5;

		// Assert
		subscription.MessagesReceived.Should().Be(5);
	}
}

