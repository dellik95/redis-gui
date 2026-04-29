using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

/// <summary>
/// Validator for ImportRedisDataCommand.
/// </summary>
public sealed class ImportRedisDataCommandValidation : AbstractValidator<ImportRedisDataCommand>
{
	/// <summary>
	/// Initializes a new instance of the ImportRedisDataCommandValidation class.
	/// </summary>
	public ImportRedisDataCommandValidation()
	{
		RuleFor(x => x.ConnectionId)
			.NotEmpty()
			.WithMessage("Connection ID is required.");

		RuleFor(x => x.Data)
			.NotEmpty()
			.WithMessage("Import data is required.")
			.MaximumLength(10 * 1024 * 1024) // 10MB max
			.WithMessage("Import data must not exceed 10MB.");

		RuleFor(x => x.Format)
			.IsInEnum()
			.WithMessage("Invalid import format.");
	}
}
