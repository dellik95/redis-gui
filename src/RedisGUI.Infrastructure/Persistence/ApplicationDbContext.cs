using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Infrastructure.Persistence;

/// <summary>
/// Represents the main database context for the application
/// </summary>
public class ApplicationDbContext : DbContext, IUnitOfWork
{
	/// <summary>
	/// Initializes a new instance of the ApplicationDbContext
	/// </summary>
	/// <param name="options">The options to be used by the DbContext</param>
	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{
	}

	/// <summary>
	/// Gets or sets the DbSet of Redis connections
	/// </summary>
	public DbSet<RedisConnection> Connections { get; set; }

	/// <summary>
	/// Configures the model that was discovered by convention from the entity types
	/// </summary>
	/// <param name="modelBuilder">The builder being used to construct the model for this context</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}
}
