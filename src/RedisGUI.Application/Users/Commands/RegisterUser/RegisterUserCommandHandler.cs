using System;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Mediator;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Users.Commands.RegisterUser;

/// <summary>
/// Handler for registering a new user
/// </summary>
internal sealed class RegisterUserCommandHandler : ICommandHandler<RegisterUserCommand, Guid>
{
	private readonly IUserRepository userRepository;
	private readonly IPasswordHasher passwordHasher;
	private readonly IUnitOfWork unitOfWork;

	/// <summary>
	/// Initializes a new instance of the RegisterUserCommandHandler
	/// </summary>
	public RegisterUserCommandHandler(
		IUserRepository userRepository,
		IPasswordHasher passwordHasher,
		IUnitOfWork unitOfWork)
	{
		this.userRepository = userRepository;
		this.passwordHasher = passwordHasher;
		this.unitOfWork = unitOfWork;
	}

	/// <summary>
	/// Handles the user registration command
	/// </summary>
	public async Task<Result<Guid>> Handle(RegisterUserCommand request, CancellationToken cancellationToken)
	{
		// 1. Create and validate email value object
		var emailResult = Email.Create(request.Email);
		if (emailResult.IsFailure)
			return Result.Failure<Guid>(emailResult.Error);

		// 2. Check if email already exists
		var existingUser = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
		if (existingUser is not null)
			return Result.Failure<Guid>(Domain.Errors.DomainErrors.User.EmailAlreadyExists);

		// 3. Create and validate password hash value object
		var passwordResult = PasswordHash.Create(request.Password, passwordHasher);
		if (passwordResult.IsFailure)
			return Result.Failure<Guid>(passwordResult.Error);

		// 4. Create and validate user profile value object
		var profileResult = UserProfile.Create(request.FirstName, request.LastName);
		if (profileResult.IsFailure)
			return Result.Failure<Guid>(profileResult.Error);

		// 5. Create user aggregate
		var userResult = User.Create(
			emailResult.Value,
			passwordResult.Value,
			profileResult.Value,
			UserRole.User); // Default role

		if (userResult.IsFailure)
			return Result.Failure<Guid>(userResult.Error);

		// 6. Persist to repository
		userRepository.Add(userResult.Value);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Success(userResult.Value.Id);
	}
}
