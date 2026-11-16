using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.SignalR;
using System.Security.Claims;
using RedisGUI.Infrastructure.Authorization;
using RedisGUI.Infrastructure.BackgroundJobs;

namespace RedisGUI.Infrastructure.SignalR.Hubs;

/// <summary>Subscribes an authorized owner to updates for one job.</summary>
[Authorize]
public sealed class BackgroundJobsHub(BackgroundJobManager jobs, IRedisConnectionRepository repository,
	IAuthorizationService authorization) : Hub
{
	public static string Group(Guid jobId) => $"job:{jobId:N}";

	public async Task Subscribe(Guid jobId)
	{
		var identifier = Context.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? Context.User?.FindFirst("sub")?.Value;
		var job = Guid.TryParse(identifier, out var ownerId) ? jobs.Get(jobId, ownerId) : null;
		if (job is null) throw new HubException("Job not found.");
		var connection = await repository.GetConnectionByIdAsync(job.ConnectionId, Context.ConnectionAborted);
		if (connection.IsFailure || Context.User is null ||
			!(await authorization.AuthorizeAsync(Context.User, connection.Value, new ConnectionOwnershipRequirement())).Succeeded)
			throw new HubException("Job not found.");
		await Groups.AddToGroupAsync(Context.ConnectionId, Group(jobId), Context.ConnectionAborted);
	}

	public Task Unsubscribe(Guid jobId) => Groups.RemoveFromGroupAsync(Context.ConnectionId, Group(jobId));
}
