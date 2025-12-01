using FluentValidation;

namespace RedisGUI.Application.Redis.Commands.ImportRedisData;

public class ImportRedisDataCommandValidator : AbstractValidator<ImportRedisDataCommand>
{
    private const int MaxDataSize = 10 * 1024 * 1024; // 10MB

    public ImportRedisDataCommandValidator()
    {
        RuleFor(x => x.ConnectionId)
            .NotEmpty()
            .WithMessage("Connection ID is required");

        RuleFor(x => x.Data)
            .NotEmpty()
            .WithMessage("Data is required")
            .Must(data => data == null || data.Length <= MaxDataSize)
            .WithMessage($"Data size cannot exceed {MaxDataSize / (1024 * 1024)} MB");
    }
}
