using System.Collections.Concurrent;
using System.Linq;

namespace RedisGUI.Infrastructure.Persistence.SqlProcessor;

/// <summary>
/// Processes SQL commands to extract metadata and determine command types
/// </summary>
public class SqlCommandsProcessor : ISqlCommandsProcessor
{
	private static readonly string[] crudMarkers = ["merge ", "insert ", "update ", "delete ", "create "];
	private static readonly string[] tableMarkers = ["from", "join", "into", "update", "merge"];
	private static readonly string[] separator = ["."];
	private readonly ConcurrentDictionary<string, Lazy<SortedSet<string>>> commandTableNames = new(StringComparer.OrdinalIgnoreCase);

	/// <inheritdoc />
	public bool IsCrudCommand(string text)
	{
		if (string.IsNullOrWhiteSpace(text))
		{
			return false;
		}

		return text.Split(['\n'], StringSplitOptions.RemoveEmptyEntries)
				   .Any(line => crudMarkers.Any(marker => line.Trim().StartsWith(marker, StringComparison.OrdinalIgnoreCase)));
	}

	/// <inheritdoc />
	public SortedSet<string> GetSqlCommandTableNames(string commandText, DbContext context, string[]? columnsToInclude = null, string[]? columnsToExclude = null)
	{
		string commandTextKey = $"{commandText.GetHashCode():X}";

		return commandTableNames.GetOrAdd(commandTextKey,
				_ => new Lazy<SortedSet<string>>(() => GetFilteredTableNames(commandText, context, columnsToInclude, columnsToExclude),
					LazyThreadSafetyMode.ExecutionAndPublication))
			.Value;
	}

	private static SortedSet<string> GetFilteredTableNames(string commandText, DbContext context, string[]? columnsToInclude, string[]? columnsToExclude)
	{
		List<string> contextTableNames = GetTableNamesFromContext(context);
		SortedSet<string> rawSqlTableNames = GetRawSqlCommandTableNames(commandText);
		IEnumerable<string> allTableNames = rawSqlTableNames.Intersect(contextTableNames);

		IEnumerable<string> filteredNames = allTableNames
			.Intersect(columnsToInclude ?? [])
			.Except(columnsToExclude ?? []);
		return [.. filteredNames];
	}

	private static List<string> GetTableNamesFromContext(DbContext context)
	{
		if (context == null)
		{
			return [];
		}

		return context.Model.GetEntityTypes()
			.Select(t => t.GetTableName())
			.Where(t => !string.IsNullOrEmpty(t))
			.Select(t => t!)
			.Distinct()
			.ToList();
	}

	private static SortedSet<string> GetRawSqlCommandTableNames(string commandText)
	{
		SortedSet<string> tables = new(StringComparer.OrdinalIgnoreCase);
		string[] sqlItems = commandText.Split([" ", "\r\n", Environment.NewLine, "\n"], StringSplitOptions.RemoveEmptyEntries);

		for (int i = 0; i < sqlItems.Length; i++)
		{
			foreach (string marker in tableMarkers)
			{
				if (!sqlItems[i].Equals(marker, StringComparison.OrdinalIgnoreCase))
				{
					continue;
				}

				if (++i >= sqlItems.Length)
				{
					break;
				}

				string tableName = ExtractTableName(sqlItems[i]);
				if (!string.IsNullOrWhiteSpace(tableName))
				{
					tables.Add(tableName);
				}
			}
		}

		return tables;
	}

	private static string ExtractTableName(string sqlItem)
	{
		string[] tableNameParts = sqlItem.Split(separator, StringSplitOptions.RemoveEmptyEntries);
		string tableName = tableNameParts.Length switch
		{
			1 => tableNameParts[0].Trim(),
			>= 2 => tableNameParts[1].Trim(),
			_ => string.Empty
		};

		return CleanTableName(tableName);
	}

	private static string CleanTableName(string tableName)
	{
		return tableName.Replace("[", string.Empty)
						.Replace("]", string.Empty)
						.Replace("'", string.Empty)
						.Replace("`", string.Empty)
						.Replace("\"", string.Empty)
						.Replace("(", string.Empty)
						.Replace(")", string.Empty);
	}
}
