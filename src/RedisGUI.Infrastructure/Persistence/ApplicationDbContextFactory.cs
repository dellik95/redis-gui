using Microsoft.EntityFrameworkCore.Design;

namespace RedisGUI.Infrastructure.Persistence;

/// <summary>
/// Design-time factory for ApplicationDbContext to enable EF Core CLI tools (dotnet ef)
/// </summary>
public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
	public ApplicationDbContext CreateDbContext(string[] args)
	{
		var optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
		optionsBuilder.UseMySQL("Server=localhost;Port=3306;Database=redisgui;Uid=redisgui;Pwd=redisguipass;");

		return new ApplicationDbContext(optionsBuilder.Options);
	}
}
