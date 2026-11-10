using FluentAssertions;
using Moq;
using RedisGUI.Application.Core.Behaviors;
using RedisGUI.Application.Core.Services;
using RedisGUI.Application.Users.Commands.RegisterUser;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Audit;
using RedisGUI.Domain.Errors;
using RedisGUI.Shared.Mediator;
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
		// Arrange
		AuditingBehavior<TestRequestWithoutAttribute, Result<Guid>> testBehavior = new(
			auditLogRepositoryMock.Object,
			unitOfWorkMock.Object,
			currentUserServiceMock.Object);
		TestRequestWithoutAttribute request = new();
		Result<Guid> response = Result.Success(Guid.NewGuid());

		// Act
		Result<Guid> result = await testBehavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(response));

		// Assert
		result.Should().Be(response);
		auditLogRepositoryMock.Verify(x => x.Add(It.IsAny<AuditLog>()), Times.Never);
	}

	[Test]
	public async Task Handle_SuccessfulRequest_ShouldCreateAuditLog()
	{
		// Arrange
		Guid userId = Guid.NewGuid();
		Guid entityId = Guid.NewGuid();
		RegisterUserCommand request = new("test@example.com", "Password123!", "John", "Doe");
		Result<Guid> response = Result.Success(entityId);

		currentUserServiceMock.Setup(x => x.UserId).Returns(userId);
		currentUserServiceMock.Setup(x => x.IpAddress).Returns("127.0.0.1");
		currentUserServiceMock.Setup(x => x.UserAgent).Returns("Mozilla/5.0");

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Result.Success(1));

		// Act
		Result<Guid> result = await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(response));

		// Assert
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
		// Arrange
		RegisterUserCommand request = new("test@example.com", "Password123!", "John", "Doe");
		Result<Guid> response = Result.Failure<Guid>(DomainErrors.User.EmailAlreadyExists);

		// Act
		Result<Guid> result = await behavior.Handle(
			request,
			CancellationToken.None,
			async () => await Task.FromResult(response));

		// Assert
		result.Should().Be(response);
		auditLogRepositoryMock.Verify(x => x.Add(It.IsAny<AuditLog>()), Times.Never);
	}

	private class TestRequestWithoutAttribute : IRequest<Result<Guid>>
	{
	}
}

