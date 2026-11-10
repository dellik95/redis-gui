using FluentAssertions;
using RedisGUI.Infrastructure.Authentication;
using System.Collections.Generic;
using System.ComponentModel.DataAnnotations;

namespace RedisGUI.Application.Tests.Authentication;

/// <summary>
/// Tests for JwtSettings data annotation validation.
/// </summary>
public class JwtSettingsTests
{
	[Test]
	public void JwtSettings_ValidConfiguration_ShouldPassValidation()
	{
		// Arrange
		JwtSettings settings = new()
		{
			SecretKey = new string('x', 32),
			Issuer = "RedisGUI",
			Audience = "RedisGUI.Client",
		};

		// Act
		IList<ValidationResult> results = Validate(settings);

		// Assert
		results.Should().BeEmpty();
	}

	[Test]
	public void JwtSettings_ShortSecretKey_ShouldFailValidation()
	{
		// Arrange
		JwtSettings settings = new()
		{
			SecretKey = "short",
			Issuer = "RedisGUI",
			Audience = "RedisGUI.Client",
		};

		// Act
		IList<ValidationResult> results = Validate(settings);

		// Assert
		results.Should().ContainSingle();
	}

	[Test]
	public void JwtSettings_MissingIssuerOrAudience_ShouldFailValidation()
	{
		// Arrange
		JwtSettings settings = new()
		{
			SecretKey = new string('x', 32),
			Issuer = string.Empty,
			Audience = string.Empty,
		};

		// Act
		IList<ValidationResult> results = Validate(settings);

		// Assert
		results.Should().NotBeEmpty();
	}

	private static IList<ValidationResult> Validate(JwtSettings settings)
	{
		ValidationContext context = new(settings);
		List<ValidationResult> results = [];
		Validator.TryValidateObject(settings, context, results, validateAllProperties: true);
		return results;
	}
}


