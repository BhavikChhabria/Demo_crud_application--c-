using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using MiddlewareDemo.Models;
using MiddlewareDemo.Services;
using System.Threading.Tasks;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Logging;

namespace MiddlewareDemo.Controllers
{
    [Route("api/auth")]
    [ApiController]
    public class AuthController : ControllerBase
    {
        private readonly AuthService _authService;
        private readonly ILogger<AuthController> _logger;

        public AuthController(AuthService authService, ILogger<AuthController> logger)
        {
            _authService = authService ?? throw new ArgumentNullException(nameof(authService));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ✅ Register a new user
        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterUserModel userModel, string city, string pincode, string mobileNumber)
        {
            if (userModel == null)
                return BadRequest(new { Error = "Invalid request data." });

            var message = await _authService.Register(userModel, city, pincode, mobileNumber);
            if (message.Contains("successfully"))
            {
                _logger.LogInformation("User registered successfully: {Email}", userModel.Email);
                return Ok(new { Message = message });
            }

            _logger.LogWarning("Registration failed: {Message}", message);
            return BadRequest(new { Error = message });
        }

        // ✅ User Login
        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginUserModel userModel)
        {
            if (userModel == null)
                return BadRequest(new { Error = "Invalid login data." });

            var (token, errorMessage) = await _authService.Login(userModel);
            if (!string.IsNullOrEmpty(errorMessage))
            {
                _logger.LogWarning("Login failed: {Error}", errorMessage);
                return BadRequest(new { Error = errorMessage });
            }

            _logger.LogInformation("User logged in: {Email}", userModel.Email);
            return Ok(new
            {
                Message = "Login successful",
                Token = token
            });
        }

        // ✅ Get User By ID (Includes UserDetail)
        [Authorize]
        [HttpGet("user/{id}")]
        public async Task<IActionResult> GetUserById(int id)
        {
            var userDTO = await _authService.GetUserById(id);
            if (userDTO == null)
            {
                _logger.LogWarning("User not found: ID {Id}", id);
                return NotFound(new { Error = "User not found." });
            }

            return Ok(new { User = userDTO });
        }

        // ✅ Get All Users (Includes UserDetail)
        [Authorize]
        [HttpGet("users")]
        public async Task<IActionResult> GetAllUsers()
        {
            var users = await _authService.GetAllUsers();
            return Ok(new { Users = users });
        }

        // ✅ Update User By ID (Includes UserDetail)
        [Authorize]
        [HttpPut("user/{id}")]
        public async Task<IActionResult> UpdateUser(int id, [FromBody] RegisterUserModel userModel, string city, string pincode, string mobileNumber)
        {
            if (userModel == null)
                return BadRequest(new { Error = "Invalid user data." });

            var result = await _authService.UpdateUser(id, userModel, city, pincode, mobileNumber);
            if (result.Contains("successfully"))
            {
                _logger.LogInformation("User updated: ID {Id}", id);
                return Ok(new { Message = result });
            }

            _logger.LogWarning("User update failed: ID {Id}, Error: {Error}", id, result);
            return BadRequest(new { Error = result });
        }

        // ✅ Patch User (JSON Patch)
        // [Authorize]
        // [HttpPatch("user/{id}")]
        // public async Task<IActionResult> PatchUser(int id, [FromBody] JsonPatchDocument<User> patchDoc)
        // {
        //     if (patchDoc == null || patchDoc.Operations.Count == 0)
        //         return BadRequest(new { Error = "Invalid patch request!" });

        //     var result = await _authService.PatchUser(id, patchDoc);
        //     if (result.Contains("not found") || result.Contains("Invalid"))
        //     {
        //         _logger.LogWarning("Patch failed: ID {Id}, Error: {Error}", id, result);
        //         return BadRequest(new { Error = result });
        //     }

        //     _logger.LogInformation("User patched: ID {Id}", id);
        //     return Ok(new { Message = result });
        // }


        [Authorize]
        [HttpPatch("user/{id}")]
        public async Task<IActionResult> PatchUser(int id, [FromBody] JsonPatchDocument<User> patchDoc)
        {
            if (patchDoc == null || patchDoc.Operations.Count == 0)
                return BadRequest(new { Error = "Invalid patch request!" });

            try
            {
                var result = await _authService.PatchUser(id, patchDoc);
                if (result.Contains("not found") || result.Contains("Invalid"))
                {
                    _logger.LogWarning("Patch failed: ID {Id}, Error: {Error}", id, result);
                    return NotFound(new { Error = result });
                }

                _logger.LogInformation("User patched successfully: ID {Id}", id);
                return Ok(new { Message = result });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Exception while patching user ID {Id}", id);
                return StatusCode(500, new { Error = "An unexpected error occurred while patching the user." });
            }
        }
        // ✅ Delete User By ID
        [Authorize]
        [HttpDelete("user/{id}")]
        public async Task<IActionResult> DeleteUser(int id)
        {
            var result = await _authService.DeleteUser(id);
            if (result.Contains("successfully"))
            {
                _logger.LogInformation("User deleted: ID {Id}", id);
                return Ok(new { Message = result });
            }

            _logger.LogWarning("Delete failed: ID {Id}, Error: {Error}", id, result);
            return NotFound(new { Error = result });
        }
    }
}
