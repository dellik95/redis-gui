using Microsoft.EntityFrameworkCore;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;

namespace RedisGUI.Infrastructure.Persistence;

public class ApplicationDbContext : DbContext, IUnitOfWork
{
	public DbSet<RedisConnection> Connections { get; set; }

	public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : base(options)
	{

	}

	protected override void OnModelCreating(ModelBuilder modelBuilder)
	{
		modelBuilder.ApplyConfigurationsFromAssembly(typeof(ApplicationDbContext).Assembly);

		base.OnModelCreating(modelBuilder);
	}
}