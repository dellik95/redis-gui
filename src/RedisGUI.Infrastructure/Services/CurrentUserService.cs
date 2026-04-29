using Microsoft.AspNetCore.Http;
using RedisGUI.Application.Core.Services;
using System.Security.Claims;

namespace RedisGUI.Infrastructure.Services;

/// <summary>
/// Implementation of ICurrentUserService using HttpContext
/// </summary>
internal sealed class CurrentUserService : ICurrentUserService
{
	private readonly IHttpContextAccessor _httpContextAccessor;

	public CurrentUserService(IHttpContextAccessor httpContextAccessor)
	{
		_httpContextAccessor = httpContextAccessor;
	}

	/// <summary>
	/// Gets the current user's ID from the JWT claims
	/// </summary>
	public Guid? UserId
	{
		get
		{
			string? userIdClaim = _httpContextAccessor.HttpContext?.User?.FindFirst(ClaimTypes.NameIdentifier)?.Value
				?? _httpContextAccessor.HttpContext?.User?.FindFirst("sub")?.Value;

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
	public bool IsAuthenticated => _httpContextAccessor.HttpContext?.User?.Identity?.IsAuthenticated ?? false;
}
