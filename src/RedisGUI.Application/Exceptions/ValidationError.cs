namespace RedisGUI.Application.Exceptions;

/// <summary>
/// Represents a validation error with property name and error message.
/// </summary>
/// <param name="PropertyName">The name of the property that failed validation.</param>
/// <param name="ErrorMessage">The error message describing the validation failure.</param>
public record ValidationError(string PropertyName, string ErrorMessage);
