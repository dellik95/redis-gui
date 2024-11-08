using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RedisGUI.Server.OpenApi;

/// <summary>
/// Configures Swagger generation options for API versioning
/// </summary>
public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
	/// <summary>
	/// The provider for API version descriptions
	/// </summary>
	private readonly IApiVersionDescriptionProvider apiVersionDescriptionProvider;

	/// <summary>
	/// Create new instance of <see cref="ConfigureSwaggerOptions" />
	/// </summary>
	/// <param name="apiVersionDescriptionProvider">Api version descriptor</param>
	public ConfigureSwaggerOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
	{
		this.apiVersionDescriptionProvider = apiVersionDescriptionProvider;
	}

	/// <summary>
	/// Configures the Swagger generation options
	/// </summary>
	/// <param name="options">The options to configure</param>
	public void Configure(SwaggerGenOptions options)
	{
		foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
		{
			options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
		}
	}

	/// <summary>
	/// Configures the Swagger generation options with a specific name
	/// </summary>
	/// <param name="name">The name of the options to configure</param>
	/// <param name="options">The options to configure</param>
	public void Configure(string name, SwaggerGenOptions options)
	{
		Configure(options);
	}

	/// <summary>
	/// Creates version information for the Swagger documentation
	/// </summary>
	/// <param name="apiVersionDescription">The API version description</param>
	/// <returns>An OpenApiInfo object containing the API version information</returns>
	private static OpenApiInfo CreateVersionInfo(ApiVersionDescription apiVersionDescription)
	{
		var openApiInfo = new OpenApiInfo
		{
			Title = $"Redis-Gui.Api v{apiVersionDescription.ApiVersion}",
			Version = apiVersionDescription.ApiVersion.ToString()
		};

		if (apiVersionDescription.IsDeprecated)
		{
			openApiInfo.Description += "This API version has been deprecated.";
		}

		return openApiInfo;
	}
}
