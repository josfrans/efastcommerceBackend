using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface IVendorService
    {
        Task<IEnumerable<TenantVendor>> GetVendorsByTenantAsync(Guid tenantId);
        Task ApproveVendorAsync(Guid tenantId, Guid userId);
        Task RevokeVendorAsync(Guid tenantId, Guid userId);
        Task<TenantVendor?> GetVendorAsync(Guid tenantId, Guid userId);
        Task AddVendorAsync(Guid tenantId, Guid userId);
    }
}
