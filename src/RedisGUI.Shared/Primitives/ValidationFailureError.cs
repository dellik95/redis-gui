namespace RedisGUI.Shared.Primitives;

/// <summary>Validation errors grouped by request property.</summary>
public sealed record ValidationFailureError(IReadOnlyDictionary<string, string[]> Errors)
	: Error("Validation.Failed", "One or more validation errors occurred.");

/// <summary>An exception that exposes an application error to the mediator.</summary>
public interface IResultErrorException
{
	Error Error { get; }
}
