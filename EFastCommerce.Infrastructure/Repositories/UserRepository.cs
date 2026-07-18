using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Infrastructure.Data;

namespace EFastCommerce.Infrastructure.Repositories
{
    public class UserRepository : Repository<User>, IUserRepository
    {
        public UserRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<User?> GetByEmailAsync(string email)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        }

        public async Task<User?> GetUserByEmailConfirmationTokenAsync(string token)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.EmailConfirmationToken == token);
        }

        public async Task<User?> GetByUsernameAsync(string username)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Username.ToLower() == username.ToLower());
        }

        public override async Task<User?> GetByIdAsync(System.Guid id)
        {
            return await _dbSet.FirstOrDefaultAsync(u => u.Id == id);
        }

        public async Task<User?> GetUserWithTenantsAsync(System.Guid id)
        {
            return await _dbSet.Include(u => u.OwnedTenants)
                               .Include(u => u.TenantVendors)
                                   .ThenInclude(tv => tv.Tenant)
                               .Include(u => u.StoreSubscriptions)
                                   .ThenInclude(ss => ss.Tenant)
                               .FirstOrDefaultAsync(u => u.Id == id);
        }
    }
}
