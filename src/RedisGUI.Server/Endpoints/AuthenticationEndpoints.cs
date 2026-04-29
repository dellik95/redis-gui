using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Routing;
using RedisGUI.Application.Users.Commands.LoginUser;
using RedisGUI.Application.Users.Commands.RegisterUser;
using RedisGUI.Server.Abstraction;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Server.Endpoints;

/// <summary>
/// Defines authentication-related endpoints
/// </summary>
public class AuthenticationEndpoints : IEndpoint
{
	/// <summary>
	/// Maps authentication endpoints
	/// </summary>
	public void MapEndpoint(IEndpointRouteBuilder app)
	{
		RouteGroupBuilder group = app.MapGroup("auth")
			.WithTags("Authentication")
			.RequireRateLimiting("auth");

		group.MapPost("register", RegisterUser)
			.WithName("RegisterUser")
			.WithSummary("Register a new user")
			.Produces<Guid>(StatusCodes.Status200OK)
			.Produces<ProblemDetails>(StatusCodes.Status400BadRequest);

		group.MapPost("login", LoginUser)
			.WithName("LoginUser")
			.WithSummary("Authenticate a user and get JWT token")
			.Produces<LoginUserResponse>(StatusCodes.Status200OK)
			.Produces<ProblemDetails>(StatusCodes.Status401Unauthorized);
	}

	/// <summary>
	/// Registers a new user
	/// </summary>
	private static async Task<IResult> RegisterUser(
		[FromBody] RegisterUserCommand command,
		[FromServices] IMediator mediator,
		CancellationToken cancellationToken)
	{
		Result<Guid> result = await mediator.Send(command, cancellationToken);

		return result.IsSuccess
			? Results.Ok(result.Value)
			: Results.BadRequest(new ProblemDetails
			{
				Title = "Registration failed",
				Detail = result.Error.Message,
				Status = StatusCodes.Status400BadRequest
			});
	}

	/// <summary>
	/// Authenticates a user and returns a JWT token
	/// </summary>
	private static async Task<IResult> LoginUser(
		[FromBody] LoginUserCommand command,
		[FromServices] IMediator mediator,
		CancellationToken cancellationToken)
	{
		Result<LoginUserResponse> result = await mediator.Send(command, cancellationToken);

		return result.IsSuccess
			? Results.Ok(result.Value)
			: Results.Unauthorized();
	}
}
