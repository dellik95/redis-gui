using FluentAssertions;
using Moq;
using RedisGUI.Application.Audit.Queries.GetAuditLogs;
using RedisGUI.Domain.Audit;
using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Audit;

[TestFixture]
public class GetAuditLogsQueryHandlerTests
{
	[Test]
	public async Task Handle_CombinedFiltersAndPagination_ReturnsTotalMatchingCount()
	{
		var repository = new Mock<IAuditLogRepository>();
		Guid userId = Guid.NewGuid();
		Guid entityId = Guid.NewGuid();
		DateTime from = DateTime.UtcNow.AddDays(-7);
		var log = AuditLog.Create(userId, AuditAction.Update, "Connection", entityId, AuditMetadata.Create(null, null));
		repository.Setup(x => x.SearchAsync(userId, "Connection", entityId, from, null, 10, 10, It.IsAny<CancellationToken>()))
			.ReturnsAsync((new List<AuditLog> { log }, 21));
		var handler = new GetAuditLogsQueryHandler(repository.Object);

		var result = await handler.Handle(new GetAuditLogsQuery(UserId: userId, EntityType: "Connection", EntityId: entityId, FromDate: from, Skip: 10, Take: 10), CancellationToken.None);

		result.IsSuccess.Should().BeTrue();
		result.Value.TotalCount.Should().Be(21);
		result.Value.Items.Should().ContainSingle();
		result.Value.Skip.Should().Be(10);
	}
}
