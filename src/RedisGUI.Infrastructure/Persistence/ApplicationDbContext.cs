using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;

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
	/// Gets or sets the DbSet of users
	/// </summary>
	public DbSet<User> Users { get; set; }

	/// <summary>
	/// Gets or sets the DbSet of audit logs
	/// </summary>
	public DbSet<AuditLog> AuditLogs { get; set; }

	/// <summary>
	/// Configures the model that was discovered by convention from the entity types
	/// </summary>
	/// <param name="modelBuilder">The builder being used to construct the model for this context</param>
	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}

	/// <summary>
	/// Saves all changes made in this context to the database asynchronously
	/// Handles infrastructure-specific exceptions and converts them to domain errors
	/// </summary>
	/// <param name="cancellationToken">A token to cancel the asynchronous operation</param>
	/// <returns>A Result containing the number of state entries written to the database, or an error if the operation failed</returns>
	public new async Task<Result<int>> SaveChangesAsync(CancellationToken cancellationToken = default)
	{
		try
		{
			int count = await base.SaveChangesAsync(cancellationToken);
			return Result.Success(count);
		}
		catch (DbUpdateConcurrencyException)
		{
			return Result.Failure<int>(DomainErrors.Storage.ConcurrencyConflict);
		}
		catch (DbUpdateException)
		{
			// Handle other database update exceptions (e.g., constraint violations, connection issues)
			// Return a generic storage error - specific error details should be logged by infrastructure
			return Result.Failure<int>(DomainErrors.Storage.ItemNotFound);
		}
	}
}
