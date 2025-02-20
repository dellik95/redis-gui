using System.Collections.Generic;
using Microsoft.EntityFrameworkCore;

namespace RedisGUI.Infrastructure.Persistence.SqlProcessor;

/// <summary>
/// Provides functionality to process SQL commands and extract metadata
/// </summary>
public interface ISqlCommandsProcessor
{
    /// <summary>
    /// Gets the table names referenced in the SQL command
    /// </summary>
    /// <param name="commandText">The SQL command text</param>
    /// <param name="context">The database context</param>
    /// <param name="columnsToInclude">Optional array of column names to include</param>
    /// <param name="columnsToExclude">Optional array of column names to exclude</param>
    /// <returns>A sorted set of table names</returns>
    SortedSet<string> GetSqlCommandTableNames(string commandText, DbContext context, string[] columnsToInclude = null, string[] columnsToExclude = null);
    
    /// <summary>
    /// Determines if the SQL command is a CRUD operation
    /// </summary>
    /// <param name="text">The SQL command text to check</param>
    /// <returns>True if the command is a CRUD operation, false otherwise</returns>
    bool IsCrudCommand(string text);
}
