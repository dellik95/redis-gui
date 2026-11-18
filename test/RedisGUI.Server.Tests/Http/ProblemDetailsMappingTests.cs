using System.IO;
using System.Collections.Generic;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.AspNetCore.Hosting;
using Moq;
using RedisGUI.Domain.Exceptions;
using RedisGUI.Server.Extensions;
using RedisGUI.Server.Middleware;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Server.Tests.Http;

[TestFixture]
public class ProblemDetailsMappingTests
{
	[TestCase("Connection.NotFound", 404)]
	[TestCase("Redis.KeyNotFound", 404)]
	[TestCase("Connection.Forbidden", 403)]
	[TestCase("Connection.ReadOnlyMode", 403)]
	[TestCase("User.Deactivated", 403)]
	[TestCase("Job.Forbidden", 403)]
	[TestCase("User.InvalidCredentials", 401)]
	[TestCase("ConnectionManager.Cancelled", 408)]
	[TestCase("Storage.ConcurrencyConflict", 409)]
	[TestCase("Storage.KeyAlreadyExists", 409)]
	[TestCase("Redis.ScanFailed", 502)]
	[TestCase("Redis.DeleteByPatternFailed", 502)]
	[TestCase("Export.CaptureFailed", 502)]
	[TestCase("Import.RestoreFailed", 502)]
	[TestCase("Config.UpdateFailed", 502)]
	[TestCase("Redis.ScanUnavailable", 503)]
	[TestCase("Connection.NotEstablished", 503)]
	[TestCase("Metrics.ServiceUnavailable", 503)]
	[TestCase("ConnectionManager.UnexpectedError", 500)]
	[TestCase("Handler.ExecutionError", 500)]
	[TestCase("MetricsCollection.Exception", 500)]
	[TestCase("Redis.InvalidScan", 400)]
	[TestCase("Import.ParseError", 400)]
	[TestCase("Cli.EmptyCommand", 400)]
	[TestCase("Redis.UnsupportedCommand", 400)]
	public async Task FailedResult_ProducesProblemDetails(string code, int status)
	{
		await using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = services };
		context.Response.Body = new MemoryStream();
		var result = Result.Failure<string>(new Error(code, "Example error"));
		await result.ToHttpResult(context).ExecuteAsync(context);

		Assert.That(context.Response.StatusCode, Is.EqualTo(status));
		Assert.That(context.Response.ContentType, Does.StartWith("application/problem+json"));
		context.Response.Body.Position = 0;
		using var json = await JsonDocument.ParseAsync(context.Response.Body);
		Assert.That(json.RootElement.GetProperty("status").GetInt32(), Is.EqualTo(status));
		Assert.That(json.RootElement.GetProperty("detail").GetString(), Is.EqualTo("Example error"));
		Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo(code));
		Assert.That(json.RootElement.GetProperty("message").GetString(), Is.EqualTo("Example error"));
		Assert.That(json.RootElement.GetProperty("statusCode").GetInt32(), Is.EqualTo(status));
	}

	[Test]
	public async Task ErrorException_UsesTheSameStatusAndProblemDetails()
	{
		await using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = services };
		context.Response.Body = new MemoryStream();
		var environment = new Mock<IWebHostEnvironment>();
		environment.SetupGet(x => x.EnvironmentName).Returns("Production");
		var middleware = new GlobalExceptionHandlerMiddleware(
			_ => throw new ErrorException(new Error("Connection.Forbidden", "Denied")),
			NullLogger<GlobalExceptionHandlerMiddleware>.Instance, environment.Object);

		await middleware.InvokeAsync(context);

		Assert.That(context.Response.StatusCode, Is.EqualTo(403));
		Assert.That(context.Response.ContentType, Does.StartWith("application/problem+json"));
		context.Response.Body.Position = 0;
		using var json = await JsonDocument.ParseAsync(context.Response.Body);
		Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("Connection.Forbidden"));
	}

	[Test]
	public async Task ValidationFailure_ReturnsFieldErrors()
	{
		await using var services = new ServiceCollection().AddLogging().AddProblemDetails().BuildServiceProvider();
		var context = new DefaultHttpContext { RequestServices = services };
		context.Response.Body = new MemoryStream();
		var error = new ValidationFailureError(new Dictionary<string, string[]> { ["Host"] = ["Host is required"] });
		await Result.Failure<string>(error).ToHttpResult(context).ExecuteAsync(context);
		Assert.That(context.Response.StatusCode, Is.EqualTo(400));
		context.Response.Body.Position = 0;
		using var json = await JsonDocument.ParseAsync(context.Response.Body);
		Assert.That(json.RootElement.GetProperty("errors").GetProperty("Host")[0].GetString(), Is.EqualTo("Host is required"));
		Assert.That(json.RootElement.GetProperty("code").GetString(), Is.EqualTo("Validation.Failed"));
	}

	[Test]
	public void SuccessfulResult_PreservesValue()
	{
		var result = Result.Success("value").ToHttpResult();
		Assert.That(((IStatusCodeHttpResult)result).StatusCode, Is.EqualTo(200));
		Assert.That(((IValueHttpResult)result).Value, Is.EqualTo("value"));
	}
}
