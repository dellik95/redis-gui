using System.Security.Claims;

namespace RedisGUI.Infrastructure.BackgroundJobs;

/// <summary>Authenticated identity explicitly bound to a background execution scope.</summary>
internal sealed class JobIdentity
{
	public ClaimsPrincipal? Principal { get; set; }
}
