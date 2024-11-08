namespace RedisGUI.Domain.Redis;

/// <summary>
/// Represents Redis data types
/// </summary>
public enum RedisValueType
{
    /// <summary>
    /// Represents no specific Redis type or an unknown type
    /// </summary>
    None = 0,

    /// <summary>
    /// Represents a Redis string value type
    /// </summary>
    String = 1,

    /// <summary>
    /// Represents a Redis list type
    /// </summary>
    List = 2,

    /// <summary>
    /// Represents a Redis set type
    /// </summary>
    Set = 3,

    /// <summary>
    /// Represents a Redis sorted set type
    /// </summary>
    SortedSet = 4,

    /// <summary>
    /// Represents a Redis hash type
    /// </summary>
    Hash = 5,

    /// <summary>
    /// Represents a Redis stream type
    /// </summary>
    Stream = 6
} 