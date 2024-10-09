using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Infrastructure.Persistence.EntityConfigurations;

public class RedisConnectionEntityConfiguration : IEntityTypeConfiguration<RedisConnection>
{
	public void Configure(EntityTypeBuilder<RedisConnection> builder)
	{
		builder.HasKey(x => x.Id);

		builder.Property(x => x.Host)
			.IsRequired()
			.HasConversion(x => x.Value, x => new ConnectionHost(x))
			.HasMaxLength(ConnectionHost.MaxLength);

		builder.Property(x => x.Name)
			.HasConversion(x => x.Value, x => new ConnectionName(x))
			.IsRequired()
			.HasMaxLength(ConnectionName.MaxLength);

		builder.Property(x => x.Port)
			.IsRequired()
			.HasConversion(x => x.Value, x => new ConnectionPort(x));

		builder.OwnsOne(x => x.Credentials);
	}
}