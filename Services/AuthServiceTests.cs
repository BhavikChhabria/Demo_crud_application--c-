using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging.Abstractions;
using MiddlewareDemo.Data;
using MiddlewareDemo.Models;
using MiddlewareDemo.Services;
using Moq;
using System;
using System.Threading.Tasks;
using Xunit;

namespace MiddlewareDemo.Tests
{
    public class AuthServiceTests : IDisposable
    {
        private readonly ApplicationDbContext _context;
        private readonly AuthService _authService;

        public AuthServiceTests()
        {
            // Setup an in-memory database for testing
            var options = new DbContextOptionsBuilder<ApplicationDbContext>()
                .UseInMemoryDatabase(databaseName: Guid.NewGuid().ToString())
                .Options;
            _context = new ApplicationDbContext(options);

            // Mock the IConfiguration
            var mockConfiguration = new Mock<IConfiguration>();
            mockConfiguration.Setup(c => c["JwtSettings:Secret"]).Returns("test-secret-key");
            mockConfiguration.Setup(c => c["JwtSettings:Issuer"]).Returns("test-issuer");
            mockConfiguration.Setup(c => c["JwtSettings:Audience"]).Returns("test-audience");
            mockConfiguration.Setup(c => c["JwtSettings:ExpiryMinutes"]).Returns("60");

            // Create an instance of AuthService with the mocked dependencies
            _authService = new AuthService(_context, mockConfiguration.Object, NullLogger<AuthService>.Instance);
        }

        // Implement IDisposable to clean up the in-memory database after each test
        public void Dispose()
        {
            _context.Database.EnsureDeleted();
            _context.Dispose();
        }
    }
}