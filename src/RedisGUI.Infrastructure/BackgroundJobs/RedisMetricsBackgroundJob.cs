using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using RedisGUI.Application.RedisMetrics.Queries;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.SignalR;
using RedisGUI.Shared.Mediator;
using System.Linq;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>
/// Background service that periodically collects Redis metrics and broadcasts them to connected clients
/// </summary>
public class RedisMetricsBackgroundJob : BackgroundService
{
	private readonly IServiceProvider serviceProvider;
	private readonly INotificationService notificationService;
	private readonly IHubSubscribersManager hubSubscribersManager;
	private readonly IOptions<MetricsCollectorOptions> options;
	private readonly ILogger<RedisMetricsBackgroundJob> logger;

	/// <summary>
	/// Initializes a new instance of the RedisMetricsBackgroundJob
	/// </summary>
	/// <param name="serviceProvider">Service provider for creating scoped services</param>
	/// <param name="notificationService">Service for sending notifications to clients</param>
	/// <param name="hubSubscribersManager">Manager for hub subscriber connections</param>
	/// <param name="options">Configuration options for metrics collection</param>
	/// <param name="logger">Logger instance</param>
	public RedisMetricsBackgroundJob(
		IServiceProvider serviceProvider,
		INotificationService notificationService,
		IHubSubscribersManager hubSubscribersManager,
		IOptions<MetricsCollectorOptions> options,
		ILogger<RedisMetricsBackgroundJob> logger)
	{
		this.serviceProvider = serviceProvider;
		this.notificationService = notificationService;
		this.hubSubscribersManager = hubSubscribersManager;
		this.options = options;
		this.logger = logger;
	}

	/// <summary>
	/// Executes the background job that collects and broadcasts Redis metrics
	/// </summary>
	/// <param name="stoppingToken">Token that can be used to stop the background job</param>
	/// <returns>A task representing the background operation</returns>
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		while (!stoppingToken.IsCancellationRequested)
		{
			try
			{
				using var scope = this.serviceProvider.CreateScope();
				var sender = scope.ServiceProvider.GetRequiredService<IMediator>();
				
				foreach (var subscriber in this.hubSubscribersManager.GetSubscribersMap().GroupBy(x => x.Value))
				{
					var metrics = await sender.Send<RedisGUI.Domain.RedisMetrics.RedisMetrics>(new GetRedisMetricsQuery(subscriber.Key), stoppingToken);

					if (!metrics.IsSuccess)
					{
						continue;
					}

					foreach (var pair in subscriber)
					{
						await this.notificationService.NotifyUser(pair.Key, metrics.Value);
					}
				}
			}
			catch (Exception ex) when (ex is not OperationCanceledException)
			{
				// Log the error but continue execution
				logger.LogError(ex, "Error collecting Redis metrics for subscribers");
			}

			await Task.Delay(TimeSpan.FromSeconds(this.options.Value.PoolingIntervalInSec), stoppingToken);
		}
	}
}
