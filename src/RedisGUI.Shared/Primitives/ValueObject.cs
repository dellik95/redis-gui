namespace RedisGUI.Shared.Primitives;

/// <summary>
/// Base class for value objects in domain-driven design
/// </summary>
public abstract class ValueObject : IEquatable<ValueObject>
{
	/// <summary>
	/// Gets the atomic values that make up this value object
	/// </summary>
	/// <returns>Collection of atomic values</returns>
	protected abstract IEnumerable<object?> GetAtomicValues();

	/// <summary>
	/// Checks if two value objects are equal
	/// </summary>
	/// <param name="obj">The value object to compare with</param>
	/// <returns>True if equal; otherwise, false</returns>
	public override bool Equals(object? obj)
	{
		if (obj == null || obj.GetType() != GetType())
		{
			return false;
		}

		ValueObject other = (ValueObject)obj;
		return GetAtomicValues().SequenceEqual(other.GetAtomicValues());
	}

	/// <summary>
	/// Gets the hash code for this value object
	/// </summary>
	/// <returns>Hash code</returns>
	public override int GetHashCode()
	{
		return GetAtomicValues()
			.Select(x => x != null ? x.GetHashCode() : 0)
			.Aggregate((x, y) => x ^ y);
	}

	/// <summary>
	/// Checks if two value objects are equal
	/// </summary>
	/// <param name="other">The value object to compare with</param>
	/// <returns>True if equal; otherwise, false</returns>
	public bool Equals(ValueObject? other)
	{
		return Equals((object?)other);
	}

	/// <summary>
	/// Equality operator
	/// </summary>
	public static bool operator ==(ValueObject? a, ValueObject? b)
	{
		if (ReferenceEquals(a, null) && ReferenceEquals(b, null))
			return true;

		if (ReferenceEquals(a, null) || ReferenceEquals(b, null))
			return false;

		return a.Equals(b);
	}

	/// <summary>
	/// Inequality operator
	/// </summary>
	public static bool operator !=(ValueObject? a, ValueObject? b)
	{
		return !(a == b);
	}
}
