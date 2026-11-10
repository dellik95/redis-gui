using RedisGUI.Domain.Redis;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Domain.Connection;

/// <summary>
/// Service responsible for generating and seeding rich synthetic demo data into Redis.
/// </summary>
public interface IRedisSeedDataService
{
	/// <summary>
	/// Seeds rich configurable demo/test data across Redis databases with configurable key/data complexities and TTLs.
	/// </summary>
	/// <param name="connection">The target Redis connection.</param>
	/// <param name="options">Configuration options controlling data types, counts, databases, and complexity.</param>
	/// <param name="cancellationToken">Cancellation token.</param>
	/// <returns>Summary of created keys, data types, and affected databases.</returns>
	Task<Result<SeedDataResult>> SeedSampleDataAsync(
		RedisConnection connection,
		SeedSampleDataOptions? options = null,
		CancellationToken cancellationToken = default);
}
