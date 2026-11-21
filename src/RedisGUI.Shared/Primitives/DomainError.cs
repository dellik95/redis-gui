using FluentResults;

namespace RedisGUI.Shared.Primitives;

public class DomainError : FluentResults.Error, IEquatable<DomainError>
{
    public DomainError(string code, string message) : base(message)
    {
        Code = code;
        WithMetadata("Code", code);
    }

    public string Code { get; }

    public static readonly DomainError None = new("", "");
    public static readonly DomainError NullValue = new("Error.NullValue", "Null value was provided");
    public static implicit operator DomainError(string message) => new("Error.General", message);
    public static implicit operator DomainError(Exception exception) => new("Error.General", exception.Message);

    public bool Equals(DomainError? other) =>
        other is not null && GetType() == other.GetType() && Code == other.Code && Message == other.Message;

    public override bool Equals(object? other) => other is DomainError error && Equals(error);
    public override int GetHashCode() => HashCode.Combine(GetType(), Code, Message);
    public override string ToString() => Message;
}

public static class DomainErrorExtensions
{
    public static DomainError GetDomainError(this IResultBase result)
    {
        var error = result.Errors[0];
        if (error is DomainError domainError)
            return domainError;

        if (result.Errors.Any(static e => e.Metadata.ContainsKey("PropertyName")))
        {
            var validationErrors = result.Errors
                .GroupBy(static e => e.Metadata.GetValueOrDefault("PropertyName")?.ToString() ?? string.Empty)
                .ToDictionary(static g => g.Key, static g => g.Select(static e => e.Message).ToArray());
            return new ValidationFailureError(validationErrors);
        }

        var code = error.Metadata.GetValueOrDefault("Code")?.ToString()
            ?? error.Metadata.GetValueOrDefault("ErrorCode")?.ToString() ?? "Error.General";
        return new DomainError(code, error.Message);
    }
}
