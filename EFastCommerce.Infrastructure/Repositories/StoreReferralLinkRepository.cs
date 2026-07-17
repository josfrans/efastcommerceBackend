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
    public class StoreReferralLinkRepository : IStoreReferralLinkRepository
    {
        private readonly EFastCommerceContext _context;

        public StoreReferralLinkRepository(EFastCommerceContext context)
        {
            _context = context;
        }

        public async Task<StoreReferralLink?> GetByIdAsync(Guid id)
        {
            return await _context.StoreReferralLinks
                .Include(r => r.Tenant)
                .Include(r => r.ReferrerUser)
                .FirstOrDefaultAsync(r => r.Id == id);
        }

        public async Task<StoreReferralLink?> GetByTokenAsync(string token)
        {
            return await _context.StoreReferralLinks
                .Include(r => r.Tenant)
                .Include(r => r.ReferrerUser)
                .FirstOrDefaultAsync(r => r.Token == token);
        }

        public async Task<IEnumerable<StoreReferralLink>> GetByTenantAndReferrerAsync(Guid tenantId, Guid referrerId)
        {
            return await _context.StoreReferralLinks
                .Where(r => r.TenantId == tenantId && r.ReferrerUserId == referrerId)
                .ToListAsync();
        }

        public async Task<IEnumerable<StoreReferralLink>> GetByTenantAsync(Guid tenantId)
        {
            return await _context.StoreReferralLinks
                .Include(r => r.ReferrerUser)
                .Where(r => r.TenantId == tenantId)
                .OrderByDescending(r => r.CreatedAt)
                .ToListAsync();
        }

        public async Task<StoreReferralLink> AddAsync(StoreReferralLink referralLink)
        {
            await _context.StoreReferralLinks.AddAsync(referralLink);
            await _context.SaveChangesAsync();
            return referralLink;
        }

        public async Task UpdateAsync(StoreReferralLink referralLink)
        {
            _context.StoreReferralLinks.Update(referralLink);
            await _context.SaveChangesAsync();
        }

        public async Task DeleteAsync(StoreReferralLink referralLink)
        {
            _context.StoreReferralLinks.Remove(referralLink);
            await _context.SaveChangesAsync();
        }
    }
}
