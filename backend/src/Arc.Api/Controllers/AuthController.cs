using Arc.Api.Common;
using Arc.Api.DTOs.Auth;
using Arc.Domain.Models;
using Arc.Domain.Exceptions;
using Arc.Application.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;


namespace Arc.Api.Controllers;
/// <summary>
/// Authentication controller that handles user registration, login, and profile operations.
/// Implements secure authentication flows with proper error handling and logging.
/// </summary>
[ApiController]
[Route("api/auth")]
public sealed class AuthController : ControllerBase
{
    private readonly IAuthenticationService _authenticationService;
    private readonly IJwtTokenService _jwtTokenService;
    private readonly IUserContext _userContext;
    private readonly ILogger<AuthController> _logger;
    private readonly IPasswordResetRepository _passwordResetRepository;
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IEmailService _emailService;

    public AuthController(
        IAuthenticationService authenticationService,
        IJwtTokenService jwtTokenService,
        IUserContext userContext,
        ILogger<AuthController> logger,
        IPasswordResetRepository passwordResetRepository,
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IEmailService emailService)
    {
        _authenticationService = authenticationService ?? throw new ArgumentNullException(nameof(authenticationService));
        _jwtTokenService = jwtTokenService ?? throw new ArgumentNullException(nameof(jwtTokenService));
        _userContext = userContext ?? throw new ArgumentNullException(nameof(userContext));
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _passwordResetRepository = passwordResetRepository ?? throw new ArgumentNullException(nameof(passwordResetRepository));
        _userRepository = userRepository ?? throw new ArgumentNullException(nameof(userRepository));
        _passwordHashingService = passwordHashingService ?? throw new ArgumentNullException(nameof(passwordHashingService));
        _emailService = emailService ?? throw new ArgumentNullException(nameof(emailService));
    }

    /// <summary>
    /// Registers a new user account.
    /// </summary>
    /// <param name="request">Registration request containing username, email, password, and optional role</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    [HttpPost("register")]
    public async Task<ActionResult<AuthResponseDto>> Register([FromBody] RegisterRequestDto request)
    {
        try
        {
            var role = string.IsNullOrWhiteSpace(request.Role) 
                ? UserRole.User 
                : UserRoleExtensions.FromRoleString(request.Role);

            var user = await _authenticationService.RegisterAsync(request.Username, request.Email, request.Password, role);
            var token = _jwtTokenService.GenerateToken(user);

            Response.Cookies.Append(
                CookieSettings.CookieName,
                token,
                CookieSettings.GetCookieOptions(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
            );

            var response = new AuthResponseDto
            {
                User = MapToUserDto(user)
            };

            _logger.LogInformation("User registered successfully: {UserId}, Username: {Username}, Email: {Email}", 
                user.Id, user.Username, user.Email);
            return Ok(response);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Registration failed: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid registration request: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Authenticates a user with email and password.
    /// </summary>
    /// <param name="request">Login request containing email and password</param>
    /// <returns>Authentication response with JWT token and user information</returns>
    [HttpPost("login")]
    public async Task<ActionResult<AuthResponseDto>> Login([FromBody] LoginRequestDto request)
    {
        try
        {
            var user = await _authenticationService.AuthenticateAsync(request.Email, request.Password);
            var token = _jwtTokenService.GenerateToken(user);

            Response.Cookies.Append(
                CookieSettings.CookieName,
                token,
                CookieSettings.GetCookieOptions(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
            );

            var response = new AuthResponseDto
            {
                User = MapToUserDto(user)
            };

            _logger.LogInformation("User logged in successfully: {UserId}", user.Id);
            return Ok(response);
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Login failed for email {Email}: {Message}", request.Email, ex.Message);
            return Unauthorized(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid login request: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
    }

    /// <summary>
    /// Gets the current authenticated user's profile information.
    /// </summary>
    /// <returns>Current user information</returns>
    [HttpGet("me")]
    [Authorize]
    public async Task<ActionResult<UserDto>> GetCurrentUser()
    {
        try
        {
            var currentUserId = _userContext.CurrentUserId;
            
            if (currentUserId == UserId.Anonymous)
            {
                _logger.LogWarning("Unauthorized access attempt to /me endpoint");
                return Unauthorized(new { Message = "Authentication required" });
            }

            var user = await _authenticationService.GetUserByIdAsync(currentUserId);
            if (user == null)
            {
                _logger.LogWarning("User not found for authenticated request: {UserId}", currentUserId);
                return NotFound(new { Message = "User not found" });
            }

            if (!user.IsActive)
            {
                _logger.LogWarning("Inactive user attempted to access profile: {UserId}", currentUserId);
                return Unauthorized(new { Message = "Account is inactive" });
            }

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error retrieving current user profile");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Updates the current authenticated user's profile.
    /// </summary>
    /// <param name="request">Profile update request containing new username and email</param>
    /// <returns>Updated user information</returns>
    [HttpPut("profile")]
    [Authorize]
    public async Task<ActionResult<UserDto>> UpdateProfile([FromBody] UpdateProfileRequestDto request)
    {
        try
        {
            var currentUserId = _userContext.CurrentUserId;
            
            if (currentUserId == UserId.Anonymous)
            {
                _logger.LogWarning("Unauthorized profile update attempt");
                return Unauthorized(new { Message = "Authentication required" });
            }

            var updatedUser = await _authenticationService.UpdateProfileAsync(currentUserId, request.Username, request.Email, request.Firstname);
            
            _logger.LogInformation("Profile updated successfully for user: {UserId}", currentUserId);
            return Ok(MapToUserDto(updatedUser));
        }
        catch (AuthenticationException ex)
        {
            _logger.LogWarning("Profile update failed: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (ArgumentException ex)
        {
            _logger.LogWarning("Invalid profile update request: {Message}", ex.Message);
            return BadRequest(new { Message = ex.Message });
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error updating user profile");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Refreshes the auth token for the currently authenticated user.
    /// Resets the cookie and JWT expiry, keeping active sessions alive.
    /// </summary>
    [HttpPost("refresh")]
    [Authorize]
    public async Task<ActionResult<UserDto>> Refresh()
    {
        try
        {
            var currentUserId = _userContext.CurrentUserId;
            if (currentUserId == UserId.Anonymous)
                return Unauthorized(new { Message = "Authentication required" });

            var user = await _authenticationService.GetUserByIdAsync(currentUserId);
            if (user == null || !user.IsActive)
                return Unauthorized(new { Message = "Account is inactive or not found" });

            var token = _jwtTokenService.GenerateToken(user);
            Response.Cookies.Append(
                CookieSettings.CookieName,
                token,
                CookieSettings.GetCookieOptions(HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction())
            );

            return Ok(MapToUserDto(user));
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error refreshing token");
            return StatusCode(500, new { Message = "Internal server error" });
        }
    }

    /// <summary>
    /// Logs out the current user by clearing the authentication cookie.
    /// </summary>
    /// <returns>No content</returns>
    [HttpPost("logout")]
    public IActionResult Logout()
    {
        Response.Cookies.Delete(
            CookieSettings.CookieName,
            new CookieOptions
            {
                HttpOnly = true,
                Secure = HttpContext.RequestServices.GetRequiredService<IWebHostEnvironment>().IsProduction(),
                SameSite = SameSiteMode.Lax,
                Path = "/"
            }
        );
        
        _logger.LogInformation("User logged out successfully");
        return NoContent();
    }

    /// <summary>
    /// Initiates password reset by sending reset link to email.
    /// </summary>
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Email))
            return BadRequest(new { message = "Email is required" });

        var user = await _userRepository.GetByEmailAsync(request.Email);
        
        if (user != null && user.IsActive)
        {
            var token = Convert.ToBase64String(System.Security.Cryptography.RandomNumberGenerator.GetBytes(32));
            var resetToken = PasswordResetToken.Create(user.Id, token);
            
            await _passwordResetRepository.CreateAsync(resetToken);
            
            var resetLink = $"http://localhost:5173/reset/{token}";
            await _emailService.SendPasswordResetEmailAsync(user.Email, resetLink);
        }
        
        return Ok(new { message = "If an account exists, a reset link has been sent" });
    }

    /// <summary>
    /// Resets password using token from email.
    /// </summary>
    [HttpPost("reset-password")]
    public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Token))
            return BadRequest(new { message = "Token is required" });
        
        if (string.IsNullOrWhiteSpace(request.NewPassword))
            return BadRequest(new { message = "Password is required" });

        var resetToken = await _passwordResetRepository.GetByTokenAsync(request.Token);
        
        if (resetToken == null || !resetToken.IsValid())
            return BadRequest(new { message = "Invalid or expired token" });

        var user = await _userRepository.GetByIdAsync(resetToken.UserId);
        if (user == null)
            return BadRequest(new { message = "User not found" });

        var passwordHash = _passwordHashingService.HashPassword(request.NewPassword);
        user.UpdatePassword(passwordHash);
        await _userRepository.UpdateAsync(user);

        var usedToken = resetToken.MarkAsUsed();
        await _passwordResetRepository.UpdateAsync(usedToken);

        _logger.LogInformation("Password reset completed for user {UserId}", user.Id);
        return Ok(new { message = "Password has been reset successfully" });
    }

    private static UserDto MapToUserDto(User user)
    {
        return new UserDto
        {
            Id = user.Id.Value.ToString(),
            Username = user.Username,
            Email = user.Email,
            Role = user.Role.ToRoleString(),
            Firstname = user.Firstname,
            CreatedAt = user.CreatedAt,
            IsActive = user.IsActive
        };
    }
}