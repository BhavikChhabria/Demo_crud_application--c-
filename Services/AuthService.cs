using Microsoft.EntityFrameworkCore;
using Microsoft.IdentityModel.Tokens;
using MiddlewareDemo.Data;
using MiddlewareDemo.Models;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Text.RegularExpressions;
using Microsoft.AspNetCore.JsonPatch;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System.Threading.Tasks;

namespace MiddlewareDemo.Services
{
    public class AuthService
    {
        private readonly ApplicationDbContext _context;
        private readonly IConfiguration _config;
        private readonly ILogger<AuthService> _logger;

        public AuthService(ApplicationDbContext context, IConfiguration config, ILogger<AuthService> logger)
        {
            _context = context ?? throw new ArgumentNullException(nameof(context));
            _config = config ?? throw new ArgumentNullException(nameof(config));
            _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        }

        // ✅ Register a new user
        public async Task<string> Register(RegisterUserModel userModel, string city, string pincode, string mobileNumber)
        {
            if (userModel == null) return "Invalid user data.";

            if (!IsValidEmail(userModel.Email))
                return "Invalid email format! Please enter a valid email address.";

            if (!IsValidPassword(userModel.Password))
                return "Weak password! Must be 8+ chars, 1 uppercase, 1 number, 1 special char.";

            if (await _context.Users.AnyAsync(u => u.Email == userModel.Email))
                return "User already exists with this email!";

            if (userModel.Password != userModel.ConfirmPassword)
                return "Password and Confirm Password do not match!";

            var hashedPassword = BCrypt.Net.BCrypt.HashPassword(userModel.Password);

            var user = new User
            {
                FirstName = userModel.FirstName,
                LastName = userModel.LastName,
                Email = userModel.Email,
                PasswordHash = hashedPassword
            };

            await _context.Users.AddAsync(user);
            await _context.SaveChangesAsync();

            var userDetail = new UserDetail
            {
                UserId = user.Id,
                City = city,
                Pincode = pincode,
                MobileNumber = mobileNumber,
                LastLoginUser = DateTime.UtcNow
            };

            await _context.UserDetails.AddAsync(userDetail);
            await _context.SaveChangesAsync();

            _logger.LogInformation("New user registered: {Email}", user.Email);

            return $"User {user.Email} registered successfully!";
        }

        // ✅ Login a user
        public async Task<(string? token, string? errorMessage)> Login(LoginUserModel userModel)
        {
            if (userModel == null) return (null, "Invalid login data.");

            if (!IsValidEmail(userModel.Email))
                return (null, "Invalid email format!");

            var user = await _context.Users.FirstOrDefaultAsync(u => u.Email == userModel.Email);
            if (user == null || !BCrypt.Net.BCrypt.Verify(userModel.Password, user.PasswordHash))
                return (null, "Invalid credentials!");

            user.LoginCount++;
            await _context.SaveChangesAsync();

            var secretKey = _config["JwtSettings:Secret"];
            if (string.IsNullOrEmpty(secretKey))
            {
                _logger.LogError("JWT Secret key is missing in configuration.");
                return (null, "Server error: Missing JWT configuration.");
            }

            if (!int.TryParse(_config["JwtSettings:ExpiryMinutes"], out int expiryMinutes))
                expiryMinutes = 60; // Default 60 min if missing

            var key = Encoding.UTF8.GetBytes(secretKey);
            var tokenHandler = new JwtSecurityTokenHandler();
            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(new[]
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Email, user.Email)
                }),
                Expires = DateTime.UtcNow.AddMinutes(expiryMinutes),
                Issuer = _config["JwtSettings:Issuer"],
                Audience = _config["JwtSettings:Audience"],
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            _logger.LogInformation("User logged in: {Email}", user.Email);

            return (tokenHandler.WriteToken(token), null);
        }

        // ✅ Get user by ID (Include UserDetail)
        public async Task<UserDTO?> GetUserById(int id)
        {
            var user = await _context.Users.FindAsync(id);
            var userDetail = await _context.UserDetails.FirstOrDefaultAsync(ud => ud.UserId == id);

            return user == null ? null : new UserDTO(user, userDetail);
        }

        // ✅ Get all users (Include UserDetail)
        public async Task<List<UserDTO>> GetAllUsers()
        {
            var users = await _context.Users.ToListAsync();
            var userDetails = await _context.UserDetails.ToListAsync();

            return users.Select(user =>
            {
                var userDetail = userDetails.FirstOrDefault(ud => ud.UserId == user.Id);
                return new UserDTO(user, userDetail);
            }).ToList();
        }

        // ✅ Update user and UserDetail
        public async Task<string> UpdateUser(int id, RegisterUserModel userModel, string city, string pincode, string mobileNumber)
        {
            if (userModel == null) return "Invalid user data.";

            if (!IsValidEmail(userModel.Email))
                return "Invalid email format! Please enter a valid email address.";

            var user = await _context.Users.FindAsync(id);
            if (user == null) return "User not found!";

            user.FirstName = userModel.FirstName;
            user.LastName = userModel.LastName;
            user.Email = userModel.Email;

            var userDetail = await _context.UserDetails.FirstOrDefaultAsync(ud => ud.UserId == id);
            if (userDetail != null)
            {
                userDetail.City = city;
                userDetail.Pincode = pincode;
                userDetail.MobileNumber = mobileNumber;
                userDetail.LastLoginUser = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User updated: {@User}", new UserDTO(user, userDetail));

            return "User updated successfully!";
        }

        // ✅ Patch user
        public async Task<string> PatchUser(int id, JsonPatchDocument<User> patchDoc)
        {
            if (patchDoc == null || patchDoc.Operations.Count == 0)
                return "Invalid patch request!";

            var user = await _context.Users.FindAsync(id);
            if (user == null) return "User not found!";

            patchDoc.ApplyTo(user);

            var passwordOperation = patchDoc.Operations.FirstOrDefault(o => o.path == "/passwordHash");
            if (passwordOperation?.value is string newPassword)
            {
                if (!IsValidPassword(newPassword))
                    return "Weak password!";
                
                user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(newPassword);
            }

            await _context.SaveChangesAsync();
            _logger.LogInformation("User patched: {@User}", new UserDTO(user, null));

            return "User updated successfully!";
        }

        // ✅ Delete user
        public async Task<string> DeleteUser(int id)
        {
            var user = await _context.Users.FindAsync(id);
            if (user == null) return "User not found!";

            var userDetail = await _context.UserDetails.FirstOrDefaultAsync(ud => ud.UserId == id);
            if (userDetail != null)
                _context.UserDetails.Remove(userDetail);

            _context.Users.Remove(user);
            await _context.SaveChangesAsync();
            _logger.LogInformation("User deleted: ID {UserId}", id);

            return "User deleted successfully!";
        }

        // ✅ Email validation
        private static bool IsValidEmail(string email)
        {
            const string emailPattern = @"^[^@\s]+@[^@\s]+\.[^@\s]+$";
            return Regex.IsMatch(email, emailPattern);
        }

        // ✅ Password validation
        private static bool IsValidPassword(string password)
        {
            const string passwordPattern = @"^(?=.*[A-Z])(?=.*[a-z])(?=.*\d)(?=.*[@$!%*?&#^()-+=])[A-Za-z\d@$!%*?&#^()-+=]{8,}$";
            return Regex.IsMatch(password, passwordPattern);
        }
    }
}
