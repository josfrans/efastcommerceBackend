using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface ITenantVendorRepository : IRepository<TenantVendor>
    {
        Task<IEnumerable<TenantVendor>> GetVendorsByTenantAsync(Guid tenantId);
        Task<TenantVendor?> GetVendorAsync(Guid tenantId, Guid userId);
    }
}
