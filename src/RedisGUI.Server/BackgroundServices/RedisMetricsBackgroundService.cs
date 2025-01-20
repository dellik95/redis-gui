using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Options;
using RedisGUI.Application.RedisMetrics.Queries;
using RedisGUI.Infrastructure.Configuration;
using RedisGUI.Infrastructure.SignalR;

namespace RedisGUI.Server.BackgroundServices;

public class RedisMetricsBackgroundService : BackgroundService
{
	private readonly IServiceProvider serviceProvider;
	private readonly INotificationService notificationService;
	private readonly IHubSubscribersManager hubSubscribersManager;
	private readonly IOptions<MetricsCollectorOptions> options;

	public RedisMetricsBackgroundService(
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
			foreach (var subscriber in this.hubSubscribersManager.GetSubscribersMap())
			{
				var metrics = await sender.Send(new GetRedisMetricsQuery(subscriber.Value), stoppingToken);
				if (metrics.IsSuccess)
				{
					await this.notificationService.NotifyUser(subscriber.Key, metrics.Value);
				}
			}

			await Task.Delay(TimeSpan.FromSeconds(this.options.Value.PoolingIntervalInSec), stoppingToken);
		}
	}
}
