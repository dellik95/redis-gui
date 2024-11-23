using System;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Application.Abstraction.Messaging;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Connection;
using RedisGUI.Domain.Primitives;

namespace RedisGUI.Application.Connections.DeleteConnection;

/// <summary>
/// Handles redis connection deletion 
/// </summary>
internal class DeleteConnectionCommandHandler : ICommandHandler<DeleteConnectionCommand>
{
	private readonly IRedisConnectionRepository connectionRepository;
	private readonly IUnitOfWork unitOfWork;

	/// <summary>
	/// Handles redis connection deletion 
	/// </summary>
	/// <param name="connectionRepository">Repository for Redis connections</param>
	/// <param name="unitOfWork">Unit of work for transaction management</param>
	public DeleteConnectionCommandHandler(IRedisConnectionRepository connectionRepository, IUnitOfWork unitOfWork)
	{
		this.connectionRepository = connectionRepository;
		this.unitOfWork = unitOfWork;
	}
	public async Task<Result> Handle(DeleteConnectionCommand command, CancellationToken cancellationToken)
	{
		var result = await this.connectionRepository.DeleteAsync(command.Id, cancellationToken); ;

		if (result.IsSuccess)
		{
			await this.unitOfWork.SaveChangesAsync(cancellationToken);
		}

		return result;
	}
}
