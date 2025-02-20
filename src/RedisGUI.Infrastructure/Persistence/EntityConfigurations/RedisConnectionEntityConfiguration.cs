using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Infrastructure.Persistence.EntityConfigurations;

/// <summary>
/// Entity Framework configuration for RedisConnection entity
/// </summary>
public class RedisConnectionEntityConfiguration : IEntityTypeConfiguration<RedisConnection>
{
	/// <summary>
	/// Configures the entity mapping for RedisConnection
	/// </summary>
	/// <param name="builder">The entity type builder used to configure the entity</param>
	public void Configure(EntityTypeBuilder<RedisConnection> builder)
	{
		builder.UseTpcMappingStrategy();

		builder.HasKey(x => x.Id);

		builder.Property(x => x.ServerHost)
			.IsRequired()
			.HasConversion(x => x.Value, x => new ConnectionHost(x))
			.HasMaxLength(ConnectionHost.MaxLength);

		builder.Property(x => x.ConnectionName)
			.HasConversion(x => x.Value, x => new ConnectionName(x))
			.IsRequired()
			.HasMaxLength(ConnectionName.MaxLength);

		builder.Property(x => x.ServerPort)
			.IsRequired()
			.HasConversion(x => x.Value, x => new ConnectionPort(x));

		builder.Property(x => x.IsAvailable)
			.IsRequired()
			.HasDefaultValue(false);
	}
}
