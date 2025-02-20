using System;
using System.Collections.Generic;

namespace RedisGUI.Infrastructure.Persistence.InMemoryDbDataReader;

/// <summary>
/// Represents a single row of data from a database table
/// </summary>
public class DbTableRow
{
    /// <summary>
    /// Gets the values for each column in the row
    /// </summary>
    public IList<object> Values { get; }

    /// <summary>
    /// Gets or sets the depth of nesting for this row
    /// </summary>
    public int Depth { get; set; }

    /// <summary>
    /// Initializes a new instance of the DbTableRow class
    /// </summary>
    /// <param name="values">The values for each column in the row</param>
    /// <exception cref="ArgumentNullException">Thrown when values is null</exception>
    public DbTableRow(IList<object> values)
    {
        Values = values ?? throw new ArgumentNullException(nameof(values));
    }
}
