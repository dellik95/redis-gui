using RedisGUI.Domain.Audit;

namespace RedisGUI.Domain.Tests.Audit;

/// <summary>
/// Tests for AuditMetadata value object
/// </summary>
public class AuditMetadataTests
{
	[Test]
	public void Create_WithAllValues_ShouldCreateInstance()
	{
		// Arrange & Act
		AuditMetadata metadata = AuditMetadata.Create(
			"127.0.0.1",
			"Mozilla/5.0",
			"oldValue",
			"newValue");

		// Assert
		metadata.IpAddress.Should().Be("127.0.0.1");
		metadata.UserAgent.Should().Be("Mozilla/5.0");
		metadata.OldValue.Should().Be("oldValue");
		metadata.NewValue.Should().Be("newValue");
	}

	[Test]
	public void Create_WithNullValues_ShouldCreateInstance()
	{
		// Arrange & Act
		AuditMetadata metadata = AuditMetadata.Create();

		// Assert
		metadata.IpAddress.Should().BeNull();
		metadata.UserAgent.Should().BeNull();
		metadata.OldValue.Should().BeNull();
		metadata.NewValue.Should().BeNull();
	}

	[Test]
	public void Create_WithPartialValues_ShouldCreateInstance()
	{
		// Arrange & Act
		AuditMetadata metadata = AuditMetadata.Create(
			ipAddress: "127.0.0.1",
			userAgent: "Mozilla/5.0");

		// Assert
		metadata.IpAddress.Should().Be("127.0.0.1");
		metadata.UserAgent.Should().Be("Mozilla/5.0");
		metadata.OldValue.Should().BeNull();
		metadata.NewValue.Should().BeNull();
	}

	[Test]
	public void Equals_SameValues_ShouldReturnTrue()
	{
		// Arrange
		AuditMetadata metadata1 = AuditMetadata.Create("127.0.0.1", "Mozilla/5.0", "old", "new");
		AuditMetadata metadata2 = AuditMetadata.Create("127.0.0.1", "Mozilla/5.0", "old", "new");

		// Act & Assert
		metadata1.Should().Be(metadata2);
		metadata1.GetHashCode().Should().Be(metadata2.GetHashCode());
	}

	[Test]
	public void Equals_DifferentValues_ShouldReturnFalse()
	{
		// Arrange
		AuditMetadata metadata1 = AuditMetadata.Create("127.0.0.1", "Mozilla/5.0");
		AuditMetadata metadata2 = AuditMetadata.Create("192.168.1.1", "Chrome");

		// Act & Assert
		metadata1.Should().NotBe(metadata2);
	}
}

