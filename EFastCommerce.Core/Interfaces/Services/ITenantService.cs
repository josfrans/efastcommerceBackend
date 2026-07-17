using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface ITenantService
    {
        Task<Tenant?> GetTenantByIdAsync(Guid id);
        Task<Tenant?> GetTenantBySlugAsync(string slug);
        Task<IEnumerable<Tenant>> GetAllTenantsAsync();
        Task<Tenant> CreateTenantAsync(Tenant tenant);
        Task UpdateTenantAsync(Tenant tenant);
    }
}
