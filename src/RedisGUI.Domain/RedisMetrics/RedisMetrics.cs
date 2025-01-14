using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Guards;

namespace RedisGUI.Domain.RedisMetrics
{
    /// <summary>
    /// Represents metrics related to Redis usage.
    /// </summary>
    public sealed class RedisMetrics
    {
        /// <summary>
        /// Creates a new instance of <see cref="RedisMetrics"/>
        /// </summary>
        private RedisMetrics(double cpu, double memory, int connectedClients, double networkSpeed, long keyCount)
        {

            Cpu = cpu;
            Memory = memory;
            ConnectedClients = connectedClients;
            NetworkSpeed = networkSpeed;
            KeyCount = keyCount;
        }

        public static RedisMetrics Create(double cpu, double memoryUsage, int connectedClients, double networkSpeed, long keyCount)
        {
	        Ensure.Is(() => cpu >= 0, DomainErrors.InvalidValue("CPU usage must be non-negative."));
	        Ensure.Is(() => memoryUsage >= 0, DomainErrors.InvalidValue("Memory usage must be non-negative."));
	        Ensure.Is(() => connectedClients >= 0, DomainErrors.InvalidValue("Connected clients must be non-negative."));
	        Ensure.Is(() => networkSpeed >= 0, DomainErrors.InvalidValue("Network speed must be non-negative."));
	        Ensure.Is(() => keyCount >= 0, DomainErrors.InvalidValue("Key count must be non-negative."));

	        return new RedisMetrics(cpu, memoryUsage, connectedClients, networkSpeed, keyCount);
        }

		/// <summary>
		/// Gets the CPU usage.
		/// </summary>
		public double Cpu { get; }

        /// <summary>
        /// Gets the memory usage.
        /// </summary>
        public double Memory { get; }

        /// <summary>
        /// Gets the number of connected clients.
        /// </summary>
        public int ConnectedClients { get; }

        /// <summary>
        /// Gets the network speed.
        /// </summary>
        public double NetworkSpeed { get; }

        /// <summary>
        /// Gets the key count.
        /// </summary>
        public long KeyCount { get; }
    }
}
