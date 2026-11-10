using FluentAssertions;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Domain.Audit;

namespace RedisGUI.Application.Tests.Core.Attributes;

/// <summary>
/// Tests for AuditableAttribute
/// </summary>
[TestFixture]
public class AuditableAttributeTests
{
	[Test]
	public void Constructor_WithActionAndEntityType_ShouldSetProperties()
	{
		// Arrange & Act
		AuditableAttribute attribute = new(AuditAction.Create, "User");

		// Assert
		attribute.Action.Should().Be(AuditAction.Create);
		attribute.EntityType.Should().Be("User");
	}

	[Test]
	public void Constructor_WithActionOnly_ShouldSetAction()
	{
		// Arrange & Act
		AuditableAttribute attribute = new(AuditAction.Update);

		// Assert
		attribute.Action.Should().Be(AuditAction.Update);
		attribute.EntityType.Should().BeNull();
	}
}

