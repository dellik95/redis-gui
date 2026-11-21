using FluentResults;
using FluentAssertions;
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
		Result<int> result = Result.Ok(5);

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
		DomainError error = new("Test.Error", "Test error");
		Result<int> result = Result.Fail<int>(error);

		// Act
		Result<int> mapped = result.Map(x => x * 2);

		// Assert
		mapped.IsFailed.Should().BeTrue();
		mapped.GetDomainError().Should().Be(error);
	}

	[Test]
	public async Task MapAsync_SuccessfulResult_ShouldTransformValue()
	{
		// Arrange
		Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

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
		Result<int> result = Result.Ok(5);

		// Act
		Result<int> bound = result.Bind(x => Result.Ok(x * 2));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
	}

	[Test]
	public void Bind_FailedResult_ShouldReturnFailure()
	{
		// Arrange
		DomainError error = new("Test.Error", "Test error");
		Result<int> result = Result.Fail<int>(error);

		// Act
		Result<int> bound = result.Bind(x => Result.Ok(x * 2));

		// Assert
		bound.IsFailed.Should().BeTrue();
		bound.GetDomainError().Should().Be(error);
	}

	[Test]
	public async Task BindAsync_SuccessfulResult_ShouldChainOperations()
	{
		// Arrange
		Result<int> result = Result.Ok(5);

		// Act
		Result<int> bound = await result.BindAsync(async x => await Task.FromResult(Result.Ok(x * 2)));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
	}

	[Test]
	public async Task BindAsync_FromTask_SuccessfulResult_ShouldChainOperations()
	{
		// Arrange
		Task<Result<int>> resultTask = Task.FromResult(Result.Ok(5));

		// Act
		Result<int> bound = await resultTask.BindAsync(async x => await Task.FromResult(Result.Ok(x * 2)));

		// Assert
		bound.IsSuccess.Should().BeTrue();
		bound.Value.Should().Be(10);
	}

	[Test]
	public void Match_SuccessfulResult_ShouldExecuteSuccessHandler()
	{
		// Arrange
		Result<int> result = Result.Ok(42);

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
		DomainError error = new("Test.Error", "Test error");
		Result<int> result = Result.Fail<int>(error);

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
		Result<int> result = Result.Ok(42);
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
		DomainError error = new("Test.Error", "Test error");
		Result<int> result = Result.Fail<int>(error);
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
		Result<int> result = Result.Ok(42);
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

	[Test]
	public void Ensure_PredicateSatisfied_ReturnsOriginalResult()
	{
		// Arrange
		Result<int> result = Result.Ok(42);

		// Act
		Result<int> ensured = result.Ensure(x => x > 0, new DomainError("Invalid", "Must be positive"));

		// Assert
		ensured.IsSuccess.Should().BeTrue();
		ensured.Value.Should().Be(42);
	}

	[Test]
	public void Ensure_PredicateNotSatisfied_ReturnsFailure()
	{
		// Arrange
		Result<int> result = Result.Ok(-1);
		DomainError error = new("Invalid", "Must be positive");

		// Act
		Result<int> ensured = result.Ensure(x => x > 0, error);

		// Assert
		ensured.IsFailed.Should().BeTrue();
		ensured.GetDomainError().Should().Be(error);
	}

	[Test]
	public async Task Match_TaskResult_ExecutesAppropriateBranch()
	{
		// Arrange
		Task<Result> successTask = Task.FromResult(Result.Ok());
		Task<Result> failureTask = Task.FromResult(Result.Fail(new DomainError("Err", "Fail")));

		// Act
		string successResult = await successTask.Match(() => "ok", err => "failed");
		string failureResult = await failureTask.Match(() => "ok", err => "failed");

		// Assert
		successResult.Should().Be("ok");
		failureResult.Should().Be("failed");
	}
}

