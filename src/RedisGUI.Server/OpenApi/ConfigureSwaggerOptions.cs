using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using Swashbuckle.AspNetCore.SwaggerGen;

namespace RedisGUI.Server.OpenApi;

public class ConfigureSwaggerOptions : IConfigureNamedOptions<SwaggerGenOptions>
{
	private readonly IApiVersionDescriptionProvider apiVersionDescriptionProvider;

	public ConfigureSwaggerOptions(IApiVersionDescriptionProvider apiVersionDescriptionProvider)
	{
		this.apiVersionDescriptionProvider = apiVersionDescriptionProvider;
	}


	public void Configure(SwaggerGenOptions options)
	{
		foreach (var description in apiVersionDescriptionProvider.ApiVersionDescriptions)
		{
			options.SwaggerDoc(description.GroupName, CreateVersionInfo(description));
		}
	}

	public void Configure(string? name, SwaggerGenOptions options)
	{
		this.Configure(options);
	}

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