namespace RedisGUI.Application.Exceptions;

public class ValidationException : Exception
{
	public IEnumerable<ValidationError> Errors { get; init; }

	public ValidationException(IEnumerable<ValidationError> errors)
	{
		Errors = errors;
	}
}