using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface IReferralService
    {
        Task<StoreReferralLink> GenerateReferralAsync(Guid tenantId, Guid referrerId, int expiresHours);
        Task<StoreReferralLink?> ValidateTokenAsync(string token);
        Task<StoreSubscription> SubscribeReferredUserAsync(Guid userId, string token);
        Task<IEnumerable<StoreReferralLink>> GetReferralsByTenantAsync(Guid tenantId);
    }
}
