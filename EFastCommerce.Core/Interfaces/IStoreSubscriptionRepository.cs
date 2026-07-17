using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface IStoreSubscriptionRepository : IRepository<StoreSubscription>
    {
        Task<StoreSubscription?> GetSubscriptionAsync(Guid tenantId, Guid userId);
        Task<IEnumerable<StoreSubscription>> GetSubscriptionsByTenantAsync(Guid tenantId);
        Task<IEnumerable<StoreSubscription>> GetSubscriptionsByUserAsync(Guid userId);
    }
}
