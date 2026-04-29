using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using System;

namespace RedisGUI.Domain.Tests.Connection;

/// <summary>
/// Tests for DatabaseNumber value object
/// </summary>
[TestFixture]
public class DatabaseNumberTests
{
	[Test]
	public void Constructor_ValidDatabaseNumber_ShouldCreateInstance()
	{
		// Arrange & Act
		DatabaseNumber databaseNumber = new(0);

		// Assert
		databaseNumber.Value.Should().Be(0);
	}

	[Test]
	public void Constructor_ValidDatabaseNumber_ShouldCreateInstanceWithMaxValue()
	{
		// Arrange & Act
		DatabaseNumber databaseNumber = new(DatabaseNumber.MaxValue);

		// Assert
		databaseNumber.Value.Should().Be(DatabaseNumber.MaxValue);
	}

	[Test]
	public void Constructor_NegativeDatabaseNumber_ShouldThrowException()
	{
		// Arrange & Act
		Func<DatabaseNumber> act = () => new DatabaseNumber(-1);

		// Assert
		act.Should().Throw<Domain.Exceptions.ErrorException>()
			.Which.Errors.Should().Contain(DomainErrors.DatabaseNumber.LessThanAllowed);
	}

	[Test]
	public void Constructor_DatabaseNumberGreaterThanMax_ShouldThrowException()
	{
		// Arrange & Act
		Func<DatabaseNumber> act = () => new DatabaseNumber(DatabaseNumber.MaxValue + 1);

		// Assert
		act.Should().Throw<Domain.Exceptions.ErrorException>()
			.Which.Errors.Should().Contain(DomainErrors.DatabaseNumber.GreaterThanAllowed);
	}

	[Test]
	public void ToString_ShouldReturnStringValue()
	{
		// Arrange
		DatabaseNumber databaseNumber = new(5);

		// Act
		string result = databaseNumber.ToString();

		// Assert
		result.Should().Be("5");
	}

	[Test]
	public void Equals_SameValue_ShouldReturnTrue()
	{
		// Arrange
		DatabaseNumber databaseNumber1 = new(5);
		DatabaseNumber databaseNumber2 = new(5);

		// Act & Assert
		databaseNumber1.Should().Be(databaseNumber2);
		databaseNumber1.GetHashCode().Should().Be(databaseNumber2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValue_ShouldReturnFalse()
	{
		// Arrange
		DatabaseNumber databaseNumber1 = new(5);
		DatabaseNumber databaseNumber2 = new(10);

		// Act & Assert
		databaseNumber1.Should().NotBe(databaseNumber2);
	}
}

