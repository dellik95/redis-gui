using Asp.Versioning.ApiExplorer;
using Microsoft.AspNetCore.Builder;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace RedisGUI.Server.OpenApi;

/// <summary>
/// Configures Swagger UI options for API versioning
/// </summary>
public class ConfigureSwaggerUiOptions : IConfigureNamedOptions<SwaggerUIOptions>
{
	/// <summary>
	/// The provider for API version descriptions
	/// </summary>
	private readonly IApiVersionDescriptionProvider apiVersionDescriptionProvider;

	/// <summary>
	/// Initializes a new instance of the ConfigureSwaggerUiOptions class
	/// </summary>
	/// <param name="apiVersionDescriptionProvider">The provider for API version descriptions</param>
	public ConfigureSwaggerUiOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
	{
		this.apiVersionDescriptionProvider = apiVersionDescriptionProvider;
	}

	/// <summary>
	/// Configures the Swagger UI options
	/// </summary>
	/// <param name="options">The options to configure</param>
	public void Configure(SwaggerUIOptions options)
	{
		foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
		{
			var url = $"/swagger/{description.GroupName}/swagger.json";
			var name = description.GroupName.ToUpperInvariant();
			options.SwaggerEndpoint(url, name);
		}
	}

	/// <summary>
	/// Configures the Swagger UI options with a specific name
	/// </summary>
	/// <param name="name">The name of the options to configure</param>
	/// <param name="options">The options to configure</param>
	public void Configure(string name, SwaggerUIOptions options)
	{
		Configure(options);
	}
}
