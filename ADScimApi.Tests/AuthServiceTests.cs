using Microsoft.VisualStudio.TestTools.UnitTesting;
using ADScimApi.Auth;
using ADScimApi.Models;
using Microsoft.Extensions.Configuration;
using System.Threading.Tasks;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;
using Moq;

namespace ADScimApi.Tests;

[TestClass]
public class AuthServiceTests
{
    private IAuthService _authService;
    private Mock<ILogger<AuthService>> _loggerMock;
    private IConfiguration _configuration;

    [TestInitialize]
    public void Setup()
    {
        // Setup configuration
        var inMemorySettings = new Dictionary<string, string> {
            {"JwtSettings:Secret", "TestSecretKeyWithAtLeast32Characters1234567890"},
            {"JwtSettings:ExpiryMinutes", "60"},
            {"JwtSettings:Issuer", "TestIssuer"},
            {"JwtSettings:Audience", "TestAudience"}
        };

        _configuration = new ConfigurationBuilder()
            .AddInMemoryCollection(inMemorySettings)
            .Build();

        _loggerMock = new Mock<ILogger<AuthService>>();
        _authService = new AuthService(_configuration, _loggerMock.Object);
    }

    [TestMethod]
    public async Task Authenticate_ValidCredentials_ReturnsToken()
    {
        // Arrange
        var request = new AuthRequest
        {
            Username = "admin",
            Password = "password"
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        Assert.IsNotNull(result);
        Assert.IsNotNull(result.Token);
        Assert.IsTrue(result.ExpiresIn > 0);
        Assert.AreEqual("admin", result.Username);
    }

    [TestMethod]
    public async Task Authenticate_InvalidCredentials_ReturnsNull()
    {
        // Arrange
        var request = new AuthRequest
        {
            Username = "invalid",
            Password = "wrongpassword"
        };

        // Act
        var result = await _authService.AuthenticateAsync(request);

        // Assert
        Assert.IsNull(result);
    }
}