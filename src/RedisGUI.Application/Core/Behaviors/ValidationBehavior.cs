using FluentValidation;
using RedisGUI.Application.Exceptions;
using RedisGUI.Shared.Mediator;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = RedisGUI.Application.Exceptions.ValidationException;

namespace RedisGUI.Application.Core.Behaviors;

/// <summary>
/// Mediator pipeline behavior that validates requests before handler execution.
/// </summary>
public class ValidationBehavior<TRequest, TResponse>(IEnumerable<IValidator<TRequest>> validators)
	: IPipelineBehavior<TRequest, TResponse>
	where TRequest : IRequest<TResponse>
{
	private readonly IReadOnlyList<IValidator<TRequest>> validators = validators as IReadOnlyList<IValidator<TRequest>> ?? validators.ToArray();

	public async Task<TResponse> Handle(TRequest request, CancellationToken cancellationToken, Func<Task<TResponse>> next)
	{
		if (validators.Count == 0)
		{
			return await next();
		}

		var context = new ValidationContext<TRequest>(request);
		var validationResults = await Task.WhenAll(
			validators.Select(validator => validator.ValidateAsync(context, cancellationToken)));

		var validationErrors = validationResults
			.SelectMany(result => result.Errors)
			.Where(error => error is not null)
			.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
			.ToList();

		if (validationErrors.Count != 0)
		{
			throw new ValidationException(validationErrors);
		}

		return await next();
	}
}
