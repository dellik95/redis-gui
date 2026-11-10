using RedisGUI.Domain.Primitives;
using System.Threading.Tasks;

namespace RedisGUI.Shared.Primitives;

/// <summary>
/// Extension methods for the Result pattern to enable functional composition
/// </summary>
public static class ResultExtensions
{
	/// <summary>
	/// Maps a successful result to a new result type
	/// </summary>
	/// <typeparam name="TValue">Source value type</typeparam>
	/// <typeparam name="TNew">Target value type</typeparam>
	/// <param name="result">The result to map</param>
	/// <param name="mapper">The mapping function</param>
	/// <returns>A new result with the mapped value, or the original error</returns>
	public static Result<TNew> Map<TValue, TNew>(
		this Result<TValue> result,
		Func<TValue, TNew> mapper)
	{
		return result.IsSuccess
			? Result.Success(mapper(result.Value))
			: Result.Failure<TNew>(result.Error);
	}

	/// <summary>
	/// Maps a successful result to a new result type asynchronously
	/// </summary>
	/// <typeparam name="TValue">Source value type</typeparam>
	/// <typeparam name="TNew">Target value type</typeparam>
	/// <param name="resultTask">The result task to map</param>
	/// <param name="mapper">The async mapping function</param>
	/// <returns>A new result with the mapped value, or the original error</returns>
	public static async Task<Result<TNew>> MapAsync<TValue, TNew>(
		this Task<Result<TValue>> resultTask,
		Func<TValue, Task<TNew>> mapper)
	{
		Result<TValue> result = await resultTask;
		return result.IsSuccess
			? Result.Success(await mapper(result.Value))
			: Result.Failure<TNew>(result.Error);
	}

	/// <summary>
	/// Binds a successful result to a new result (monadic bind)
	/// </summary>
	/// <typeparam name="TValue">Source value type</typeparam>
	/// <typeparam name="TNew">Target value type</typeparam>
	/// <param name="result">The result to bind</param>
	/// <param name="binder">The binding function that returns a new result</param>
	/// <returns>A new result from the binder, or the original error</returns>
	public static Result<TNew> Bind<TValue, TNew>(
		this Result<TValue> result,
		Func<TValue, Result<TNew>> binder)
	{
		return result.IsSuccess
			? binder(result.Value)
			: Result.Failure<TNew>(result.Error);
	}

	/// <summary>
	/// Binds a successful result to a new result asynchronously (monadic bind)
	/// </summary>
	/// <typeparam name="TValue">Source value type</typeparam>
	/// <typeparam name="TNew">Target value type</typeparam>
	/// <param name="result">The result to bind</param>
	/// <param name="binder">The async binding function that returns a new result</param>
	/// <returns>A new result from the binder, or the original error</returns>
	public static async Task<Result<TNew>> BindAsync<TValue, TNew>(
		this Result<TValue> result,
		Func<TValue, Task<Result<TNew>>> binder)
	{
		return result.IsSuccess
			? await binder(result.Value)
			: Result.Failure<TNew>(result.Error);
	}

	/// <summary>
	/// Binds a successful result task to a new result asynchronously
	/// </summary>
	/// <typeparam name="TValue">Source value type</typeparam>
	/// <typeparam name="TNew">Target value type</typeparam>
	/// <param name="resultTask">The result task to bind</param>
	/// <param name="binder">The async binding function that returns a new result</param>
	/// <returns>A new result from the binder, or the original error</returns>
	public static async Task<Result<TNew>> BindAsync<TValue, TNew>(
		this Task<Result<TValue>> resultTask,
		Func<TValue, Task<Result<TNew>>> binder)
	{
		Result<TValue> result = await resultTask;
		return result.IsSuccess
			? await binder(result.Value)
			: Result.Failure<TNew>(result.Error);
	}

	/// <summary>
	/// Matches on the result, executing the appropriate function
	/// </summary>
	/// <typeparam name="T">Return type</typeparam>
	/// <param name="result">The result to match on</param>
	/// <param name="onSuccess">Function to execute on success</param>
	/// <param name="onFailure">Function to execute on failure</param>
	/// <returns>The result of the executed function</returns>
	public static T Match<T>(
		this Result result,
		Func<T> onSuccess,
		Func<Error, T> onFailure)
	{
		return result.IsSuccess ? onSuccess() : onFailure(result.Error);
	}

	/// <summary>
	/// Matches on the result with a value, executing the appropriate function
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	/// <typeparam name="T">Return type</typeparam>
	/// <param name="result">The result to match on</param>
	/// <param name="onSuccess">Function to execute on success with the value</param>
	/// <param name="onFailure">Function to execute on failure</param>
	/// <returns>The result of the executed function</returns>
	public static T Match<TValue, T>(
		this Result<TValue> result,
		Func<TValue, T> onSuccess,
		Func<Error, T> onFailure)
	{
		return result.IsSuccess ? onSuccess(result.Value) : onFailure(result.Error);
	}

	/// <summary>
	/// Executes an action if the result is successful
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	/// <param name="result">The result</param>
	/// <param name="action">The action to execute</param>
	/// <returns>The original result</returns>
	public static Result<TValue> Tap<TValue>(
		this Result<TValue> result,
		Action<TValue> action)
	{
		if (result.IsSuccess)
		{
			action(result.Value);
		}

		return result;
	}

	/// <summary>
	/// Executes an async action if the result is successful
	/// </summary>
	/// <typeparam name="TValue">Value type</typeparam>
	/// <param name="result">The result</param>
	/// <param name="action">The async action to execute</param>
	/// <returns>The original result</returns>
	public static async Task<Result<TValue>> TapAsync<TValue>(
		this Result<TValue> result,
		Func<TValue, Task> action)
	{
		if (result.IsSuccess)
		{
			await action(result.Value);
		}

		return result;
	}
}

