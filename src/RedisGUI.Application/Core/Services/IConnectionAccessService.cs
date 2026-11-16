using RedisGUI.Domain.Connection;
using System.Threading.Tasks;

namespace RedisGUI.Application.Core.Services;

/// <summary>Checks whether the current user may access a connection.</summary>
public interface IConnectionAccessService
{
	Task<bool> CanAccessAsync(RedisConnection connection);
}
