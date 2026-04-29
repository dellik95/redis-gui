using Microsoft.EntityFrameworkCore.Diagnostics;
using RedisGUI.Shared.Mediator;
using System.Linq;

namespace RedisGUI.Infrastructure.Persistence.Interceptors;

/// <summary>
/// EF Core interceptor that automatically dispatches domain events after successful save operations
/// </summary>
public sealed class DomainEventsDispatcherInterceptor : SaveChangesInterceptor
{
	private readonly IMediator mediator;
	private readonly ILogger<DomainEventsDispatcherInterceptor> logger;

	/// <summary>
	/// Initializes a new instance of the DomainEventsDispatcherInterceptor
	/// </summary>
	/// <param name="mediator">The mediator for publishing domain events</param>
	/// <param name="logger">Logger instance</param>
	public DomainEventsDispatcherInterceptor(
		IMediator mediator,
		ILogger<DomainEventsDispatcherInterceptor> logger)
	{
		this.mediator = mediator;
		this.logger = logger;
	}

	/// <summary>
	/// Intercepts the saving changes operation to dispatch domain events
	/// </summary>
	public override async ValueTask<InterceptionResult<int>> SavingChangesAsync(
		DbContextEventData eventData,
		InterceptionResult<int> result,
		CancellationToken cancellationToken = default)
	{
		DbContext? dbContext = eventData.Context;
		if (dbContext is null)
		{
			return await base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		// Collect all entities with domain events
		List<Entity> entities = dbContext.ChangeTracker
			.Entries<Entity>()
			.Where(e => e.Entity.GetDomainEvents().Count > 0)
			.Select(e => e.Entity)
			.ToList();

		if (entities.Count == 0)
		{
			return await base.SavingChangesAsync(eventData, result, cancellationToken);
		}

		// Extract all domain events before clearing
		List<IDomainEvent> domainEvents = entities
			.SelectMany(e => e.GetDomainEvents())
			.ToList();

		// Clear events from entities before saving
		foreach (Entity? entity in entities)
		{
			entity.ClearDomainEvents();
		}

		// Save changes first
		InterceptionResult<int> saveResult = await base.SavingChangesAsync(eventData, result, cancellationToken);

		// Dispatch events after successful save
		if (!(saveResult.Result <= 0))
		{
			logger.LogInformation(
				"Dispatching {Count} domain event(s) after successful save",
				domainEvents.Count);

			foreach (IDomainEvent? domainEvent in domainEvents)
			{
				try
				{
					await mediator.Publish(domainEvent, cancellationToken);
					logger.LogDebug(
						"Dispatched domain event: {EventType}",
						domainEvent.GetType().Name);
				}
				catch (Exception ex)
				{
					logger.LogError(
						ex,
						"Error dispatching domain event: {EventType}",
						domainEvent.GetType().Name);
					// Continue dispatching other events even if one fails
				}
			}
		}

		return saveResult;
	}
}

