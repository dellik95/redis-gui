using StackExchange.Redis;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Factory for retrieving the appropriate Redis type handler strategy.
/// </summary>
public interface IRedisTypeHandlerFactory
{
	/// <summary>
	/// Gets the strategy handler for the specified RedisType.
	/// </summary>
	IRedisTypeHandler? GetHandler(RedisType type);

	/// <summary>
	/// Gets the strategy handler for the specified type name.
	/// </summary>
	IRedisTypeHandler? GetHandler(string typeName);
}
