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
    public class StoreInvitationRepository : Repository<StoreInvitation>, IStoreInvitationRepository
    {
        public StoreInvitationRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<StoreInvitation?> GetByTokenAsync(string token)
        {
            // We ignore query filters using IgnoreQueryFilters() because during validation 
            // the active TenantId has not been resolved (it's validated publicly)
            return await _dbSet.IgnoreQueryFilters()
                               .Include(i => i.Tenant)
                               .FirstOrDefaultAsync(i => i.Token.ToLower() == token.ToLower());
        }

        public async Task<IEnumerable<StoreInvitation>> GetInvitationsByTenantAsync(Guid tenantId)
        {
            return await _dbSet.Where(i => i.TenantId == tenantId)
                               .Include(i => i.ReferrerUser)
                               .OrderByDescending(i => i.CreatedAt)
                               .ToListAsync();
        }
    }
}
