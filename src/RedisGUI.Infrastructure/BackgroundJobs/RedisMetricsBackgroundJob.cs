using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RedisGUI.Application.RedisMetrics.Queries;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.SignalR;

namespace RedisGUI.Infrastructure.BackgroundJobs;

public class RedisMetricsBackgroundJob : BackgroundService
{
	private readonly IServiceProvider serviceProvider;
	private readonly INotificationService notificationService;
	private readonly IHubSubscribersManager hubSubscribersManager;
	private readonly IOptions<MetricsCollectorOptions> options;

	public RedisMetricsBackgroundJob(
		IServiceProvider serviceProvider,
		INotificationService notificationService,
		IHubSubscribersManager hubSubscribersManager,
		IOptions<MetricsCollectorOptions> options)
	{
		this.serviceProvider = serviceProvider;
		this.notificationService = notificationService;
		this.hubSubscribersManager = hubSubscribersManager;
		this.options = options;
	}

	/// <inheritdoc />
	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var scope = this.serviceProvider.CreateScope();
		var sender = scope.ServiceProvider.GetRequiredService<ISender>();
		while (!stoppingToken.IsCancellationRequested)
		{
			foreach (var subscriber in this.hubSubscribersManager.GetSubscribersMap().GroupBy(x => x.Value))
			{
				var metrics = await sender.Send(new GetRedisMetricsQuery(subscriber.Key), stoppingToken);

				if (!metrics.IsSuccess)
				{
					continue;
				}

				foreach (var pair in subscriber)
				{
					await this.notificationService.NotifyUser(pair.Key, metrics.Value);
				}
			}

			await Task.Delay(TimeSpan.FromSeconds(this.options.Value.PoolingIntervalInSec), stoppingToken);
		}
	}
}
