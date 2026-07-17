using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ProductsController : ControllerBase
    {
        private readonly IProductService _productService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ITenantService _tenantService;
        private readonly IVendorService _vendorService;

        public ProductsController(
            IProductService productService, 
            ITenantProvider tenantProvider,
            ITenantService tenantService,
            IVendorService vendorService)
        {
            _productService = productService;
            _tenantProvider = tenantProvider;
            _tenantService = tenantService;
            _vendorService = vendorService;
        }

        private async Task<bool> IsOwnerOrVendorAsync(Guid tenantId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                return false;

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant != null && tenant.OwnerId == userId)
                return true;

            var vendor = await _vendorService.GetVendorAsync(tenantId, userId);
            if (vendor != null && vendor.Status == "Approved")
                return true;

            return false;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Product>>> GetProducts([FromQuery] Guid? tenantId)
        {
            Guid resolvedTenantId;
            
            if (tenantId.HasValue)
            {
                resolvedTenantId = tenantId.Value;
            }
            else if (_tenantProvider.TenantId.HasValue)
            {
                resolvedTenantId = _tenantProvider.TenantId.Value;
            }
            else
            {
                return BadRequest("Tenant context is required to list products. Use 'X-Tenant-Slug' or 'X-Tenant-Id' header, or pass tenantId query param.");
            }

            var products = await _productService.GetProductsByTenantAsync(resolvedTenantId);
            return Ok(products);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Product>> GetById(Guid id)
        {
            var product = await _productService.GetProductByIdAsync(id);
            if (product == null)
            {
                return NotFound();
            }
            return Ok(product);
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Product>> Create([FromBody] Product product)
        {
            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;

            if (!await IsOwnerOrVendorAsync(tenantId))
            {
                return StatusCode(403, new { Error = "You are not authorized to create products for this store." });
            }

            product.TenantId = tenantId;

            var createdProduct = await _productService.CreateProductAsync(product);
            return CreatedAtAction(nameof(GetById), new { id = createdProduct.Id }, createdProduct);
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] Product productUpdate)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            if (!await IsOwnerOrVendorAsync(existingProduct.TenantId))
            {
                return StatusCode(403, new { Error = "You are not authorized to modify products for this store." });
            }

            existingProduct.Name = productUpdate.Name;
            existingProduct.Description = productUpdate.Description;
            existingProduct.Price = productUpdate.Price;
            existingProduct.ImageUrl = productUpdate.ImageUrl;
            existingProduct.Stock = productUpdate.Stock;
            existingProduct.IsActive = productUpdate.IsActive;
            existingProduct.MeasurementUnit = productUpdate.MeasurementUnit;
            existingProduct.Size = productUpdate.Size;

            await _productService.UpdateProductAsync(existingProduct);

            return NoContent();
        }

        [HttpDelete("{id}")]
        [Authorize]
        public async Task<IActionResult> Delete(Guid id)
        {
            var existingProduct = await _productService.GetProductByIdAsync(id);
            if (existingProduct == null)
            {
                return NotFound();
            }

            if (!await IsOwnerOrVendorAsync(existingProduct.TenantId))
            {
                return StatusCode(403, new { Error = "You are not authorized to delete products for this store." });
            }

            await _productService.DeleteProductAsync(id);
            return NoContent();
        }
    }
}
