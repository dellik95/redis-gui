using System;
using System.Threading;
using System.Threading.Tasks;
using MediatR;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using RedisGUI.Application.RedisMetrics.Queries.GetRedisMetrics;
using RedisGUI.Infrastructure.SignalR;

namespace RedisGUI.Infrastructure.BackgroundServices;

public class RedisMetricsBackgroundService : BackgroundService
{
	private readonly INotificationService notificationService;
	private readonly IRedisMetricsHubConnectionManager connectionManager;
	private readonly IServiceProvider serviceProvider;
	private readonly ILogger<RedisMetricsBackgroundService> logger;

	public RedisMetricsBackgroundService(
		INotificationService notificationService,
		IRedisMetricsHubConnectionManager connectionManager,
		IServiceProvider serviceProvider,
		ILogger<RedisMetricsBackgroundService> logger)
	{
		this.notificationService = notificationService;
		this.connectionManager = connectionManager;
		this.serviceProvider = serviceProvider;
		this.logger = logger;
	}

	protected override async Task ExecuteAsync(CancellationToken stoppingToken)
	{
		using var scope = this.serviceProvider.CreateScope();
		var sender = scope.ServiceProvider.GetRequiredService<ISender>();
		while (!stoppingToken.IsCancellationRequested)
		{
			foreach (var connection in connectionManager.GetAllConnections())
			{
				try
				{
					var request = new GetRedisMetricsQuery(connection.redisConnectionId);
					var response = await sender.Send(request, stoppingToken);

					if (response.IsSuccess)
					{
						await notificationService.NotifyUser(connection.clientId, response.Value);
					}

				}
				catch (Exception e)
				{
					this.logger.LogError(e, "Error on processing metrics for {ClientId} with connection {ConnectionId}", connection.clientId, connection.redisConnectionId);
				}
			}

			await Task.Delay(TimeSpan.FromSeconds(1), stoppingToken);
		}
	}
}
