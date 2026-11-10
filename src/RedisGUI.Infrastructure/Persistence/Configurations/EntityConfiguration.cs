using Microsoft.EntityFrameworkCore.Metadata.Builders;

namespace RedisGUI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Base entity configuration that applies common settings to all entities
/// </summary>
/// <typeparam name="T">Entity type</typeparam>
public abstract class EntityConfiguration<T> : IEntityTypeConfiguration<T>
	where T : Entity
{
	/// <summary>
	/// Configures the entity with common settings including concurrency control
	/// </summary>
	/// <param name="builder">The entity type builder</param>
	public void Configure(EntityTypeBuilder<T> builder)
	{
		// Configure RowVersion for optimistic concurrency control
		builder.Property(e => e.RowVersion)
			.IsRowVersion()
			.IsConcurrencyToken();

		// Call derived class configuration
		ConfigureEntity(builder);
	}

	/// <summary>
	/// Configures entity-specific settings
	/// </summary>
	/// <param name="builder">The entity type builder</param>
	protected abstract void ConfigureEntity(EntityTypeBuilder<T> builder);
}

