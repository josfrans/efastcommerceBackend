using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface IStoreReferralLinkRepository
    {
        Task<StoreReferralLink?> GetByIdAsync(Guid id);
        Task<StoreReferralLink?> GetByTokenAsync(string token);
        Task<IEnumerable<StoreReferralLink>> GetByTenantAndReferrerAsync(Guid tenantId, Guid referrerId);
        Task<IEnumerable<StoreReferralLink>> GetByTenantAsync(Guid tenantId);
        Task<StoreReferralLink> AddAsync(StoreReferralLink referralLink);
        Task UpdateAsync(StoreReferralLink referralLink);
        Task DeleteAsync(StoreReferralLink referralLink);
    }
}
