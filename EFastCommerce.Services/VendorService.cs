using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class VendorService : IVendorService
    {
        private readonly ITenantVendorRepository _vendorRepository;

        public VendorService(ITenantVendorRepository vendorRepository)
        {
            _vendorRepository = vendorRepository;
        }

        public async Task<IEnumerable<TenantVendor>> GetVendorsByTenantAsync(Guid tenantId)
        {
            return await _vendorRepository.GetVendorsByTenantAsync(tenantId);
        }

        public async Task<TenantVendor?> GetVendorAsync(Guid tenantId, Guid userId)
        {
            return await _vendorRepository.GetVendorAsync(tenantId, userId);
        }

        public async Task ApproveVendorAsync(Guid tenantId, Guid userId)
        {
            var vendor = await _vendorRepository.GetVendorAsync(tenantId, userId);
            if (vendor == null)
            {
                throw new Exception("Vendor not found.");
            }

            vendor.Status = "Approved";
            vendor.ApprovedAt = DateTime.UtcNow;

            await _vendorRepository.UpdateAsync(vendor);
        }

        public async Task RevokeVendorAsync(Guid tenantId, Guid userId)
        {
            var vendor = await _vendorRepository.GetVendorAsync(tenantId, userId);
            if (vendor == null)
            {
                throw new Exception("Vendor not found.");
            }

            vendor.Status = "Revoked";
            // Do not clear ApprovedAt to maintain audit trail

            await _vendorRepository.UpdateAsync(vendor);
        }

        public async Task AddVendorAsync(Guid tenantId, Guid userId)
        {
            var existingVendor = await _vendorRepository.GetVendorAsync(tenantId, userId);
            if (existingVendor != null)
            {
                // If exists, make sure it is Approved
                if (existingVendor.Status != "Approved")
                {
                    existingVendor.Status = "Approved";
                    existingVendor.ApprovedAt = DateTime.UtcNow;
                    await _vendorRepository.UpdateAsync(existingVendor);
                }
                return;
            }

            var newVendor = new TenantVendor
            {
                Id = Guid.NewGuid(),
                TenantId = tenantId,
                UserId = userId,
                Status = "Approved",
                CreatedAt = DateTime.UtcNow,
                ApprovedAt = DateTime.UtcNow
            };

            await _vendorRepository.AddAsync(newVendor);
        }
    }
}
