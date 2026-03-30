namespace Arc.Application.Identity;

public interface IEmailService
{
    Task SendPasswordResetEmailAsync(string email, string resetLink);
}