using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface IInvitationService
    {
        Task<StoreInvitation> GenerateInvitationAsync(Guid tenantId, int expiresHours);
        Task<IEnumerable<StoreInvitation>> GetInvitationsByTenantAsync(Guid tenantId);
        Task<bool> DeleteInvitationAsync(Guid id, Guid tenantId);
        Task<StoreInvitation?> ValidateTokenAsync(string token);
        Task<IEnumerable<StoreSubscription>> GetCustomersByTenantAsync(Guid tenantId);
        Task<bool> RemoveCustomerAsync(Guid userId, Guid tenantId);
        Task<StoreSubscription> SubscribeUserAsync(Guid userId, string token);
        Task<bool> IsUserSubscribedAsync(Guid userId, Guid tenantId);
    }
}
