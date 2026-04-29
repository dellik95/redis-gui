using FluentAssertions;
using RedisGUI.Application.Exceptions;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Application.Tests.Exceptions;

/// <summary>
/// Tests for ValidationException
/// </summary>
[TestFixture]
public class ValidationExceptionTests
{
	[Test]
	public void Constructor_WithErrors_ShouldSetErrors()
	{
		// Arrange
		List<ValidationError> errors =
		[
			new ValidationError("Name", "Name is required"),
			new ValidationError("Email", "Email is invalid")
		];

		// Act
		ValidationException exception = new(errors);

		// Assert
		exception.Errors.Should().HaveCount(2);
		exception.Errors.Should().Contain(e => e.PropertyName == "Name");
		exception.Errors.Should().Contain(e => e.PropertyName == "Email");
	}

	[Test]
	public void Constructor_WithSingleError_ShouldSetError()
	{
		// Arrange
		List<ValidationError> errors =
		[
			new ValidationError("Name", "Name is required")
		];

		// Act
		ValidationException exception = new(errors);

		// Assert
		exception.Errors.Should().HaveCount(1);
		ValidationError error = exception.Errors.First();
		error.PropertyName.Should().Be("Name");
		error.ErrorMessage.Should().Be("Name is required");
	}
}

