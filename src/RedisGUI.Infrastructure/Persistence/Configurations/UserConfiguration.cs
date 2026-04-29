using Microsoft.EntityFrameworkCore.Metadata.Builders;
using RedisGUI.Domain.Users;

namespace RedisGUI.Infrastructure.Persistence.Configurations;

/// <summary>
/// Entity Framework configuration for User entity
/// </summary>
internal sealed class UserConfiguration : IEntityTypeConfiguration<User>
{
	/// <summary>
	/// Configures the User entity mapping
	/// </summary>
	public void Configure(EntityTypeBuilder<User> builder)
	{
		builder.ToTable("Users");

		builder.HasKey(u => u.Id);

		// Email value object
		builder.OwnsOne(u => u.Email, email =>
		{
			email.Property(e => e.Value)
				.HasColumnName("Email")
				.HasMaxLength(255)
				.IsRequired();

			email.HasIndex(e => e.Value)
				.IsUnique();

			email.WithOwner();
		});

		// PasswordHash value object
		builder.OwnsOne(u => u.PasswordHash, passwordHash =>
		{
			passwordHash.Property(p => p.Value)
				.HasColumnName("PasswordHash")
				.HasMaxLength(512)
				.IsRequired();

			passwordHash.WithOwner();
		});

		// UserProfile value object
		builder.OwnsOne(u => u.Profile, profile =>
		{
			profile.Property(p => p.FirstName)
				.HasColumnName("FirstName")
				.HasMaxLength(100)
				.IsRequired();

			profile.Property(p => p.LastName)
				.HasColumnName("LastName")
				.HasMaxLength(100)
				.IsRequired();

			// FullName is a computed property, so we ignore it
			profile.Ignore(p => p.FullName);

			profile.WithOwner();
		});

		// Enum as string
		builder.Property(u => u.Role)
			.HasConversion<string>()
			.HasMaxLength(20)
			.IsRequired();

		builder.Property(u => u.IsActive)
			.IsRequired();

		builder.Property(u => u.CreatedAt)
			.IsRequired();

		builder.Property(u => u.LastLoginAt)
			.IsRequired(false);
	}
}
