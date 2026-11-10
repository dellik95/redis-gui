using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Tests for ConnectionOwnerId value object
/// </summary>
[TestFixture]
public class ConnectionOwnerIdTests
{
	[Test]
	public void Some_ValidGuid_ShouldCreateInstanceWithValue()
	{
		// Arrange
		Guid userId = Guid.NewGuid();

		// Act
		ConnectionOwnerId ownerId = ConnectionOwnerId.Some(userId);

		// Assert
		ownerId.HasValue.Should().BeTrue();
		ownerId.Value.Should().Be(userId);
	}

	[Test]
	public void None_ShouldCreateInstanceWithoutValue()
	{
		// Act
		ConnectionOwnerId ownerId = ConnectionOwnerId.None();

		// Assert
		ownerId.HasValue.Should().BeFalse();
		ownerId.Value.Should().BeNull();
	}

	[Test]
	public void FromNullable_WithGuid_ShouldCreateSome()
	{
		// Arrange
		Guid userId = Guid.NewGuid();

		// Act
		ConnectionOwnerId ownerId = ConnectionOwnerId.FromNullable(userId);

		// Assert
		ownerId.HasValue.Should().BeTrue();
		ownerId.Value.Should().Be(userId);
	}

	[Test]
	public void FromNullable_WithNull_ShouldCreateNone()
	{
		// Act
		ConnectionOwnerId ownerId = ConnectionOwnerId.FromNullable(null);

		// Assert
		ownerId.HasValue.Should().BeFalse();
		ownerId.Value.Should().BeNull();
	}

	[Test]
	public void Constructor_EmptyGuid_ShouldThrowException()
	{
		// Arrange & Act
		Func<ConnectionOwnerId> act = () => new ConnectionOwnerId(Guid.Empty);

		// Assert
		act.Should().Throw<Domain.Exceptions.ErrorException>()
			.Which.Errors.Should().Contain(DomainErrors.ConnectionOwnerId.Empty);
	}

	[Test]
	public void ToString_WithValue_ShouldReturnGuidString()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		ConnectionOwnerId ownerId = ConnectionOwnerId.Some(userId);

		// Act
		string result = ownerId.ToString();

		// Assert
		result.Should().Be(userId.ToString());
	}

	[Test]
	public void ToString_WithoutValue_ShouldReturnNone()
	{
		// Arrange
		ConnectionOwnerId ownerId = ConnectionOwnerId.None();

		// Act
		string result = ownerId.ToString();

		// Assert
		result.Should().Be("None");
	}

	[Test]
	public void Equals_SameValue_ShouldReturnTrue()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		ConnectionOwnerId ownerId1 = ConnectionOwnerId.Some(userId);
		ConnectionOwnerId ownerId2 = ConnectionOwnerId.Some(userId);

		// Act & Assert
		ownerId1.Should().Be(ownerId2);
		ownerId1.GetHashCode().Should().Be(ownerId2.GetHashCode());
	}

	[Test]
	public void Equals_BothNone_ShouldReturnTrue()
	{
		// Arrange
		ConnectionOwnerId ownerId1 = ConnectionOwnerId.None();
		ConnectionOwnerId ownerId2 = ConnectionOwnerId.None();

		// Act & Assert
		ownerId1.Should().Be(ownerId2);
		ownerId1.GetHashCode().Should().Be(ownerId2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValue_ShouldReturnFalse()
	{
		// Arrange
		ConnectionOwnerId ownerId1 = ConnectionOwnerId.Some(Guid.NewGuid());
		ConnectionOwnerId ownerId2 = ConnectionOwnerId.Some(Guid.NewGuid());

		// Act & Assert
		ownerId1.Should().NotBe(ownerId2);
	}

	[Test]
	public void Equals_SomeVsNone_ShouldReturnFalse()
	{
		// Arrange
		ConnectionOwnerId ownerId1 = ConnectionOwnerId.Some(Guid.NewGuid());
		ConnectionOwnerId ownerId2 = ConnectionOwnerId.None();

		// Act & Assert
		ownerId1.Should().NotBe(ownerId2);
	}
}

