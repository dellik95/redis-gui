using FluentAssertions;
using Moq;
using RedisGUI.Application.Audit.Queries.GetAuditLogs;
using RedisGUI.Domain.Audit;
using RedisGUI.Shared.Primitives;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Audit;

[TestFixture]
public class GetAuditLogsQueryHandlerTests
{
	private Mock<IAuditLogRepository> _repositoryMock = null!;
	private GetAuditLogsQueryHandler _handler = null!;

	[SetUp]
	public void Setup()
	{
		_repositoryMock = new Mock<IAuditLogRepository>();
		_handler = new GetAuditLogsQueryHandler(_repositoryMock.Object);
	}

	[Test]
	public async Task Handle_WithUserId_ReturnsUserLogs()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		GetAuditLogsQuery query = new(
			UserId: userId,
			Skip: 0,
			Take: 10);

		List<AuditLog> logs =
		[
			CreateTestAuditLog(userId, AuditAction.Create, "User", Guid.NewGuid())
		];

		_repositoryMock
			.Setup(x => x.GetByUserIdAsync(userId, 0, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(logs);

		// Act
		Result<GetAuditLogsResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		_repositoryMock.Verify(x => x.GetByUserIdAsync(userId, 0, 10, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_WithEntityTypeAndId_ReturnsEntityLogs()
	{
		// Arrange
		Guid entityId = Guid.NewGuid();
		GetAuditLogsQuery query = new(
			EntityType: "Connection",
			EntityId: entityId);

		List<AuditLog> logs =
		[
			CreateTestAuditLog(Guid.NewGuid(), AuditAction.Update, "Connection", entityId)
		];

		_repositoryMock
			.Setup(x => x.GetByEntityAsync("Connection", entityId, It.IsAny<CancellationToken>()))
			.ReturnsAsync(logs);

		// Act
		Result<GetAuditLogsResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		_repositoryMock.Verify(x => x.GetByEntityAsync("Connection", entityId, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_WithDateRange_ReturnsDateRangeLogs()
	{
		// Arrange
		DateTime fromDate = DateTime.UtcNow.AddDays(-7);
		DateTime toDate = DateTime.UtcNow;
		GetAuditLogsQuery query = new(
			FromDate: fromDate,
			ToDate: toDate,
			Skip: 0,
			Take: 10);

		List<AuditLog> logs =
		[
			CreateTestAuditLog(Guid.NewGuid(), AuditAction.Create, "User", Guid.NewGuid())
		];

		_repositoryMock
			.Setup(x => x.GetByDateRangeAsync(fromDate, toDate, 0, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync(logs);

		// Act
		Result<GetAuditLogsResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
		_repositoryMock.Verify(x => x.GetByDateRangeAsync(fromDate, toDate, 0, 10, It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_NoFilters_ReturnsRecentLogs()
	{
		// Arrange
		GetAuditLogsQuery query = new(
			Skip: 0,
			Take: 10);

		List<AuditLog> logs =
		[
			CreateTestAuditLog(Guid.NewGuid(), AuditAction.Create, "User", Guid.NewGuid())
		];

		_repositoryMock
			.Setup(x => x.GetByDateRangeAsync(
				It.Is<DateTime>(d => d <= DateTime.UtcNow.AddDays(-29) && d >= DateTime.UtcNow.AddDays(-31)),
				It.Is<DateTime>(d => d <= DateTime.UtcNow && d >= DateTime.UtcNow.AddSeconds(-1)),
				0,
				10,
				It.IsAny<CancellationToken>()))
			.ReturnsAsync(logs);

		// Act
		Result<GetAuditLogsResponse> result = await _handler.Handle(query, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Items.Should().HaveCount(1);
	}

	private static AuditLog CreateTestAuditLog(Guid userId, AuditAction action, string entityType, Guid entityId)
	{
		return AuditLog.Create(
			userId,
			action,
			entityType,
			entityId,
			AuditMetadata.Create("127.0.0.1", "TestAgent"));
	}
}

