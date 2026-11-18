using System.ComponentModel.DataAnnotations;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>Configuration options for the in-process background job manager.</summary>
public sealed class BackgroundJobOptions
{
	public const string SectionName = "BackgroundJobs";

	[Range(1, 32)]
	public int WorkerCount { get; set; } = 2;

	[Range(1, 10000)]
	public int QueueCapacity { get; set; } = 100;

	[Range(1, 50000)]
	public int MaxRetainedJobs { get; set; } = 512;

	[Range(1024, 1024L * 1024 * 1024)]
	public long MaxActiveParameterBytes { get; set; } = 64L * 1024 * 1024;

	public TimeSpan ResultRetention { get; set; } = TimeSpan.FromMinutes(30);

	public TimeSpan CleanupInterval { get; set; } = TimeSpan.FromMinutes(5);
}
