using System;
using System.Diagnostics.CodeAnalysis;



namespace RedisGUI.Domain.Primitives;

/// <summary>
///     Represent Success/Failed result.
/// </summary>
public class Result
{
	/// <summary>
	///     Create new instance of <see cref="Result" />.
	/// </summary>
	/// <param name="isSuccess">Indicates whether result is successful</param>
	/// <param name="error">Error</param>
	/// <exception cref="InvalidOperationException"></exception>
	protected internal Result(bool isSuccess, Error error)
	{
		if (isSuccess && error != Error.None)
		{
			throw new InvalidOperationException();
		}

		if (!isSuccess && error == Error.None)
		{
			throw new InvalidOperationException();
		}

		IsSuccess = isSuccess;
		Error = error;
	}

	/// <summary>
	///     Indicates whether result is Failure
	/// </summary>
	public bool IsFailure => !IsSuccess;

	/// <summary>
	///     Indicates whether result is successful
	/// </summary>
	public bool IsSuccess { get; }

	/// <summary>
	///     Contains error of result
	/// </summary>
	public Error Error { get; }

	/// <summary>
	///     Create successful result
	/// </summary>
	public static Result Success()
	{
		return new Result(true, Error.None);
	}

	/// <summary>
	///     Create failed result
	/// </summary>
	/// <param name="error">Error</param>
	public static Result Failure(Error error)
	{
		return new Result(false, error);
	}

	/// <summary>
	///     Create successful result with value
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	public static Result<TValue> Success<TValue>(TValue value)
	{
		return new Result<TValue>(value, true, Error.None);
	}

	/// <summary>
	///     Create failed result with default value
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	public static Result<TValue> Failure<TValue>(Error error)
	{
		return new Result<TValue>(default, false, error);
	}

	/// <summary>
	///     CCreate success result if value not null otherwise failure
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	/// <param name="value">Value</param>
	/// <param name="error">Error</param>
	public static Result<TValue> Create<TValue>(TValue value, Error error)
	{
		return value is null ? Failure<TValue>(error) : Success(value);
	}

	/// <summary>
	///     CCreate success result if value not null otherwise failure
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	/// <param name="value">Value</param>
	public static Result<TValue> Create<TValue>(TValue value)
	{
		return value is null ? Failure<TValue>(Error.NullValue) : Success(value);
	}
}

/// <summary>
///     Represent Success/Failed result with value
/// </summary>
public class Result<TValue> : Result
{
	private readonly TValue value;

	/// <summary>
	///     Create new instance of <see cref="Result{TValue}" />
	/// </summary>
	/// <param name="value"></param>
	/// <param name="isSuccess"></param>
	/// <param name="error"></param>
	protected internal Result(TValue value, bool isSuccess, Error error) : base(isSuccess, error)
	{
		this.value = value;
	}

	/// <summary>
	///     Return value from result
	/// </summary>
	public TValue Value => (IsSuccess
		? value
		: throw new InvalidOperationException("The value of a failure result can not be accessed."))!;


	/// <summary>
	///     Create result from value
	/// </summary>
	/// <param name="value">Value of result</param>
	public static implicit operator Result<TValue>(TValue value)
	{
		return Create(value);
	}

	/// <summary>
	///     Return value from result
	/// </summary>
	/// <param name="result">Result</param>
	public static implicit operator TValue(Result<TValue> result)
	{
		return result.Value;
	}
}
