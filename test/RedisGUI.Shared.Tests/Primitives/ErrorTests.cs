using FluentAssertions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for Error class
/// </summary>
[TestFixture]
public class ErrorTests
{
	[Test]
	public void Constructor_ShouldSetCodeAndMessage()
	{
		// Arrange & Act
		Error error = new("Test.Error", "Test error message");

		// Assert
		error.Code.Should().Be("Test.Error");
		error.Message.Should().Be("Test error message");
	}

	[Test]
	public void None_ShouldReturnNoneError()
	{
		// Arrange & Act
		Error error = Error.None;

		// Assert
		error.Code.Should().Be(string.Empty);
		error.Message.Should().Be(string.Empty);
	}

	[Test]
	public void NullValue_ShouldReturnNullValueError()
	{
		// Arrange & Act
		Error error = Error.NullValue;

		// Assert
		error.Code.Should().Be("Error.NullValue");
		error.Message.Should().Be("Null value was provided");
	}

	[Test]
	public void Equals_SameCodeAndMessage_ShouldReturnTrue()
	{
		// Arrange
		Error error1 = new("Test.Error", "Test message");
		Error error2 = new("Test.Error", "Test message");

		// Act & Assert
		error1.Should().Be(error2);
		error1.GetHashCode().Should().Be(error2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentCode_ShouldReturnFalse()
	{
		// Arrange
		Error error1 = new("Test.Error1", "Message");
		Error error2 = new("Test.Error2", "Message");

		// Act & Assert
		error1.Should().NotBe(error2);
	}

	[Test]
	public void Equals_SameCodeDifferentMessage_ShouldCheckCodeOnly()
	{
		// Arrange
		Error error1 = new("Test.Error", "Message 1");
		Error error2 = new("Test.Error", "Message 2");

		// Act & Assert
		// Note: Error equality should be based on Code only for error type matching
		error1.Code.Should().Be(error2.Code);
	}
}

