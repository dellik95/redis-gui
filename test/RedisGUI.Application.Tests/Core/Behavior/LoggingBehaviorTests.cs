using FluentAssertions;
using Microsoft.Extensions.Logging;
using Moq;
using RedisGUI.Application.Core.Behavior;
using RedisGUI.Shared.Mediator;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Core.Behavior;

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
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("Starting executing request")),
				It.IsAny<Exception>(),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
		_loggerMock.Verify(
			x => x.Log(
				LogLevel.Information,
				It.IsAny<EventId>(),
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("processed successfully")),
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
				It.Is<It.IsAnyType>((v, t) => v.ToString()!.Contains("processing failed")),
				It.Is<Exception>(e => e == exception),
				It.IsAny<Func<It.IsAnyType, Exception?, string>>()),
			Times.Once);
	}

	private class TestRequest : IRequest<TestResponse>
	{
		public string Name { get; set; } = string.Empty;
	}

	private class TestResponse
	{
		public string Result { get; set; } = string.Empty;
	}
}

