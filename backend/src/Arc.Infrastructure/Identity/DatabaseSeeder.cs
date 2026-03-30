using Arc.Domain.Models;
using Arc.Application.Identity;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Identity;
using Microsoft.Extensions.Configuration;


/// <summary>
/// Seeds initial admin account on application startup.
/// </summary>
public sealed class DatabaseSeeder
{
    private readonly IUserRepository _userRepository;
    private readonly IPasswordHashingService _passwordHashingService;
    private readonly IConfiguration _configuration;
    private readonly ILogger<DatabaseSeeder> _logger;

    public DatabaseSeeder(
        IUserRepository userRepository,
        IPasswordHashingService passwordHashingService,
        IConfiguration configuration,
        ILogger<DatabaseSeeder> logger)
    {
        _userRepository = userRepository;
        _passwordHashingService = passwordHashingService;
        _configuration = configuration;
        _logger = logger;
    }

    public async Task SeedAsync(CancellationToken cancellationToken = default)
    {
        var enabledStr = _configuration["AdminAccount:Enabled"];
        var enabled = string.IsNullOrEmpty(enabledStr) || bool.Parse(enabledStr);
        
        if (!enabled)
        {
            _logger.LogInformation("Admin account seeding is disabled");
            return;
        }

        var email = _configuration["AdminAccount:Email"] ?? "admin@arc.com";
        var existingAdmin = await _userRepository.GetByEmailAsync(email);

        if (existingAdmin != null)
        {
            _logger.LogInformation("Admin account already exists: {Email}", email);
            return;
        }

        var username = _configuration["AdminAccount:Username"] ?? "admin";
        var password = _configuration["AdminAccount:Password"] ?? "Admin123!";
        var passwordHash = _passwordHashingService.HashPassword(password);

        var admin = User.Create(
            username,
            email,
            passwordHash,
            UserRole.Admin
        );

        await _userRepository.CreateAsync(admin);
        _logger.LogWarning("Admin account created: {Email} - CHANGE PASSWORD IMMEDIATELY", email);
    }
}