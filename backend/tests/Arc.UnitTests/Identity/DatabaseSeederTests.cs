using NSubstitute;
using Arc.Domain.Models;
using Arc.Application.Identity;
namespace Arc.UnitTests.Identity;
using Arc.Infrastructure.Identity;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Configuration;


public sealed class DatabaseSeederTests
{
    [Fact]
    public async Task SeedAsync_WhenAdminDoesNotExist_CreatesAdminAccount()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHashingService = Substitute.For<IPasswordHashingService>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<DatabaseSeeder>>();

        configuration["AdminAccount:Enabled"].Returns("true");
        configuration["AdminAccount:Email"].Returns("admin@arc.com");
        configuration["AdminAccount:Username"].Returns("admin");
        configuration["AdminAccount:Password"].Returns("Admin123!");
        
        userRepository.GetByEmailAsync("admin@arc.com").Returns(Task.FromResult<User?>(null));
        passwordHashingService.HashPassword("Admin123!").Returns("hashed_password");

        var seeder = new DatabaseSeeder(userRepository, passwordHashingService, configuration, logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        await userRepository.Received(1).CreateAsync(Arg.Is<User>(u => 
            u.Email == "admin@arc.com" && 
            u.Username == "admin" &&
            u.Role == UserRole.Admin));
    }

    [Fact]
    public async Task SeedAsync_WhenAdminExists_DoesNotCreateDuplicate()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHashingService = Substitute.For<IPasswordHashingService>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<DatabaseSeeder>>();

        configuration["AdminAccount:Enabled"].Returns("true");
        configuration["AdminAccount:Email"].Returns("admin@arc.com");

        var existingAdmin = User.Create(
            "admin",
            "admin@arc.com",
            "hashed_password",
            UserRole.Admin
        );
        userRepository.GetByEmailAsync("admin@arc.com").Returns(Task.FromResult<User?>(existingAdmin));

        var seeder = new DatabaseSeeder(userRepository, passwordHashingService, configuration, logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        await userRepository.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task SeedAsync_WhenDisabled_DoesNotCreateAdmin()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHashingService = Substitute.For<IPasswordHashingService>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<DatabaseSeeder>>();

        configuration["AdminAccount:Enabled"].Returns("false");

        var seeder = new DatabaseSeeder(userRepository, passwordHashingService, configuration, logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        await userRepository.DidNotReceive().GetByEmailAsync(Arg.Any<string>());
        await userRepository.DidNotReceive().CreateAsync(Arg.Any<User>());
    }

    [Fact]
    public async Task SeedAsync_UsesDefaultValues_WhenConfigurationMissing()
    {
        // Arrange
        var userRepository = Substitute.For<IUserRepository>();
        var passwordHashingService = Substitute.For<IPasswordHashingService>();
        var configuration = Substitute.For<IConfiguration>();
        var logger = Substitute.For<ILogger<DatabaseSeeder>>();

        configuration["AdminAccount:Enabled"].Returns("true");
        configuration["AdminAccount:Email"].Returns((string?)null);
        configuration["AdminAccount:Username"].Returns((string?)null);
        configuration["AdminAccount:Password"].Returns((string?)null);

        userRepository.GetByEmailAsync("admin@arc.com").Returns(Task.FromResult<User?>(null));
        passwordHashingService.HashPassword("Admin123!").Returns("hashed_password");

        var seeder = new DatabaseSeeder(userRepository, passwordHashingService, configuration, logger);

        // Act
        await seeder.SeedAsync();

        // Assert
        await userRepository.Received(1).CreateAsync(Arg.Is<User>(u => 
            u.Email == "admin@arc.com" && 
            u.Username == "admin"));
    }
}