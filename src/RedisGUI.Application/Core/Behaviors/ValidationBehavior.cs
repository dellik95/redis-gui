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

		var failures = await DispatchWeave.Results.RequestValidation.Validate(request, validators, cancellationToken);
		var validationErrors = failures
			.Select(error => new ValidationError(error.PropertyName, error.ErrorMessage))
			.ToList();

		if (validationErrors.Count != 0)
		{
			throw new ValidationException(validationErrors);
		}

		return await next();
	}
}
