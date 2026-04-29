using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisGUI.Domain.Audit;

namespace RedisGUI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for AuditLog entity
/// </summary>
internal sealed class AuditLogConfiguration : IEntityTypeConfiguration<AuditLog>
{
	/// <summary>
	/// Configures the AuditLog entity mapping
	/// </summary>
	public void Configure(EntityTypeBuilder<AuditLog> builder)
	{
		builder.ToTable("AuditLogs");

		builder.HasKey(a => a.Id);

		builder.Property(a => a.UserId)
			.IsRequired(false);

		builder.Property(a => a.Action)
			.HasConversion<string>()
			.HasMaxLength(50)
			.IsRequired();

		builder.Property(a => a.EntityType)
			.HasMaxLength(100)
			.IsRequired();

		builder.Property(a => a.EntityId)
			.IsRequired(false);

		builder.Property(a => a.Timestamp)
			.IsRequired();

		// AuditMetadata value object
		builder.OwnsOne(a => a.Metadata, metadata =>
		{
			metadata.Property(m => m.IpAddress)
				.HasColumnName("IpAddress")
				.HasMaxLength(45)
				.IsRequired(false);

			metadata.Property(m => m.UserAgent)
				.HasColumnName("UserAgent")
				.HasMaxLength(500)
				.IsRequired(false);

			metadata.Property(m => m.OldValue)
				.HasColumnName("OldValue")
				.HasMaxLength(4000)
				.IsRequired(false);

			metadata.Property(m => m.NewValue)
				.HasColumnName("NewValue")
				.HasMaxLength(4000)
				.IsRequired(false);
		});

		// Indexes for common queries
		builder.HasIndex(a => a.UserId);
		builder.HasIndex(a => a.Timestamp);
		builder.HasIndex(a => new { a.EntityType, a.EntityId });
	}
}
