using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Infrastructure.Data;

namespace EFastCommerce.Infrastructure.Repositories
{
    public class TenantVendorRepository : Repository<TenantVendor>, ITenantVendorRepository
    {
        public TenantVendorRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<TenantVendor>> GetVendorsByTenantAsync(Guid tenantId)
        {
            return await _dbSet
                .Include(tv => tv.User)
                .Where(tv => tv.TenantId == tenantId)
                .ToListAsync();
        }

        public async Task<TenantVendor?> GetVendorAsync(Guid tenantId, Guid userId)
        {
            return await _dbSet
                .Include(tv => tv.User)
                .FirstOrDefaultAsync(tv => tv.TenantId == tenantId && tv.UserId == userId);
        }
    }
}
