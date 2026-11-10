using FluentAssertions;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for ValueObject base class
/// </summary>
[TestFixture]
public class ValueObjectTests
{
	[Test]
	public void Equals_SameValues_ShouldReturnTrue()
	{
		// Arrange
		TestValueObject valueObject1 = new("value1", 42);
		TestValueObject valueObject2 = new("value1", 42);

		// Act & Assert
		valueObject1.Should().Be(valueObject2);
		valueObject1.GetHashCode().Should().Be(valueObject2.GetHashCode());
		(valueObject1 == valueObject2).Should().BeTrue();
		(valueObject1 != valueObject2).Should().BeFalse();
	}

	[Test]
	public void Equals_DifferentValues_ShouldReturnFalse()
	{
		// Arrange
		TestValueObject valueObject1 = new("value1", 42);
		TestValueObject valueObject2 = new("value2", 42);

		// Act & Assert
		valueObject1.Should().NotBe(valueObject2);
		(valueObject1 == valueObject2).Should().BeFalse();
		(valueObject1 != valueObject2).Should().BeTrue();
	}

	[Test]
	public void Equals_Null_ShouldReturnFalse()
	{
		// Arrange
		TestValueObject? valueObject = new("value", 42);

		// Act & Assert
		valueObject.Equals(null).Should().BeFalse();
		(valueObject == null).Should().BeFalse();
		(valueObject != null).Should().BeTrue();
	}

	[Test]
	public void Equals_DifferentType_ShouldReturnFalse()
	{
		// Arrange
		TestValueObject valueObject1 = new("value", 42);
		OtherTestValueObject valueObject2 = new("value", 42);

		// Act & Assert
		valueObject1.Should().NotBe(valueObject2);
	}

	[Test]
	public void GetHashCode_SameValues_ShouldReturnSameHash()
	{
		// Arrange
		TestValueObject valueObject1 = new("value", 42);
		TestValueObject valueObject2 = new("value", 42);

		// Act & Assert
		valueObject1.GetHashCode().Should().Be(valueObject2.GetHashCode());
	}

	private class TestValueObject : ValueObject
	{
		public string Value1 { get; }
		public int Value2 { get; }

		public TestValueObject(string value1, int value2)
		{
			Value1 = value1;
			Value2 = value2;
		}

		protected override IEnumerable<object?> GetAtomicValues()
		{
			yield return Value1;
			yield return Value2;
		}
	}

	private class OtherTestValueObject : ValueObject
	{
		public string Value1 { get; }
		public int Value2 { get; }

		public OtherTestValueObject(string value1, int value2)
		{
			Value1 = value1;
			Value2 = value2;
		}

		protected override IEnumerable<object?> GetAtomicValues()
		{
			yield return Value1;
			yield return Value2;
		}
	}
}

