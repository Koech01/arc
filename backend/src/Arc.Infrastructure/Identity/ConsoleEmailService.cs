using Arc.Application.Identity;
using Microsoft.Extensions.Logging;
namespace Arc.Infrastructure.Identity;


public sealed class ConsoleEmailService : IEmailService
{
    private readonly ILogger<ConsoleEmailService> _logger;

    public ConsoleEmailService(ILogger<ConsoleEmailService> logger)
    {
        _logger = logger;
    }

    public Task SendPasswordResetEmailAsync(string email, string resetLink)
    {
        _logger.LogInformation(
            "Password reset email for {Email}:\n" +
            "Subject: Reset your Arc password\n" +
            "Link: {ResetLink}\n" +
            "Expires in 15 minutes",
            email,
            resetLink
        );
        
        return Task.CompletedTask;
    }
}
