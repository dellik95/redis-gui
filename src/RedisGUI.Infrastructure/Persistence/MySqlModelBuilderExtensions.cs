namespace Microsoft.EntityFrameworkCore;

/// <summary>
/// Compatibility extension method for migrations generated with Pomelo
/// </summary>
public static class MySqlModelBuilderExtensions
{
    public static ModelBuilder AutoIncrementColumns(this ModelBuilder modelBuilder)
    {
        return modelBuilder;
    }
}
