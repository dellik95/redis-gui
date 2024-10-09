using RedisGUI.Domain.Extensions;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Domain.Exceptions;

public class ErrorException : Exception
{
	private readonly List<Error> _errors = new List<Error>();

	public ErrorException(Error error) : base($"An error occured: {error.Code}")
	{
		this._errors.Add(error);
	}

	public ErrorException(params Error[] errors) : base($"An errors occured: {errors.ToCodeString()}")
	{
		this._errors = errors.ToList();
	}

	public override string ToString() => this._errors.ToFormattedMessage();
}