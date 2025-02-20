using Microsoft.AspNetCore.Routing;

namespace RedisGUI.Server.Abstraction
{
	/// <summary>
	/// Represents the base interface for all API endpoints in the application.
	/// Provides a consistent way to define and map HTTP endpoints.
	/// </summary>
	public interface IEndpoint
	{
		/// <summary>
		/// Maps the endpoint to the application's routing configuration.
		/// </summary>
		/// <param name="routerBuilder">The router builder used to configure the endpoint's routing and behavior.</param>
		void MapEndpoint(IEndpointRouteBuilder routerBuilder);
	}
}
