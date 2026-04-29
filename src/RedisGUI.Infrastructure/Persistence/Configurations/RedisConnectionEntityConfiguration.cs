using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisGUI.Domain.Users;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Infrastructure.Persistence.Configurations
{
	/// <summary>
	/// Entity Framework configuration for RedisConnection entity
	/// </summary>
	public class RedisConnectionEntityConfiguration : IEntityTypeConfiguration<RedisConnection>
	{
		public void Configure(EntityTypeBuilder<RedisConnection> builder)
		{
			builder.UseTpcMappingStrategy();

			builder.HasKey(x => x.Id);

			// ServerHost
			builder.Property(x => x.ServerHost)
				.IsRequired()
				.HasConversion(x => x.Value, x => new ConnectionHost(x))
				.HasMaxLength(ConnectionHost.MaxLength);

			// ConnectionName
			builder.Property(x => x.ConnectionName)
				.IsRequired()
				.HasConversion(x => x.Value, x => new ConnectionName(x))
				.HasMaxLength(ConnectionName.MaxLength);

			// ServerPort
			builder.Property(x => x.ServerPort)
				.IsRequired()
				.HasConversion(x => x.Value, x => new ConnectionPort(x));

			// DatabaseNumber
			builder.Property(x => x.DatabaseNumber)
				.IsRequired()
				.HasConversion(x => x.Value, x => new DatabaseNumber(x));

			builder.Property(x => x.OwnerId)
				.HasConversion(x => x.HasValue ? x.Value : null, x => ConnectionOwnerId.FromNullable(x));

			builder.Ignore(x => x.OwnerId);

			//builder.Property<Guid?>(nameof(RedisConnection.OwnerId));

			//builder.HasOne<User>()
			//	.WithMany()
			//	.HasForeignKey(nameof(RedisConnection.OwnerId))
			//	.HasPrincipalKey(u => u.Id)
			//	.OnDelete(DeleteBehavior.Restrict);

			// IsAvailable
			builder.Property(x => x.IsAvailable)
				.IsRequired()
				.HasDefaultValue(false);
		}
	}
}
