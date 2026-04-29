using Microsoft.IdentityModel.Tokens;
using RedisGUI.Domain.Users;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace RedisGUI.Infrastructure.Authentication;

/// <summary>
/// Generates JWT tokens for user authentication
/// </summary>
internal sealed class JwtTokenGenerator : IJwtTokenGenerator
{
	private readonly JwtSettings jwtSettings;

	/// <summary>
	/// Initializes a new instance of the JwtTokenGenerator
	/// </summary>
	public JwtTokenGenerator(IOptions<JwtSettings> jwtSettings)
	{
		this.jwtSettings = jwtSettings.Value;
	}

	/// <summary>
	/// Generates a JWT token for the specified user
	/// </summary>
	public string GenerateToken(User user)
	{
		SigningCredentials signingCredentials = new(
			new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSettings.SecretKey)),
			SecurityAlgorithms.HmacSha256);

		Claim[] claims = new[]
		{
			new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
			new Claim(JwtRegisteredClaimNames.Email, user.Email.Value),
			new Claim(ClaimTypes.Role, user.Role.ToString()),
			new Claim("firstName", user.Profile.FirstName),
			new Claim("lastName", user.Profile.LastName),
			new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
		};

		JwtSecurityToken token = new(
			issuer: jwtSettings.Issuer,
			audience: jwtSettings.Audience,
			claims: claims,
			expires: DateTime.UtcNow.AddMinutes(jwtSettings.ExpirationMinutes),
			signingCredentials: signingCredentials);

		return new JwtSecurityTokenHandler().WriteToken(token);
	}
}
