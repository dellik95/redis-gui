using FluentValidation;
using System.Linq;

namespace RedisGUI.Application.Redis.Commands.BulkUpdateTtl;

/// <summary>Validates the bounded set of keys and requested expiration.</summary>
public sealed class BulkUpdateTtlCommandValidator : AbstractValidator<BulkUpdateTtlCommand>
{
	public BulkUpdateTtlCommandValidator()
	{
		RuleFor(x => x.ConnectionId).NotEmpty();
		RuleFor(x => x.Database).InclusiveBetween(0, 15).When(x => x.Database.HasValue);
		RuleFor(x => x.Keys).NotNull().Must(keys => keys is not null && keys.Take(1001).Count() is > 0 and <= 1000)
			.WithMessage("Provide between 1 and 1000 keys.");
		RuleForEach(x => x.Keys).NotEmpty().MaximumLength(512)
			.When(x => x.KeysBase64 is null || x.KeysBase64.Count == 0);
		RuleFor(x => x.KeysBase64)
			.Must((cmd, b64) => cmd.Keys is not null && b64!.Count == cmd.Keys.Count())
			.When(x => x.KeysBase64 is not null && x.KeysBase64.Count > 0)
			.WithMessage("KeysBase64 count must match Keys count.");
		RuleFor(x => x.TtlSeconds).InclusiveBetween(1, 922337203685).When(x => x.TtlSeconds.HasValue)
			.WithMessage("TTL must be positive and fit in a TimeSpan, or null to persist.");
	}
}
