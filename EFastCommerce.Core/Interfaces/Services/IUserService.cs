using System;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface IUserService
    {
        Task<User?> AuthenticateAsync(string emailOrUsername, string password);
        Task<User> RegisterAsync(User user, string password);
        Task<User?> GetUserByIdAsync(Guid id);
        Task<User?> GetUserWithTenantsAsync(Guid id);
        Task<User?> GetUserByEmailAsync(string email);
        Task<string> GeneratePasswordResetCodeAsync(string email);
        Task<bool> ResetPasswordAsync(string email, string code, string newPassword);
        bool VerifyUserPassword(User user, string password);
    }
}
