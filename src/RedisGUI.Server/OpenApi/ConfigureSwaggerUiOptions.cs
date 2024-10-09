using Asp.Versioning.ApiExplorer;
using Microsoft.Extensions.Options;
using Swashbuckle.AspNetCore.SwaggerUI;

namespace RedisGUI.Server.OpenApi;

public class ConfigureSwaggerUiOptions : IConfigureNamedOptions<SwaggerUIOptions>
{
	private readonly IApiVersionDescriptionProvider _descriptionProvider;

	public ConfigureSwaggerUiOptions(IApiVersionDescriptionProvider descriptionProvider)
	{
		_descriptionProvider = descriptionProvider;
	}
	public void Configure(SwaggerUIOptions options)
	{
		foreach (var description in this._descriptionProvider.ApiVersionDescriptions)
		{
			var url = $"/swagger/{description.GroupName}/swagger.json";
			var name = description.GroupName.ToUpperInvariant();
			options.SwaggerEndpoint(url, name);
		}
	}

	public void Configure(string? name, SwaggerUIOptions options)
	{
		this.Configure(options);
	}
}