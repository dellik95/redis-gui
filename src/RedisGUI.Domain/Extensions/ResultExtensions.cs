using RedisGUI.Domain.Primitives;
using System;
using System.Threading.Tasks;


namespace RedisGUI.Domain.Extensions;

/// <summary>
/// Represent extensions for <see cref="Result"/>
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Ensures that the result value satisfies a given predicate
	/// </summary>
	/// <param name="result">The result to check</param>
	/// <param name="predicate">The predicate that the value must satisfy</param>
	/// <param name="error">The error to return if the predicate is not satisfied</param>
	/// <typeparam name="T">The type of the result value</typeparam>
	/// <returns>The original result if successful and predicate is satisfied, otherwise a failure result</returns>
	public static Result<T> Ensure<T>(this Result<T> result, Func<T, bool> predicate, Error error)
	{
		if (result.IsFailure)
		{
			return result;
		}

		return result.IsSuccess && predicate(result.Value) ? result : Result.Failure<T>(error);
	}

	/// <summary>
	/// Convert result if successful
	/// </summary>
	/// <param name="result">Result</param>
	/// <param name="func">Map function</param>
	/// <typeparam name="TIn">Input type</typeparam>
	/// <typeparam name="TOut">Output type</typeparam>
	public static Result<TOut> Map<TIn, TOut>(this Result<TIn> result, Func<TIn, TOut> func)
	{
		return result.IsSuccess ? func(result.Value)! : Result.Failure<TOut>(result.Error);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <typeparam name="TIn"></typeparam>
	public static async Task<Result> Bind<TIn>(this Result<TIn> result, Func<TIn, Task<Result>> func)
	{
		return result.IsSuccess ? await func(result.Value) : Result.Failure(result.Error);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="result"></param>
	/// <param name="func"></param>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <returns></returns>
	public static async Task<Result<TOut>> Bind<TIn, TOut>(this Result<TIn> result, Func<TIn, Task<Result<TOut>>> func)
	{
		return result.IsSuccess ? await func(result.Value) : Result.Failure<TOut>(result.Error);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="resultTask"></param>
	/// <param name="onSuccess"></param>
	/// <param name="onFailure"></param>
	/// <typeparam name="T"></typeparam>
	/// <returns></returns>
	public static async Task<T> Match<T>(this Task<Result> resultTask, Func<T> onSuccess, Func<Error, T> onFailure)
	{
		var result = await resultTask;

		return result.IsSuccess ? onSuccess() : onFailure(result.Error);
	}

	/// <summary>
	/// 
	/// </summary>
	/// <param name="resultTask"></param>
	/// <param name="onSuccess"></param>
	/// <param name="onFailure"></param>
	/// <typeparam name="TIn"></typeparam>
	/// <typeparam name="TOut"></typeparam>
	/// <returns></returns>
	public static async Task<TOut> Match<TIn, TOut>(
		this Task<Result<TIn>> resultTask,
		Func<TIn, TOut> onSuccess,
		Func<Error, TOut> onFailure)
	{
		var result = await resultTask;

		return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
	}
}
