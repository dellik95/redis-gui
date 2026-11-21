using DispatchWeave;
using DispatchWeave.Results;
using FluentResults;
using FluentAssertions;
using Moq;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Users.Commands.RegisterUser;
using RedisGUI.Application.Redis.Commands.DeleteMultipleKeys;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Core.Behaviors;

[TestFixture]
public class AuditingBehaviorTests
{
	private Mock<IAuditLogRepository> auditLogRepositoryMock = null!;
	private Mock<IUnitOfWork> unitOfWorkMock = null!;
	private Mock<ICurrentUserService> currentUserServiceMock = null!;
	private AuditingBehavior<RegisterUserCommand, Result<Guid>> behavior = null!;

	[SetUp]
	public void Setup()
	{
		auditLogRepositoryMock = new Mock<IAuditLogRepository>();
		unitOfWorkMock = new Mock<IUnitOfWork>();
		currentUserServiceMock = new Mock<ICurrentUserService>();
		behavior = new AuditingBehavior<RegisterUserCommand, Result<Guid>>(
			auditLogRepositoryMock.Object,
			unitOfWorkMock.Object,
			currentUserServiceMock.Object);
	}

	[Test]
	public async Task Handle_RequestWithoutAuditableAttribute_ShouldNotAudit()
	{
		AuditingBehavior<TestRequestWithoutAttribute, Result<Guid>> testBehavior = new(
			auditLogRepositoryMock.Object,
			unitOfWorkMock.Object,
			currentUserServiceMock.Object);
		TestRequestWithoutAttribute request = new();
		Result<Guid> response = Result.Ok(Guid.NewGuid());

		Result<Guid> result = await testBehavior.Handle(
			request,
			async () => await Task.FromResult(response),
			CancellationToken.None);

		result.Should().Be(response);
		auditLogRepositoryMock.Verify(x => x.Add(It.IsAny<AuditLog>()), Times.Never);
	}

	[Test]
	public async Task Handle_SuccessfulRequest_ShouldCreateAuditLog()
	{
		Guid userId = Guid.NewGuid();
		Guid entityId = Guid.NewGuid();
		RegisterUserCommand request = new("test@example.com", "Password123!", "John", "Doe");
		Result<Guid> response = Result.Ok(entityId);

		currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
		currentUserServiceMock.Setup(x => x.IpAddress).Returns("127.0.0.1");
		currentUserServiceMock.Setup(x => x.UserAgent).Returns("Mozilla/5.0");

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(1);

		Result<Guid> result = await behavior.Handle(
			request,
			async () => await Task.FromResult(response),
			CancellationToken.None);

		result.Should().Be(response);
		auditLogRepositoryMock.Verify(x => x.Add(It.Is<AuditLog>(log =>
			log.UserId == userId &&
			log.Action == AuditAction.Create &&
			log.EntityId == entityId
		)), Times.Once);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_FailedRequest_ShouldNotCreateAuditLog()
	{
		RegisterUserCommand request = new("test@example.com", "Password123!", "John", "Doe");
		Result<Guid> response = Result.Fail<Guid>(DomainErrors.User.EmailAlreadyExists);

		Result<Guid> result = await behavior.Handle(
			request,
			async () => await Task.FromResult(response),
			CancellationToken.None);

		result.Should().Be(response);
		auditLogRepositoryMock.Verify(x => x.Add(It.IsAny<AuditLog>()), Times.Never);
	}

	[Test]
	public async Task Handle_BulkDelete_RecordsConnectionAndActualOutcome()
	{
		var pipeline = new AuditingBehavior<DeleteMultipleKeysCommand, Result<DeleteMultipleKeysResponse>>(
			auditLogRepositoryMock.Object, unitOfWorkMock.Object, currentUserServiceMock.Object);
		var connectionId = Guid.NewGuid();
		var command = new DeleteMultipleKeysCommand(connectionId, ["a", "b"]);
		var response = Result.Ok(new DeleteMultipleKeysResponse(1, 2, ["b"]));
		await pipeline.Handle(command, () => Task.FromResult(response), CancellationToken.None);
		auditLogRepositoryMock.Verify(x => x.Add(It.Is<AuditLog>(log =>
			log.EntityId == connectionId && log.Metadata.NewValue == "Deleted 1 of 2; failed or missing: 1.")), Times.Once);
	}

	[Test]
	public async Task Handle_InterfaceRequest_AuditsTheConcreteCommand()
	{
		var pipeline = new AuditingBehavior<ICommand<Guid>, Result<Guid>>(
			auditLogRepositoryMock.Object, unitOfWorkMock.Object, currentUserServiceMock.Object);
		var id = Guid.NewGuid();
		ICommand<Guid> request = new RegisterUserCommand("test@example.com", "Password123!", "John", "Doe");
		await pipeline.Handle(request, () => Task.FromResult(Result.Ok(id)), CancellationToken.None);
		auditLogRepositoryMock.Verify(x => x.Add(It.Is<AuditLog>(log => log.Action == AuditAction.Create && log.EntityId == id)), Times.Once);
	}

	private class TestRequestWithoutAttribute : IRequest<Result<Guid>>
	{
	}
}
