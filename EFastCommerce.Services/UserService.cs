using System;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class UserService : IUserService
    {
        private readonly IUserRepository _userRepository;

        public UserService(IUserRepository userRepository)
        {
            _userRepository = userRepository;
        }

        public async Task<User?> AuthenticateAsync(string emailOrUsername, string password)
        {
            User? user = null;

            if (emailOrUsername.Contains("@"))
            {
                user = await _userRepository.GetByEmailAsync(emailOrUsername);
            }
            else
            {
                user = await _userRepository.GetByUsernameAsync(emailOrUsername);
            }

            if (user == null)
            {
                return null;
            }

            if (!VerifyPassword(password, user.PasswordHash))
            {
                return null;
            }

            return user;
        }

        public async Task<User> RegisterAsync(User user, string password)
        {
            // Check if email or username already exists
            var existingEmail = await _userRepository.GetByEmailAsync(user.Email);
            if (existingEmail != null)
            {
                throw new Exception("Email is already registered.");
            }

            var existingUsername = await _userRepository.GetByUsernameAsync(user.Username);
            if (existingUsername != null)
            {
                throw new Exception("Username is already taken.");
            }

            if (user.Id == Guid.Empty)
            {
                user.Id = Guid.NewGuid();
            }

            user.PasswordHash = HashPassword(password);
            user.CreatedAt = DateTime.UtcNow;

            await _userRepository.AddAsync(user);
            return user;
        }

        public async Task<User?> GetUserByIdAsync(Guid id)
        {
            return await _userRepository.GetByIdAsync(id);
        }

        public async Task<User?> GetUserWithTenantsAsync(Guid id)
        {
            return await _userRepository.GetUserWithTenantsAsync(id);
        }

        public async Task<User?> GetUserByEmailAsync(string email)
        {
            return await _userRepository.GetByEmailAsync(email);
        }

        public bool VerifyUserPassword(User user, string password)
        {
            return VerifyPassword(password, user.PasswordHash);
        }

        public async Task<string> GeneratePasswordResetCodeAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var code = new Random().Next(100000, 999999).ToString();
            user.PasswordResetCode = code;
            user.PasswordResetCodeExpiration = DateTime.UtcNow.AddMinutes(15);
            
            await _userRepository.UpdateAsync(user);
            return code;
        }

        public async Task<bool> ResetPasswordAsync(string email, string code, string newPassword)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null || user.PasswordResetCode != code || user.PasswordResetCodeExpiration < DateTime.UtcNow)
            {
                return false;
            }

            user.PasswordHash = HashPassword(newPassword);
            user.PasswordResetCode = null;
            user.PasswordResetCodeExpiration = null;

            await _userRepository.UpdateAsync(user);
            return true;
        }

        public async Task<string> GenerateEmailValidationTokenAsync(string email)
        {
            var user = await _userRepository.GetByEmailAsync(email);
            if (user == null)
            {
                throw new Exception("User not found.");
            }

            var token = Guid.NewGuid().ToString("N");
            user.EmailConfirmationToken = token;
            
            await _userRepository.UpdateAsync(user);
            return token;
        }

        public async Task<Guid?> ConfirmEmailAsync(string token)
        {
            var user = await _userRepository.GetUserByEmailConfirmationTokenAsync(token);
            if (user == null)
            {
                return null;
            }

            user.IsEmailConfirmed = true;
            user.EmailConfirmationToken = null;

            await _userRepository.UpdateAsync(user);
            return user.Id;
        }

        #region Password Hashing Helper (PBKDF2)

        private static string HashPassword(string password)
        {
            byte[] salt = RandomNumberGenerator.GetBytes(16);
            byte[] hash = Rfc2898DeriveBytes.Pbkdf2(
                password,
                salt,
                iterations: 100000,
                HashAlgorithmName.SHA256,
                outputLength: 32
            );
            return $"{Convert.ToBase64String(salt)}.{Convert.ToBase64String(hash)}";
        }

        private static bool VerifyPassword(string password, string hashedPassword)
        {
            try
            {
                var parts = hashedPassword.Split('.', 2);
                if (parts.Length != 2) return false;
                byte[] salt = Convert.FromBase64String(parts[0]);
                byte[] hash = Convert.FromBase64String(parts[1]);
                byte[] testHash = Rfc2898DeriveBytes.Pbkdf2(
                    password,
                    salt,
                    iterations: 100000,
                    HashAlgorithmName.SHA256,
                    outputLength: 32
                );
                return CryptographicOperations.FixedTimeEquals(hash, testHash);
            }
            catch
            {
                return false;
            }
        }

        #endregion
    }
}
