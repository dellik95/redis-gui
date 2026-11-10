using FluentValidation;

namespace RedisGUI.Application.Redis.Queries.ExportRedisData;

/// <summary>
/// Validator for ExportRedisDataQuery.
/// </summary>
public sealed class ExportRedisDataQueryValidation : AbstractValidator<ExportRedisDataQuery>
{
	/// <summary>
	/// Initializes a new instance of the ExportRedisDataQueryValidation class.
	/// </summary>
	public ExportRedisDataQueryValidation()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required.");

		RuleFor(x => x.Pattern)
			.NotEmpty()
			.WithMessage("Pattern is required.")
			.MaximumLength(1024)
			.WithMessage("Pattern must not exceed 1024 characters.");

		RuleFor(x => x.Format)
			.IsInEnum()
			.WithMessage("Invalid export format.");
	}
}
