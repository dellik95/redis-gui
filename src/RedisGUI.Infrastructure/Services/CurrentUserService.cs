using Microsoft.AspNetCore.Http;
using RedisGUI.Application.Core.Services;
using System.Security.Claims;
using RedisGUI.Infrastructure.BackgroundJobs;

namespace RedisGUI.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService using HttpContext
/// </summary>
internal sealed class CurrentUserService : ICurrentUserService
{
	private readonly IHttpContextAccessor _httpContextAccessor;
	private readonly JobIdentity? jobIdentity;

	public CurrentUserService(IHttpContextAccessor httpContextAccessor, JobIdentity? jobIdentity = null)
	{
		_httpContextAccessor = httpContextAccessor;
		this.jobIdentity = jobIdentity;
	}

	/// <summary>
	/// Gets the current user's ID from the JWT claims
	/// </summary>
	public Guid? UserId
	{
		get
		{
			var user = _httpContextAccessor.HttpContext?.User ?? jobIdentity?.Principal;
			string? userIdClaim = user?.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? user?.FindFirst("sub")?.Value;

			return Guid.TryParse(userIdClaim, out Guid userId) ? userId : null;
		}
	}

	/// <summary>
	/// Gets the current user's IP address
	/// </summary>
	public string? IpAddress => _httpContextAccessor.HttpContext?.Connection?.RemoteIpAddress?.ToString();

	/// <summary>
	/// Gets the current user's user agent
	/// </summary>
	public string? UserAgent => _httpContextAccessor.HttpContext?.Request?.Headers["User-Agent"].ToString();

	/// <summary>
	/// Gets whether the user is authenticated
	/// </summary>
	public bool IsAuthenticated => (_httpContextAccessor.HttpContext?.User ?? jobIdentity?.Principal)?.Identity?.IsAuthenticated ?? false;
}
