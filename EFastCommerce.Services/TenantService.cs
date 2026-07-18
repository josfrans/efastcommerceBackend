using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class TenantService : ITenantService
    {
        private readonly ITenantRepository _tenantRepository;

        public TenantService(ITenantRepository tenantRepository)
        {
            _tenantRepository = tenantRepository;
        }

        public async Task<Tenant?> GetTenantByIdAsync(Guid id)
        {
            return await _tenantRepository.GetByIdAsync(id);
        }

        public async Task<Tenant?> GetTenantBySlugAsync(string slug)
        {
            return await _tenantRepository.GetBySlugAsync(slug);
        }

        public async Task<Tenant?> GetTenantByNameAsync(string name)
        {
            return await _tenantRepository.GetByNameAsync(name);
        }

        public async Task<IEnumerable<Tenant>> GetAllTenantsAsync()
        {
            return await _tenantRepository.GetAllAsync();
        }

        public async Task<Tenant> CreateTenantAsync(Tenant tenant)
        {
            if (tenant.Id == Guid.Empty)
            {
                tenant.Id = Guid.NewGuid();
            }
            tenant.Slug = tenant.Slug.ToLower().Trim();
            tenant.CreatedAt = DateTime.UtcNow;
            
            await _tenantRepository.AddAsync(tenant);
            return tenant;
        }

        public async Task UpdateTenantAsync(Tenant tenant)
        {
            tenant.Slug = tenant.Slug.ToLower().Trim();
            await _tenantRepository.UpdateAsync(tenant);
        }
    }
}
