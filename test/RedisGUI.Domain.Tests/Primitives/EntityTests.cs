using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Primitives;
using System;
using System.Collections.Generic;

namespace RedisGUI.Domain.Tests.Primitives;

/// <summary>
/// Tests for Entity base class
/// </summary>
[TestFixture]
public class EntityTests
{
	[Test]
	public void Constructor_WithId_ShouldSetId()
	{
		// Arrange
		Guid id = Guid.NewGuid();

		// Act
		TestEntity entity = new(id);

		// Assert
		entity.Id.Should().Be(id);
	}

	[Test]
	public void RaiseDomainEvent_ShouldAddEvent()
	{
		// Arrange
		TestEntity entity = new(Guid.NewGuid());
		TestDomainEvent domainEvent = new();

		// Act
		entity.RaiseTestEvent(domainEvent);

		// Assert
		IReadOnlyCollection<IDomainEvent> events = entity.GetDomainEvents();
		events.Should().HaveCount(1);
		events.Should().Contain(domainEvent);
	}

	[Test]
	public void ClearDomainEvents_ShouldRemoveAllEvents()
	{
		// Arrange
		TestEntity entity = new(Guid.NewGuid());
		entity.RaiseTestEvent(new TestDomainEvent());
		entity.RaiseTestEvent(new TestDomainEvent());

		// Act
		entity.ClearDomainEvents();

		// Assert
		entity.GetDomainEvents().Should().BeEmpty();
	}

	[Test]
	public void Equals_SameId_ShouldReturnTrue()
	{
		// Arrange
		Guid id = Guid.NewGuid();
		TestEntity entity1 = new(id);
		TestEntity entity2 = new(id);

		// Act & Assert
		entity1.Should().Be(entity2);
		(entity1 == entity2).Should().BeTrue();
		(entity1 != entity2).Should().BeFalse();
	}

	[Test]
	public void Equals_DifferentId_ShouldReturnFalse()
	{
		// Arrange
		TestEntity entity1 = new(Guid.NewGuid());
		TestEntity entity2 = new(Guid.NewGuid());

		// Act & Assert
		entity1.Should().NotBe(entity2);
		(entity1 == entity2).Should().BeFalse();
		(entity1 != entity2).Should().BeTrue();
	}

	[Test]
	public void Equals_EmptyGuid_ShouldReturnFalse()
	{
		// Arrange
		TestEntity entity1 = new(Guid.Empty);
		TestEntity entity2 = new(Guid.Empty);

		// Act & Assert
		entity1.Should().NotBe(entity2);
	}

	[Test]
	public void GetHashCode_SameId_ShouldReturnSameHash()
	{
		// Arrange
		Guid id = Guid.NewGuid();
		TestEntity entity1 = new(id);
		TestEntity entity2 = new(id);

		// Act & Assert
		entity1.GetHashCode().Should().Be(entity2.GetHashCode());
	}

	private class TestEntity : Entity
	{
		public TestEntity(Guid id) : base(id) { }
		public TestEntity() { }

		public void RaiseTestEvent(IDomainEvent domainEvent)
		{
			RaiseDomainEvent(domainEvent);
		}
	}

	private class TestDomainEvent : IDomainEvent
	{
		public DateTime OccurredOn => DateTime.UtcNow;
	}
}

