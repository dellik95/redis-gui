using FluentValidation;
using MediatR;
using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Application.Exceptions;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using ValidationException = RedisGUI.Application.Exceptions.ValidationException;

namespace RedisGUI.Application.Core.Behavior;

/// <summary>
/// MediatR pipeline behavior that performs validation on commands before they are handled
/// </summary>
/// <typeparam name="TRequest">The type of request being validated</typeparam>
/// <typeparam name="TResponse">The type of response from the handler</typeparam>
public class ValidationBehavior<TRequest, TResponse> : IPipelineBehavior<TRequest, TResponse>
	where TRequest : IBaseCommand
{
	/// <summary>
	/// Collection of validators that can validate the request type
	/// </summary>
	private readonly IEnumerable<IValidator<TRequest>> validators;

	/// <summary>
	/// Initializes a new instance of the ValidationBehavior class
	/// </summary>
	/// <param name="validators">The collection of validators for the request type</param>
	public ValidationBehavior(IEnumerable<IValidator<TRequest>> validators)
	{
		this.validators = validators;
	}

	/// <summary>
	/// Handles the request by performing validation before passing to the next handler
	/// </summary>
	/// <param name="request">The request being handled</param>
	/// <param name="next">The delegate for the next action in the pipeline</param>
	/// <param name="cancellationToken">Cancellation token</param>
	/// <returns>The response from the handler</returns>
	/// <exception cref="ValidationException">Thrown when validation fails</exception>
	public async Task<TResponse> Handle(TRequest request, RequestHandlerDelegate<TResponse> next, CancellationToken cancellationToken)
	{
		if (!validators.Any())
		{
			return await next();
		}

		var context = new ValidationContext<TRequest>(request);

		var validationErrors = validators
			.Select(x => x.Validate(context))
			.Where(x => x.Errors.Count != 0)
			.SelectMany(x => x.Errors)
			.Select(x => new ValidationError(x.PropertyName, x.ErrorMessage))
			.ToList();

		if (validationErrors.Count != 0)
		{
			throw new ValidationException(validationErrors);
		}

		return await next();
	}
}
