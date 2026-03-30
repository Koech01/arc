using Arc.Domain.Models;

namespace Arc.Application.Identity;

public interface IPasswordResetRepository
{
    Task<PasswordResetToken?> GetByTokenAsync(string token);
    Task CreateAsync(PasswordResetToken resetToken);
    Task UpdateAsync(PasswordResetToken resetToken);
    Task DeleteExpiredTokensAsync();
}