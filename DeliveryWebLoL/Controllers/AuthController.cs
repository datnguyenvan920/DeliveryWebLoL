using Microsoft.AspNetCore.Mvc;
using DeliveryWebLoL.Service.Interfaces;
using DeliveryWebLoL.Models;
using DeliveryWebLoL.DTO.Auth;
using DeliveryWebLoL.Service;

namespace DeliveryWebLoL.Controllers
{
    [ApiController]
    [Route("auth")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITokenServices _tokenService;
        private readonly IConfiguration _config;

        public AuthController(IUserService userService, ITokenServices tokenService, IConfiguration config)
        {
            _userService = userService;
            _tokenService = tokenService;
            _config = config;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] AuthRequest.LoginRequest req)
        {
            var user = await _userService.AuthenticateAsync(req.Username, req.Password);
            if (user == null) return Unauthorized();

            var accessExpiry = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpiresMinutes"] ?? "15"));
            var refreshExpiry = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpiresDays"] ?? "30"));

            var accessToken = _tokenService.CreateAccessToken(user);
            var refreshToken = _tokenService.CreateRefreshToken();

            await _userService.UpdateRefreshTokenAsync(user, refreshToken, refreshExpiry);

            return Ok(new AuthResponse.LoginResponse
            {
                UserID = user.UserID,
                Username = user.Username,
                Role = (int)user.Role,
                AccessToken = accessToken,
                RefreshToken = refreshToken,
                AccessTokenExpiresAt = accessExpiry,
                RefreshTokenExpiresAt = refreshExpiry
            });
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] AuthRequest.RegisterRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username) || string.IsNullOrWhiteSpace(req.Password) || string.IsNullOrEmpty(req.PhoneNum) || req.Role == 0)
            {
                return Ok(new AuthResponse.RegisterResponse
                {
                    Message = "All Credential must be filled - failed at 1st phase.",
                    Status = false
                });
            }

            var existing = await _userService.GetByUsernameAsync(req.Username);
            if (existing != null)
                return Ok(new AuthResponse.RegisterResponse
                {
                    Message = "Username already in use - failed at 2st phase.",
                    Status = false
                });

            var created = await _userService.RegisterAsync(req.Username, req.Password, req.PhoneNum, req.Email, req.Role);

            if (created == null)
            {
                return Ok(new AuthResponse.RegisterResponse
                {
                    Message = "Username, phone number or email already in use - failed at Creation phase.",
                    Status = false
                });
            }

            return Ok(new AuthResponse.RegisterResponse
            {
                Message = "Registration successful",
                Status = true
            });
        }

        [HttpPost("refresh2")]
        public async Task<IActionResult> Refresh2([FromBody] dynamic body)
        {
            string username = (string?)body?.username ?? string.Empty;
            string refreshToken = (string?)body?.refreshToken ?? string.Empty;
            if (string.IsNullOrWhiteSpace(username) || string.IsNullOrWhiteSpace(refreshToken)) return BadRequest();

            var user = await _userService.GetByUsernameAsync(username);
            if (user == null) return Unauthorized();

            if (user.RefreshToken != refreshToken || user.RefreshTokenExpiryTime == null || user.RefreshTokenExpiryTime <= DateTime.UtcNow)
                return Unauthorized();

            var newAccess = _tokenService.CreateAccessToken(user);
            var newRefresh = _tokenService.CreateRefreshToken();
            var expiry = DateTime.UtcNow.AddDays(int.Parse(_config["Jwt:RefreshTokenExpiresDays"] ?? "30"));

            await _userService.UpdateRefreshTokenAsync(user, newRefresh, expiry);

            return Ok(new AuthResponse.LoginResponse
            {
                UserID = user.UserID,
                Username = user.Username,
                Role = (int)user.Role,
                AccessToken = newAccess,
                RefreshToken = newRefresh,
                AccessTokenExpiresAt = DateTime.UtcNow.AddMinutes(int.Parse(_config["Jwt:AccessTokenExpiresMinutes"] ?? "15")),
                RefreshTokenExpiresAt = expiry
            });
        }

        [HttpPost("set-role")]
        public async Task<IActionResult> SetRole([FromBody] AuthRequest.SetRoleRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username)) return BadRequest();

            var user = await _userService.GetByUsernameAsync(req.Username);
            if (user == null) return NotFound();

            if (!Enum.IsDefined(typeof(UserRole), req.Role)) return BadRequest("Invalid role.");

            var newRole = (UserRole)req.Role;

            var ok = await _userService.UpdateRoleAsync(user, newRole, req.Extra ?? string.Empty);
            if (!ok) return StatusCode(500, "Failed to update role.");

            return Ok(new { message = "Role updated", role = newRole.ToString() });
        }

        [HttpPost("logout")]
        public async Task<IActionResult> Logout([FromBody] AuthRequest.LogoutRequest req)
        {
            if (req == null || string.IsNullOrWhiteSpace(req.Username)) return BadRequest();

            var user = await _userService.GetByUsernameAsync(req.Username);
            if (user == null) return NotFound();

            await _userService.LogoutAsync(user);
            return Ok(new { message = "Logged out" });
        }
    }
}