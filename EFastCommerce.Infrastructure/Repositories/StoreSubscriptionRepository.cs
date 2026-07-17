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
    public class StoreSubscriptionRepository : Repository<StoreSubscription>, IStoreSubscriptionRepository
    {
        public StoreSubscriptionRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<StoreSubscription?> GetSubscriptionAsync(Guid tenantId, Guid userId)
        {
            // Ignore filters to check subscription globally if needed, 
            // though normally the resolved tenant filter applies. We use IgnoreQueryFilters for flexibility.
            return await _dbSet.IgnoreQueryFilters()
                               .FirstOrDefaultAsync(s => s.TenantId == tenantId && s.UserId == userId);
        }

        public async Task<IEnumerable<StoreSubscription>> GetSubscriptionsByTenantAsync(Guid tenantId)
        {
            return await _dbSet.IgnoreQueryFilters()
                               .Include(s => s.User)
                               .Include(s => s.ReferredByUser)
                               .Where(s => s.TenantId == tenantId)
                               .OrderByDescending(s => s.SubscribedAt)
                               .ToListAsync();
        }

        public async Task<IEnumerable<StoreSubscription>> GetSubscriptionsByUserAsync(Guid userId)
        {
            return await _dbSet.IgnoreQueryFilters()
                               .Include(s => s.Tenant)
                               .Where(s => s.UserId == userId)
                               .OrderByDescending(s => s.SubscribedAt)
                               .ToListAsync();
        }
    }
}
