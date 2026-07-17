using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface IStoreInvitationRepository : IRepository<StoreInvitation>
    {
        Task<StoreInvitation?> GetByTokenAsync(string token);
        Task<IEnumerable<StoreInvitation>> GetInvitationsByTenantAsync(System.Guid tenantId);
    }
}
