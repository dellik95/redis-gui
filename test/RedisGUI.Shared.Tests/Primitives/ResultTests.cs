using FluentAssertions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for Result pattern
/// </summary>
[TestFixture]
public class ResultTests
{
	[Test]
	public void Success_ShouldCreateSuccessfulResult()
	{
		// Arrange & Act
		Result result = Result.Success();

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.IsFailure.Should().BeFalse();
		result.Error.Should().Be(Error.None);
	}

	[Test]
	public void Success_WithValue_ShouldCreateSuccessfulResultWithValue()
	{
		// Arrange & Act
		Result<int> result = Result.Success(42);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.IsFailure.Should().BeFalse();
		result.Value.Should().Be(42);
		result.Error.Should().Be(Error.None);
	}

	[Test]
	public void Failure_ShouldCreateFailedResult()
	{
		// Arrange
		Error error = new("Test.Error", "Test error message");

		// Act
		Result result = Result.Failure(error);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(error);
	}

	[Test]
	public void Failure_WithValue_ShouldCreateFailedResultWithError()
	{
		// Arrange
		Error error = new("Test.Error", "Test error message");

		// Act
		Result<int> result = Result.Failure<int>(error);

		// Assert
		result.IsSuccess.Should().BeFalse();
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(error);
	}

	[Test]
	public void Map_SuccessfulResult_ShouldTransformValue()
	{
		// Arrange
		Result<int> result = Result.Success(5);

		// Act
		Result<int> mapped = result.Map(x => x * 2);

		// Assert
		mapped.IsSuccess.Should().BeTrue();
		mapped.Value.Should().Be(10);
	}

	[Test]
	public void Map_FailedResult_ShouldReturnFailure()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");
		Result<int> result = Result.Failure<int>(error);

		// Act
		Result<int> mapped = result.Map(x => x * 2);

		// Assert
		mapped.IsFailure.Should().BeTrue();
		mapped.Error.Should().Be(error);
	}

	[Test]
	public void Bind_SuccessfulResult_ShouldChainOperations()
	{
		// Arrange
		Result<int> result = Result.Success(5);

		// Act
		Result<int> bound = result.Bind(x => Result.Success(x * 2));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
	}

	[Test]
	public void Bind_FailedResult_ShouldReturnFailure()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");
		Result<int> result = Result.Failure<int>(error);

		// Act
		Result<int> bound = result.Bind(x => Result.Success(x * 2));

		// Assert
		bound.IsFailure.Should().BeTrue();
		bound.Error.Should().Be(error);
	}

	[Test]
	public void Match_SuccessfulResult_ShouldExecuteSuccessHandler()
	{
		// Arrange
		Result<int> result = Result.Success(42);

		// Act
		string value = result.Match(
			onSuccess: x => x.ToString(),
			onFailure: error => "Error");

		// Assert
		value.Should().Be("42");
	}

	[Test]
	public void Match_FailedResult_ShouldExecuteFailureHandler()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");
		Result<int> result = Result.Failure<int>(error);

		// Act
		string value = result.Match(
			onSuccess: x => x.ToString(),
			onFailure: err => err.Code);

		// Assert
		value.Should().Be("Test.Error");
	}
}

