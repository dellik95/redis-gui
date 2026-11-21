using FluentAssertions;
using RedisGUI.Shared.Extensions;
using RedisGUI.Shared.Primitives;
using System.Collections.Generic;

namespace RedisGUI.Shared.Tests.Extensions;

/// <summary>
/// Tests for ErrorExtensions
/// </summary>
[TestFixture]
public class ErrorExtensionsTests
{
	[Test]
	public void ToCodeString_WithErrors_ReturnsCommaSeparatedCodes()
	{
		// Arrange
		List<DomainError> errors =
		[
			new DomainError("Error.Code1", "Message 1"),
			new DomainError("Error.Code2", "Message 2"),
			new DomainError("Error.Code3", "Message 3")
		];

		// Act
		string result = errors.ToCodeString();

		// Assert
		result.Should().Be("Error.Code1, Error.Code2, Error.Code3");
	}

	[Test]
	public void ToCodeString_EmptyCollection_ReturnsEmptyString()
	{
		// Arrange
		List<DomainError> errors = [];

		// Act
		string result = errors.ToCodeString();

		// Assert
		result.Should().BeEmpty();
	}

	[Test]
	public void ToCodeString_SingleError_ReturnsSingleCode()
	{
		// Arrange
		List<DomainError> errors =
		[
			new DomainError("Error.Single", "Single message")
		];

		// Act
		string result = errors.ToCodeString();

		// Assert
		result.Should().Be("Error.Single");
	}

	[Test]
	public void ToCodeString_NullCollection_ReturnsEmptyString()
	{
		// Arrange
		IEnumerable<DomainError>? errors = null!;

		// Act
		string result = errors.ToCodeString();

		// Assert
		result.Should().BeEmpty();
	}

	[Test]
	public void ToFormattedMessage_WithErrors_ReturnsFormattedString()
	{
		// Arrange
		List<DomainError> errors =
		[
			new DomainError("Error.Code1", "Message 1"),
			new DomainError("Error.Code2", "Message 2")
		];

		// Act
		string result = errors.ToFormattedMessage();

		// Assert
		result.Should().Be("Error.Code1: Message 1\nError.Code2: Message 2");
	}

	[Test]
	public void ToFormattedMessage_EmptyCollection_ReturnsEmptyString()
	{
		// Arrange
		List<DomainError> errors = [];

		// Act
		string result = errors.ToFormattedMessage();

		// Assert
		result.Should().BeEmpty();
	}

	[Test]
	public void ToFormattedMessage_SingleError_ReturnsSingleFormattedLine()
	{
		// Arrange
		List<DomainError> errors =
		[
			new DomainError("Error.Single", "Single message")
		];

		// Act
		string result = errors.ToFormattedMessage();

		// Assert
		result.Should().Be("Error.Single: Single message");
	}

	[Test]
	public void ToFormattedMessage_NullCollection_ReturnsEmptyString()
	{
		// Arrange
		IEnumerable<DomainError>? errors = null!;

		// Act
		string result = errors.ToFormattedMessage();

		// Assert
		result.Should().BeEmpty();
	}

	[Test]
	public void ToFormattedMessage_WithEmptyMessage_ReturnsCodeOnly()
	{
		// Arrange
		List<DomainError> errors =
		[
			new DomainError("Error.Code", string.Empty)
		];

		// Act
		string result = errors.ToFormattedMessage();

		// Assert
		result.Should().Be("Error.Code: ");
	}
}

