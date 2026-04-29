using FluentAssertions;
using Moq;
using RedisGUI.Application.Connections.GetConnectionHealth;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Connections;

[TestFixture]
public class GetConnectionHealthQueryHandlerTests
{
	private Mock<IConnectionHealthMonitor> _healthMonitorMock = null!;
	private GetConnectionHealthQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_healthMonitorMock = new Mock<IConnectionHealthMonitor>();
		_handler = new GetConnectionHealthQueryHandler(_healthMonitorMock.Object);
	}

	[Test]
	public async Task Handle_HealthExists_ReturnsHealthResponse()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetConnectionHealthQuery query = new(connectionId);

		ConnectionHealth health = ConnectionHealth.Healthy(connectionId, 10);

		_healthMonitorMock
			.Setup(x => x.GetHealth(connectionId))
			.Returns(health);

		// Act
		Result<GetConnectionHealthResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.ConnectionId.Should().Be(connectionId);
		result.Value.Status.Should().Be("Healthy");
		result.Value.ResponseTimeMs.Should().Be(10);
	}

	[Test]
	public async Task Handle_HealthNotFound_ReturnsFailure()
	{
		// Arrange
		Guid connectionId = Guid.NewGuid();
		GetConnectionHealthQuery query = new(connectionId);

		_healthMonitorMock
			.Setup(x => x.GetHealth(connectionId))
			.Returns((ConnectionHealth?)null);

		// Act
		Result<GetConnectionHealthResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.Health.NotFound);
	}
}

