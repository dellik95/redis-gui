using StackExchange.Redis;
using System;
using System.Collections.Generic;
using System.Linq;

namespace RedisGUI.Infrastructure.Redis.TypeHandlers;

/// <summary>
/// Factory that registers and resolves type handler strategies for Redis data types.
/// </summary>
public sealed class RedisTypeHandlerFactory : IRedisTypeHandlerFactory
{
	private readonly Dictionary<RedisType, IRedisTypeHandler> _handlers;

	/// <summary>
	/// Initializes a new instance with handlers injected from the DI container.
	/// </summary>
	public RedisTypeHandlerFactory(IEnumerable<IRedisTypeHandler> handlers)
	{
		_handlers = handlers.ToDictionary(h => h.Type);
	}

	/// <inheritdoc />
	public IRedisTypeHandler? GetHandler(RedisType type)
	{
		_handlers.TryGetValue(type, out var handler);
		return handler;
	}

	/// <inheritdoc />
	public IRedisTypeHandler? GetHandler(string typeName)
	{
		if (string.IsNullOrWhiteSpace(typeName))
		{
			return null;
		}

		if (Enum.TryParse<RedisType>(typeName, ignoreCase: true, out var type) && type != RedisType.None && type != RedisType.Unknown)
		{
			return GetHandler(type);
		}

		return null;
	}
}
