using FluentResults;
using RedisGUI.Application.Core.Services;
using FluentValidation;
using RedisGUI.Domain.Connection;
using DispatchWeave;
using DispatchWeave.Results;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Redis.Queries.ScanRedisKeys;

public sealed record ScanRedisKeysQuery(Guid ConnectionId, string Pattern, int? Database, string? Cursor, int Count) : IQuery<RedisKeyScanPage>;

internal sealed class ScanRedisKeysQueryValidator : AbstractValidator<ScanRedisKeysQuery>
{
	public ScanRedisKeysQueryValidator()
	{
		RuleFor(request => request.Pattern).NotNull().NotEqual(string.Empty).MaximumLength(1024);
		RuleFor(request => request.Database).InclusiveBetween(0, 15).When(request => request.Database.HasValue);
		RuleFor(request => request.Cursor).MaximumLength(8192);
		RuleFor(request => request.Count).InclusiveBetween(1, 500);
	}
}

internal sealed class ScanRedisKeysQueryHandler(IRedisConnectionRepository repository, IConnectionAccessService access, IRedisKeyService keys)
	: IQueryHandler<ScanRedisKeysQuery, RedisKeyScanPage>
{
	public async Task<Result<RedisKeyScanPage>> Handle(ScanRedisKeysQuery request, CancellationToken cancellationToken)
	{
		return await (await repository.GetConnectionByIdAsync(request.ConnectionId, cancellationToken))
			.BindAsync(async connection => await access.CanAccessAsync(connection)
				? await keys.ScanKeysAsync(connection, request.Pattern, request.Database ?? connection.DatabaseNumber.Value,
					request.Cursor, request.Count, cancellationToken)
				: Result.Fail<RedisKeyScanPage>(new DomainError("Connection.Forbidden", "Access to the connection is denied.")));
	}
}
