using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Core.Behaviors;

[TestFixture]
public class LoggingBehaviorTests
{
	private Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>> _loggerMock = null!;
	private LoggingBehavior<TestRequest, TestResponse> _behavior = null!;

	[SetUp]
	public void Setup()
	{
		_loggerMock = new Mock<ILogger<LoggingBehavior<TestRequest, TestResponse>>>();
		_behavior = new LoggingBehavior<TestRequest, TestResponse>(_loggerMock.Object);
	}

	[Test]
	public async Task Handle_SuccessfulRequest_ShouldLogStartAndSuccess()
	{
		// Arrange
		TestRequest request = new() { Name = "Test" };
		TestResponse response = new() { Result = "Success" };

		// Act
		TestResponse result = await _behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(response));

		// Assert
		result.Should().Be(response);
		_loggerMock.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("Starting executing request")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		_loggerMock.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("processed successfully")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Test]
	public async Task Handle_FailedRequest_ShouldLogError()
	{
		// Arrange
		TestRequest request = new() { Name = "Test" };
		InvalidOperationException exception = new("Test error");

		// Act
		Func<Task<TestResponse>> act = async () => await _behavior.Handle(
			request,
			CancellationToken.None,
			async () =>
			{
				await Task.CompletedTask;
				throw exception;
			});

		// Assert
		await act.Should().ThrowAsync<InvalidOperationException>();
		_loggerMock.Verify(
			x => x.Log(
				LogLevel.Error,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v != null && v.ToString()!.Contains("processing failed")),
				It.Is<Exception>(e => e == exception),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	[Test]
	public async Task Handle_FailureResult_ShouldLogWarningInsteadOfSuccess()
	{
		var logger = new Mock<ILogger<LoggingBehavior<FailedRequest, Result<string>>>>();
		var behavior = new LoggingBehavior<FailedRequest, Result<string>>(logger.Object);
		var failure = Result.Failure<string>(new Error("Metrics.Failed", "Metrics unavailable"));

		var result = await behavior.Handle(new FailedRequest(), CancellationToken.None, () => Task.FromResult(failure));

		result.Should().BeSameAs(failure);
		logger.Verify(x => x.Log(LogLevel.Warning, It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((value, type) => value != null && value.ToString()!.Contains("Metrics.Failed")),
			It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Once);
		logger.Verify(x => x.Log(LogLevel.Information, It.IsAny<EventId>(),
			It.Is<It.IsAnyType>((value, type) => value != null && value.ToString()!.Contains("processed successfully")),
			It.IsAny<Exception>(), It.IsAny<Func<It.IsAnyType, Exception?, string>>()), Times.Never);
	}

	public class FailedRequest : IRequest<Result<string>> { }

	public class TestRequest : IRequest<TestResponse>
	{
		public string Name { get; set; } = string.Empty;
	}

	public class TestResponse
	{
		public string Result { get; set; } = string.Empty;
	}
}


