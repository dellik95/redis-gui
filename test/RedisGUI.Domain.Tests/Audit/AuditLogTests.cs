using RedisGUI.Domain.Audit;
using System;

namespace RedisGUI.Domain.Tests.Audit;

/// <summary>
/// Tests for AuditLog aggregate root
/// </summary>
public class AuditLogTests
{
	[Test]
	public void Create_ValidAuditLog_ShouldCreateInstance()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		Guid entityId = Guid.NewGuid();
		AuditMetadata metadata = AuditMetadata.Create("127.0.0.1", "Mozilla/5.0");

		// Act
		AuditLog auditLog = AuditLog.Create(
			userId,
			AuditAction.Create,
			"User",
			entityId,
			metadata);

		// Assert
		auditLog.Should().NotBeNull();
		auditLog.UserId.Should().Be(userId);
		auditLog.Action.Should().Be(AuditAction.Create);
		auditLog.EntityType.Should().Be("User");
		auditLog.EntityId.Should().Be(entityId);
		auditLog.Metadata.Should().Be(metadata);
		auditLog.Timestamp.Should().BeCloseTo(DateTime.UtcNow, TimeSpan.FromSeconds(1));
		auditLog.Id.Should().NotBeEmpty();
	}

	[Test]
	public void Create_WithNullUserId_ShouldCreateInstance()
	{
		// Arrange
		AuditMetadata metadata = AuditMetadata.Create();

		// Act
		AuditLog auditLog = AuditLog.Create(
			null,
			AuditAction.Login,
			"User",
			null,
			metadata);

		// Assert
		auditLog.Should().NotBeNull();
		auditLog.UserId.Should().BeNull();
		auditLog.EntityId.Should().BeNull();
	}

	[Test]
	public void Create_DifferentActions_ShouldCreateInstances()
	{
		// Arrange
		AuditMetadata metadata = AuditMetadata.Create();

		// Act
		AuditLog createLog = AuditLog.Create(Guid.NewGuid(), AuditAction.Create, "User", Guid.NewGuid(), metadata);
		AuditLog updateLog = AuditLog.Create(Guid.NewGuid(), AuditAction.Update, "User", Guid.NewGuid(), metadata);
		AuditLog deleteLog = AuditLog.Create(Guid.NewGuid(), AuditAction.Delete, "User", Guid.NewGuid(), metadata);

		// Assert
		createLog.Action.Should().Be(AuditAction.Create);
		updateLog.Action.Should().Be(AuditAction.Update);
		deleteLog.Action.Should().Be(AuditAction.Delete);
	}
}

