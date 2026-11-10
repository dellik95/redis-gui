using RedisGUI.Domain.Exceptions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Tests.Exceptions;

/// <summary>
/// Tests for ErrorException
/// </summary>
[TestFixture]
public class ErrorExceptionTests
{
	[Test]
	public void Constructor_WithError_ShouldSetError()
	{
		// Arrange
		Error error = new("Test.Error", "Test error message");

		// Act
		ErrorException exception = new(error);

		// Assert
		exception.Errors.Should().Equal(error);
		exception.Message.Should().Contain("Test error message");
	}

	[Test]
	public void Constructor_WithError_ShouldSetMessage()
	{
		// Arrange
		Error error = new("Test.Error", "Test error message");

		// Act
		ErrorException exception = new(error);

		// Assert
		exception.Message.Should().Be("Test error message");
	}
}

