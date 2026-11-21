using FluentResults;
using System;
using RedisGUI.Domain.Audit;
using DispatchWeave;
using DispatchWeave.Results;
using System.Threading;
using System.Threading.Tasks;
using RedisGUI.Domain.Abstraction;
using RedisGUI.Domain.Users;
using RedisGUI.Shared.Primitives;

namespace RedisGUI.Application.Users.Commands.RegisterUser;

/// <summary>
/// Command to register a new user in the system
/// </summary>
/// <param name="Email">The user's email address</param>
/// <param name="Password">The user's password</param>
/// <param name="FirstName">The user's first name</param>
/// <param name="LastName">The user's last name</param>
[Auditable(AuditAction.Create, "User")]
public sealed record RegisterUserCommand(
	string Email,
	string Password,
	string FirstName,
	string LastName) : ICommand<Guid>;

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
		if (emailResult.IsFailed)
			return Result.Fail<Guid>(emailResult.Errors);

		// 2. Check if email already exists
		var existingUser = await userRepository.GetByEmailAsync(emailResult.Value, cancellationToken);
		if (existingUser is not null)
			return Result.Fail<Guid>(Domain.Errors.DomainErrors.User.EmailAlreadyExists);

		// 3. Create and validate password hash value object
		var passwordResult = PasswordHash.Create(request.Password, passwordHasher);
		if (passwordResult.IsFailed)
			return Result.Fail<Guid>(passwordResult.Errors);

		// 4. Create and validate user profile value object
		var profileResult = UserProfile.Create(request.FirstName, request.LastName);
		if (profileResult.IsFailed)
			return Result.Fail<Guid>(profileResult.Errors);

		// 5. Create user aggregate
		var userResult = User.Create(
			emailResult.Value,
			passwordResult.Value,
			profileResult.Value,
			UserRole.User); // Default role

		if (userResult.IsFailed)
			return Result.Fail<Guid>(userResult.Errors);

		// 6. Persist to repository
		userRepository.Add(userResult.Value);
		await unitOfWork.SaveChangesAsync(cancellationToken);

		return Result.Ok(userResult.Value.Id);
	}
}
