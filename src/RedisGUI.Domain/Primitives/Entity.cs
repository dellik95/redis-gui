using RedisGUI.Domain.Abstraction;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Domain.Primitives;

/// <summary>
/// Base class for all domain entities
/// </summary>
public abstract class Entity : IEquatable<Entity>
{
	/// <summary>
	/// Gets the unique identifier for this entity
	/// </summary>
	public Guid Id { get; init; }

	/// <summary>
	/// Gets the collection of domain events for this entity
	/// </summary>
	private readonly List<IDomainEvent> domainEvents = new();

	/// <summary>
	/// Initializes a new instance of the Entity class
	/// </summary>
	/// <param name="id">The unique identifier for this entity</param>
	protected Entity(Guid id)
	{
		this.Id = id;
	}

	/// <summary>
	/// Initializes a new instance of the Entity class
	/// </summary>
	protected Entity()
	{

	}

	/// <summary>
	/// Gets the domain events associated with this entity
	/// </summary>
	/// <returns>Collection of domain events</returns>
	public IReadOnlyCollection<IDomainEvent> GetDomainEvents() => domainEvents.ToList();

	/// <summary>
	/// Clears all domain events associated with this entity
	/// </summary>
	public void ClearDomainEvents() => domainEvents.Clear();

	/// <summary>
	/// Raises a new domain event
	/// </summary>
	/// <param name="domainEvent">The domain event to raise</param>
	protected void RaiseDomainEvent(IDomainEvent domainEvent)
	{
		domainEvents.Add(domainEvent);
	}

	/// <summary>
	/// Checks if two entities are equal
	/// </summary>
	/// <param name="a">First entity</param>
	/// <param name="b">Second entity</param>
	/// <returns>True if entities are equal, false otherwise</returns>
	public static bool operator ==(Entity a, Entity b)
	{
		if (a is null && b is null)
		{
			return true;
		}

		if (a is null || b is null)
		{
			return false;
		}

		return a.Equals(b);
	}

	/// <summary>
	/// Checks if two entities are not equal
	/// </summary>
	/// <param name="a">First entity</param>
	/// <param name="b">Second entity</param>
	/// <returns>True if entities are not equal, false otherwise</returns>
	public static bool operator !=(Entity a, Entity b)
	{
		return !(a == b);
	}

	/// <inheritdoc />
	public bool Equals(Entity other)
	{
		if (other is null)
		{
			return false;
		}

		return ReferenceEquals(this, other) || Id == other.Id;
	}

	/// <inheritdoc />
	public override bool Equals(object obj)
	{
		if (obj is null)
		{
			return false;
		}

		if (ReferenceEquals(this, obj))
		{
			return true;
		}

		if (obj.GetType() != GetType())
		{
			return false;
		}

		if (obj is not Entity other)
		{
			return false;
		}

		if (Id == Guid.Empty || other.Id == Guid.Empty)
		{
			return false;
		}

		return Id == other.Id;
	}

	/// <summary>
	/// Returns a hash code for this entity
	/// </summary>
	/// <returns>A hash code value generated from the entity's Id and domain events</returns>
	public override int GetHashCode()
	{
		return HashCode.Combine(Id, domainEvents);
	}
}
