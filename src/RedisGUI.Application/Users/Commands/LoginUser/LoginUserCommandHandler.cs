using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;
using System;
using System.Threading;
using System.Threading.Tasks;

namespace RedisGUI.Application.Users.Commands.LoginUser;

/// <summary>
/// Handler for user login/authentication
/// </summary>
internal sealed class LoginUserCommandHandler : ICommandHandler<LoginUserCommand, LoginUserResponse>
{
	private readonly IUserRepository userRepository;
	private readonly IPasswordHasher passwordHasher;
	private readonly IJwtTokenGenerator tokenGenerator;
	private readonly IUnitOfWork unitOfWork;

	/// <summary>
	/// Initializes a new instance of the LoginUserCommandHandler
	/// </summary>
	public LoginUserCommandHandler(
		IUserRepository userRepository,
		IPasswordHasher passwordHasher,
		IJwtTokenGenerator tokenGenerator,
		IUnitOfWork unitOfWork)
	{
		this.userRepository = userRepository;
		this.passwordHasher = passwordHasher;
		this.tokenGenerator = tokenGenerator;
		this.unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Handles the user login command
	/// </summary>
	public async Task<Result<LoginUserResponse>> Handle(
		LoginUserCommand request,
		CancellationToken cancellationToken)
	{
		// 1. Validate and create email value object
		var emailResult = Email.Create(request.Email);
		if (emailResult.IsFailure)
			return Result.Failure<LoginUserResponse>(emailResult.Error);

		// 2. Get user by email
		var user = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
		if (user is null)
			return Result.Failure<LoginUserResponse>(Domain.Errors.DomainErrors.User.InvalidCredentials);

		// 3. Verify password
		if (!passwordHasher.Verify(request.Password, user.PasswordHash.Value))
			return Result.Failure<LoginUserResponse>(Domain.Errors.DomainErrors.User.InvalidCredentials);

		// 4. Check if user is active
		if (!user.IsActive)
			return Result.Failure<LoginUserResponse>(Domain.Errors.DomainErrors.User.Deactivated);

		// 5. Record login and update last login timestamp
		user.RecordLogin();
		await unitOfWork.SaveChangesAsync(cancellationToken);

		// 6. Generate JWT token
		var token = tokenGenerator.GenerateToken(user);

		// 7. Calculate token expiration (default: 60 minutes)
		var expiresAt = DateTime.UtcNow.AddMinutes(60);

		// 8. Return response with user details
		return Result.Success(new LoginUserResponse(
			user.Id,
			user.Email.Value,
			user.Profile.FirstName,
			user.Profile.LastName,
			user.Role,
			token,
			expiresAt));
	}
}
