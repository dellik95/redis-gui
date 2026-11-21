namespace RedisGUI.Shared.Primitives;

public sealed class ValidationFailureError(IReadOnlyDictionary<string, string[]> errors)
    : DomainError("Validation.Failed", "One or more validation errors occurred.")
{
    public IReadOnlyDictionary<string, string[]> Errors { get; } = errors;
}
