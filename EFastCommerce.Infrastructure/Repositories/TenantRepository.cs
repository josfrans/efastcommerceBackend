using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Infrastructure.Data;

namespace EFastCommerce.Infrastructure.Repositories
{
    public class TenantRepository : Repository<Tenant>, ITenantRepository
    {
        public TenantRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<Tenant?> GetBySlugAsync(string slug)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Slug.ToLower() == slug.ToLower());
        }

        public async Task<Tenant?> GetByNameAsync(string name)
        {
            return await _dbSet.FirstOrDefaultAsync(t => t.Name.ToLower() == name.ToLower());
        }
    }
}
