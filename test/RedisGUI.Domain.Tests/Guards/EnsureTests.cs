using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Guards;
using RedisGUI.Domain.Primitives;
using System;

namespace RedisGUI.Domain.Tests.Guards;

/// <summary>
/// Tests for Ensure guard clauses
/// </summary>
[TestFixture]
public class EnsureTests
{
	[Test]
	public void NotEmpty_ValidString_ShouldNotThrow()
	{
		// Arrange & Act
		Action act = () => Ensure.NotEmpty("valid", "message", "paramName");

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void NotEmpty_EmptyString_ShouldThrowArgumentException()
	{
		// Arrange & Act
		Action act = () => Ensure.NotEmpty("", "message", "paramName");

		// Assert
		act.Should().Throw<ArgumentException>()
			.WithParameterName("paramName");
	}

	[Test]
	public void NotEmpty_WhitespaceString_ShouldThrowArgumentException()
	{
		// Arrange & Act
		Action act = () => Ensure.NotEmpty("   ", "message", "paramName");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NotEmpty_WithError_ValidString_ShouldNotThrow()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");

		// Act
		Action act = () => Ensure.NotEmpty("valid", error);

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void NotEmpty_WithError_EmptyString_ShouldThrowErrorException()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");

		// Act
		Action act = () => Ensure.NotEmpty("", error);

		// Assert
		act.Should().Throw<ErrorException>()
			.Which.Errors.Should().Equal(error);
	}

	[Test]
	public void LessOrEqualZero_PositiveValue_ShouldNotThrow()
	{
		// Arrange & Act
		Action act = () => Ensure.LessOrEqualZero(5, "message", "paramName");

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void LessOrEqualZero_ZeroValue_ShouldThrowArgumentException()
	{
		// Arrange & Act
		Action act = () => Ensure.LessOrEqualZero(0, "message", "paramName");

		// Assert
		act.Should().Throw<ArgumentException>()
			.WithParameterName("paramName");
	}

	[Test]
	public void LessOrEqualZero_NegativeValue_ShouldThrowArgumentException()
	{
		// Arrange & Act
		Action act = () => Ensure.LessOrEqualZero(-1, "message", "paramName");

		// Assert
		act.Should().Throw<ArgumentException>();
	}

	[Test]
	public void NotNull_ValidValue_ShouldNotThrow()
	{
		// Arrange & Act
		Action act = () => Ensure.NotNull("value", "message", "paramName");

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void NotNull_NullValue_ShouldThrowArgumentException()
	{
		// Arrange & Act
		Action act = () => Ensure.NotNull<string>(null!, "message", "paramName");

		// Assert
		act.Should().Throw<ArgumentException>()
			.WithParameterName("paramName");
	}

	[Test]
	public void Is_TrueCondition_ShouldNotThrow()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");

		// Act
		Action act = () => Ensure.Is(() => true, error);

		// Assert
		act.Should().NotThrow();
	}

	[Test]
	public void Is_FalseCondition_ShouldThrowErrorException()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");

		// Act
		Action act = () => Ensure.Is(() => false, error);

		// Assert
		act.Should().Throw<ErrorException>()
			.Which.Errors.Should().Equal(error);
	}
}

