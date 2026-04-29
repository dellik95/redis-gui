using FluentAssertions;
using Microsoft.AspNetCore.Http;
using Moq;
using NUnit.Framework;
using RedisGUI.Infrastructure.Services;
using System.Security.Claims;

namespace RedisGUI.Infrastructure.Tests.Services;

/// <summary>
/// Tests for CurrentUserService
/// </summary>
[TestFixture]
public class CurrentUserServiceTests
{
	private Mock<IHttpContextAccessor> _httpContextAccessorMock = null!;
	private CurrentUserService _service = null!;

	[SetUp]
	public void Setup()
	{
		_httpContextAccessorMock = new Mock<IHttpContextAccessor>();
		_service = new CurrentUserService(_httpContextAccessorMock.Object);
	}

	[Test]
	public void UserId_WithNameIdentifierClaim_ReturnsUserId()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, userId.ToString())
		};
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserId;

		// Assert
		result.Should().Be(userId);
	}

	[Test]
	public void UserId_WithSubClaim_ReturnsUserId()
	{
		// Arrange
		var userId = Guid.NewGuid();
		var claims = new List<Claim>
		{
			new Claim("sub", userId.ToString())
		};
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserId;

		// Assert
		result.Should().Be(userId);
	}

	[Test]
	public void UserId_WithNoClaims_ReturnsNull()
	{
		// Arrange
		var identity = new ClaimsIdentity();
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserId;

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void UserId_WithInvalidGuidClaim_ReturnsNull()
	{
		// Arrange
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, "NotAGuid")
		};
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserId;

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void UserId_WithNullHttpContext_ReturnsNull()
	{
		// Arrange
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

		// Act
		var result = _service.UserId;

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void IpAddress_WithHttpContext_ReturnsIpAddress()
	{
		// Arrange
		var ipAddress = System.Net.IPAddress.Parse("192.168.1.1");
		var httpContext = new DefaultHttpContext();
		httpContext.Connection.RemoteIpAddress = ipAddress;
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.IpAddress;

		// Assert
		result.Should().Be(ipAddress.ToString());
	}

	[Test]
	public void IpAddress_WithNullHttpContext_ReturnsNull()
	{
		// Arrange
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

		// Act
		var result = _service.IpAddress;

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void UserAgent_WithHttpContext_ReturnsUserAgent()
	{
		// Arrange
		var userAgent = "Mozilla/5.0 (Windows NT 10.0; Win64; x64)";
		var httpContext = new DefaultHttpContext();
		httpContext.Request.Headers["User-Agent"] = userAgent;
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserAgent;

		// Assert
		result.Should().Be(userAgent);
	}

	[Test]
	public void UserAgent_WithNullHttpContext_ReturnsNull()
	{
		// Arrange
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

		// Act
		var result = _service.UserAgent;

		// Assert
		result.Should().BeNull();
	}

	[Test]
	public void IsAuthenticated_WithAuthenticatedUser_ReturnsTrue()
	{
		// Arrange
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, Guid.NewGuid().ToString())
		};
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.IsAuthenticated;

		// Assert
		result.Should().BeTrue();
	}

	[Test]
	public void IsAuthenticated_WithUnauthenticatedUser_ReturnsFalse()
	{
		// Arrange
		var identity = new ClaimsIdentity();
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.IsAuthenticated;

		// Assert
		result.Should().BeFalse();
	}

	[Test]
	public void IsAuthenticated_WithNullHttpContext_ReturnsFalse()
	{
		// Arrange
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns((HttpContext?)null);

		// Act
		var result = _service.IsAuthenticated;

		// Assert
		result.Should().BeFalse();
	}

	[Test]
	public void UserId_SubClaimTakesPrecedence_WhenBothPresent()
	{
		// Arrange
		var nameIdentifierId = Guid.NewGuid();
		var subId = Guid.NewGuid();
		var claims = new List<Claim>
		{
			new Claim(ClaimTypes.NameIdentifier, nameIdentifierId.ToString()),
			new Claim("sub", subId.ToString())
		};
		var identity = new ClaimsIdentity(claims, "Test");
		var principal = new ClaimsPrincipal(identity);
		var httpContext = new DefaultHttpContext
		{
			User = principal
		};
		_httpContextAccessorMock.Setup(x => x.HttpContext).Returns(httpContext);

		// Act
		var result = _service.UserId;

		// Assert
		// NameIdentifier is checked first, so it should return that value
		result.Should().Be(nameIdentifierId);
	}
}

