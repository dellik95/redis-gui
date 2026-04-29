using FluentAssertions;
using Moq;
using RedisGUI.Application.Users.Commands.RegisterUser;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Users;

[TestFixture]
public class RegisterUserCommandHandlerTests
{
	private Mock<IUserRepository> userRepositoryMock = null!;
	private Mock<IPasswordHasher> passwordHasherMock = null!;
	private Mock<IUnitOfWork> unitOfWorkMock = null!;
	private RegisterUserCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		userRepositoryMock = new Mock<IUserRepository>();
		passwordHasherMock = new Mock<IPasswordHasher>();
		unitOfWorkMock = new Mock<IUnitOfWork>();
		handler = new RegisterUserCommandHandler(
			userRepositoryMock.Object,
			passwordHasherMock.Object,
			unitOfWorkMock.Object);
	}

	[Test]
	public async Task Handle_ValidRequest_ReturnsUserId()
	{
		// Arrange
		RegisterUserCommand command = new(
			"test@example.com",
			"Password123!",
			"John",
			"Doe");

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((User?)null);

		passwordHasherMock
			.Setup(x => x.Hash(It.IsAny<string>()))
			.Returns("hashed_password");

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Shared.Primitives.Result.Success(1));

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBe(System.Guid.Empty);
		userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Once);
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_EmailAlreadyExists_ReturnsFailure()
	{
		// Arrange
		RegisterUserCommand command = new(
			"existing@example.com",
			"Password123!",
			"John",
			"Doe");

		var emailResult = Email.Create("existing@example.com");
		var passwordHashResult = PasswordHash.Create("hash", passwordHasherMock.Object);
		var profileResult = UserProfile.Create("Jane", "Doe");

		if (emailResult.IsFailure || passwordHashResult.IsFailure || profileResult.IsFailure)
		{
			Assert.Fail("Failed to create test data");
			return;
		}

		var userResult = User.Create(
			emailResult.Value,
			passwordHashResult.Value,
			profileResult.Value,
			UserRole.User);

		if (userResult.IsFailure)
		{
			Assert.Fail("Failed to create user");
			return;
		}

		User existingUser = userResult.Value;

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(existingUser);

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.EmailAlreadyExists);
		userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
	}

	[Test]
	public async Task Handle_InvalidEmail_ReturnsFailure()
	{
		// Arrange
		RegisterUserCommand command = new(
			"invalid-email",
			"Password123!",
			"John",
			"Doe");

		// Act
		Result<Guid> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		userRepositoryMock.Verify(x => x.Add(It.IsAny<User>()), Times.Never);
	}
}

