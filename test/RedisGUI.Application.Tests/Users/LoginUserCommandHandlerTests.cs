using FluentAssertions;
using Moq;
using RedisGUI.Application.Users.Commands.LoginUser;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Errors;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Tests.Users;

[TestFixture]
public class LoginUserCommandHandlerTests
{
	private Mock<IUserRepository> userRepositoryMock = null!;
	private Mock<IPasswordHasher> passwordHasherMock = null!;
	private Mock<IJwtTokenGenerator> tokenGeneratorMock = null!;
	private Mock<IUnitOfWork> unitOfWorkMock = null!;
	private LoginUserCommandHandler handler = null!;

	[SetUp]
	public void Setup()
	{
		userRepositoryMock = new Mock<IUserRepository>();
		passwordHasherMock = new Mock<IPasswordHasher>();
		tokenGeneratorMock = new Mock<IJwtTokenGenerator>();
		unitOfWorkMock = new Mock<IUnitOfWork>();
		handler = new LoginUserCommandHandler(
			userRepositoryMock.Object,
			passwordHasherMock.Object,
			tokenGeneratorMock.Object,
			unitOfWorkMock.Object);
	}

	[Test]
	public async Task Handle_ValidCredentials_ReturnsLoginResponse()
	{
		// Arrange
		LoginUserCommand command = new(
			"test@example.com",
			"Password123!");

		var emailResult = Email.Create("test@example.com");
		var passwordHashResult = PasswordHash.Create("hashed", passwordHasherMock.Object);
		var profileResult = UserProfile.Create("John", "Doe");

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

		User user = userResult.Value;

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(user);

		passwordHasherMock
			.Setup(x => x.Verify("Password123!", It.IsAny<string>()))
			.Returns(true);

		tokenGeneratorMock
			.Setup(x => x.GenerateToken(user))
			.Returns("jwt_token");

		unitOfWorkMock
			.Setup(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()))
			.ReturnsAsync(Shared.Primitives.Result.Success(1));

		// Act
		Result<LoginUserResponse> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsSuccess.Should().BeTrue();
		result.Value.Should().NotBeNull();
		result.Value.Token.Should().Be("jwt_token");
		result.Value.Email.Should().Be("test@example.com");
		unitOfWorkMock.Verify(x => x.SaveChangesAsync(It.IsAny<CancellationToken>()), Times.Once);
	}

	[Test]
	public async Task Handle_UserNotFound_ReturnsFailure()
	{
		// Arrange
		LoginUserCommand command = new(
			"notfound@example.com",
			"Password123!");

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync((User?)null);

		// Act
		Result<LoginUserResponse> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.InvalidCredentials);
	}

	[Test]
	public async Task Handle_InvalidPassword_ReturnsFailure()
	{
		// Arrange
		LoginUserCommand command = new(
			"test@example.com",
			"WrongPassword");

		var emailResult = Email.Create("test@example.com");
		var passwordHashResult = PasswordHash.Create("hashed", passwordHasherMock.Object);
		var profileResult = UserProfile.Create("John", "Doe");

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

		User user = userResult.Value;

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(user);

		passwordHasherMock
			.Setup(x => x.Verify("WrongPassword", It.IsAny<string>()))
			.Returns(false);

		// Act
		Result<LoginUserResponse> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.InvalidCredentials);
	}

	[Test]
	public async Task Handle_DeactivatedUser_ReturnsFailure()
	{
		// Arrange
		LoginUserCommand command = new(
			"test@example.com",
			"Password123!");

		var emailResult = Email.Create("test@example.com");
		var passwordHashResult = PasswordHash.Create("hashed", passwordHasherMock.Object);
		var profileResult = UserProfile.Create("John", "Doe");

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

		User user = userResult.Value;
		user.Deactivate();

		userRepositoryMock
			.Setup(x => x.GetByEmailAsync(It.IsAny<Email>(), It.IsAny<CancellationToken>()))
			.ReturnsAsync(user);

		passwordHasherMock
			.Setup(x => x.Verify("Password123!", It.IsAny<string>()))
			.Returns(true);

		// Act
		Result<LoginUserResponse> result = await handler.Handle(command, CancellationToken.None);

		// Assert
		result.IsFailure.Should().BeTrue();
		result.Error.Should().Be(DomainErrors.User.Deactivated);
	}
}

