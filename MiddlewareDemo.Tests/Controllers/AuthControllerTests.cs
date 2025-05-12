using Xunit;
using Moq;
using Microsoft.Extensions.Logging;
using MiddlewareDemo.Controllers;
using MiddlewareDemo.Services;
using MiddlewareDemo.Models;
using Microsoft.AspNetCore.Mvc;
using System.Threading.Tasks;

namespace MiddlewareDemo.Tests.Controllers
{
    public class AuthControllerTests
    {
        private readonly Mock<AuthService> _authServiceMock;
        private readonly Mock<ILogger<AuthController>> _loggerMock;
        private readonly AuthController _controller;

        public AuthControllerTests()
        {
            _authServiceMock = new Mock<AuthService>();
            _loggerMock = new Mock<ILogger<AuthController>>();

            // You must use a mockable version of AuthService or wrap it in an interface
            _controller = new AuthController(_authServiceMock.Object, _loggerMock.Object);
        }

        [Fact]
        public async Task Register_ReturnsOk_WhenRegistrationSuccessful()
        {
            // Arrange
            var model = new RegisterUserModel
            {
                FirstName = "John",
                LastName = "Doe",
                Email = "john@example.com",
                Password = "Pass@123",
                ConfirmPassword = "Pass@123"
            };

            _authServiceMock
                .Setup(s => s.Register(model, "City", "123456", "9999999999"))
                .ReturnsAsync("User john@example.com registered successfully!");

            // Act
            var result = await _controller.Register(model, "City", "123456", "9999999999");

            // Assert
            var okResult = Assert.IsType<OkObjectResult>(result);
            Assert.Contains("registered successfully", okResult.Value.ToString());
        }

        [Fact]
        public async Task Login_ReturnsBadRequest_WhenInvalidCredentials()
        {
            // Arrange
            var model = new LoginUserModel
            {
                Email = "invalid@example.com",
                Password = "wrongpassword"
            };

            _authServiceMock
                .Setup(s => s.Login(model))
                .ReturnsAsync((null, "Invalid credentials"));

            // Act
            var result = await _controller.Login(model);

            // Assert
            var badResult = Assert.IsType<BadRequestObjectResult>(result);
            Assert.Contains("Invalid credentials", badResult.Value.ToString());
        }
    }
}
