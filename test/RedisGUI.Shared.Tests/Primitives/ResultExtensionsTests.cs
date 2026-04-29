using FluentAssertions;
using RedisGUI.Domain.Primitives;
using RedisGUI.Shared.Primitives;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Tests.Primitives;

/// <summary>
/// Tests for ResultExtensions
/// </summary>
[TestFixture]
public class ResultExtensionsTests
{
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
	public async Task MapAsync_SuccessfulResult_ShouldTransformValue()
	{
		// Arrange
		Task<Result<int>> resultTask = Task.FromResult(Result.Success(5));

		// Act
		Result<int> mapped = await resultTask.MapAsync(async x => await Task.FromResult(x * 2));

		// Assert
		mapped.IsSuccess.Should().BeTrue();
		mapped.Value.Should().Be(10);
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
	public async Task BindAsync_SuccessfulResult_ShouldChainOperations()
	{
		// Arrange
		Result<int> result = Result.Success(5);

		// Act
		Result<int> bound = await result.BindAsync(async x => await Task.FromResult(Result.Success(x * 2)));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
	}

	[Test]
	public async Task BindAsync_FromTask_SuccessfulResult_ShouldChainOperations()
	{
		// Arrange
		Task<Result<int>> resultTask = Task.FromResult(Result.Success(5));

		// Act
		Result<int> bound = await resultTask.BindAsync(async x => await Task.FromResult(Result.Success(x * 2)));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
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

	[Test]
	public void Tap_SuccessfulResult_ShouldExecuteAction()
	{
		// Arrange
		Result<int> result = Result.Success(42);
		bool executed = false;

		// Act
		Result<int> tapped = result.Tap(x => executed = true);

		// Assert
		executed.Should().BeTrue();
		tapped.Should().Be(result);
	}

	[Test]
	public void Tap_FailedResult_ShouldNotExecuteAction()
	{
		// Arrange
		Error error = new("Test.Error", "Test error");
		Result<int> result = Result.Failure<int>(error);
		bool executed = false;

		// Act
		Result<int> tapped = result.Tap(x => executed = true);

		// Assert
		executed.Should().BeFalse();
		tapped.Should().Be(result);
	}

	[Test]
	public async Task TapAsync_SuccessfulResult_ShouldExecuteAction()
	{
		// Arrange
		Result<int> result = Result.Success(42);
		bool executed = false;

		// Act
		Result<int> tapped = await result.TapAsync(async x =>
		{
			await Task.CompletedTask;
			executed = true;
		});

		// Assert
		executed.Should().BeTrue();
		tapped.Should().Be(result);
	}
}

