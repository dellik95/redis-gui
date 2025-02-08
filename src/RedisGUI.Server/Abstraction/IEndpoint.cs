using Microsoft.AspNetCore.Routing;

namespace RedisGUI.Server.Abstraction
{
	/// <summary>
	/// Represent endpoint abstraction.
	/// </summary>
	public interface IEndpoint
	{
		/// <summary>
		/// Map endpoint using routerBuilder.
		/// </summary>
		/// <param name="routerBuilder">Router builder.</param>
		void MapEndpoint(IEndpointRouteBuilder routerBuilder);
	}
}
