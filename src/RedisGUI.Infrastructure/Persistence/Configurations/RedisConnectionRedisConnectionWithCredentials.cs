using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RedisGUI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for RedisConnection entity
/// </summary>
public class RedisConnectionRedisConnectionWithCredentials : IEntityTypeConfiguration<RedisConnectionWithCredentials>
{
	/// <summary>
	/// Configures the entity mapping for RedisConnection
	/// </summary>
	/// <param name="builder">The entity type builder used to configure the entity</param>
	public void Configure(EntityTypeBuilder<RedisConnectionWithCredentials> builder)
	{
		builder.OwnsOne(x => x.ConnectionCredentials);
	}
}
