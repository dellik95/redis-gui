using FluentValidation;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Core.Extensions;

public static class FluentValidationExtensions
{
	public static IRuleBuilder<T, TProperty> WithError<T, TProperty>(this IRuleBuilderOptions<T, TProperty> builder,
		Error error)
	{
		if (error is null)
		{
			throw new ArgumentNullException(nameof(error), "Could not be null.");
		}

		return builder.WithErrorCode(error.Code).WithMessage(error.Message);
	}
}