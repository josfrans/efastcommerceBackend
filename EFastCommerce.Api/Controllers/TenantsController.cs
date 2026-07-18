using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class TenantsController : ControllerBase
    {
        private readonly ITenantService _tenantService;

        public TenantsController(ITenantService tenantService)
        {
            _tenantService = tenantService;
        }

        [HttpGet]
        public async Task<ActionResult<IEnumerable<Tenant>>> GetAll()
        {
            var tenants = await _tenantService.GetAllTenantsAsync();
            return Ok(tenants);
        }

        [HttpGet("{id}")]
        public async Task<ActionResult<Tenant>> GetById(Guid id)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(id);
            if (tenant == null)
            {
                return NotFound();
            }
            return Ok(tenant);
        }

        [HttpGet("slug/{slug}")]
        public async Task<ActionResult<Tenant>> GetBySlug(string slug)
        {
            var tenant = await _tenantService.GetTenantBySlugAsync(slug);
            if (tenant == null)
            {
                return NotFound();
            }
            return Ok(tenant);
        }

        [HttpGet("check-availability")]
        public async Task<ActionResult> CheckAvailability([FromQuery] string type, [FromQuery] string value)
        {
            if (string.IsNullOrWhiteSpace(type) || string.IsNullOrWhiteSpace(value))
            {
                return BadRequest(new { Error = "Type and value are required." });
            }

            Tenant? existingTenant = null;

            if (type.ToLower() == "slug")
            {
                existingTenant = await _tenantService.GetTenantBySlugAsync(value);
            }
            else if (type.ToLower() == "name")
            {
                existingTenant = await _tenantService.GetTenantByNameAsync(value);
            }
            else
            {
                return BadRequest(new { Error = "Invalid type. Must be 'slug' or 'name'." });
            }

            return Ok(new { available = existingTenant == null });
        }

        [HttpPut("{id}")]
        [Authorize]
        public async Task<IActionResult> Update(Guid id, [FromBody] Tenant tenantUpdate)
        {
            // Security: Ensure the user is actually the Owner (or has access to this tenant)
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return StatusCode(403, new { Error = "You can only modify your own store details." });
            }

            var existingTenant = await _tenantService.GetTenantByIdAsync(id);
            if (existingTenant == null)
            {
                return NotFound();
            }

            // Check if the user is the Owner
            if (existingTenant.OwnerId != userId)
            {
                return StatusCode(403, new { Error = "You can only modify your own store details." });
            }

            existingTenant.Name = tenantUpdate.Name;
            existingTenant.LogoUrl = tenantUpdate.LogoUrl;
            existingTenant.ThemeColor = tenantUpdate.ThemeColor;

            await _tenantService.UpdateTenantAsync(existingTenant);

            return NoContent();
        }

        public class CreateTenantRequest
        {
            public string CompanyName { get; set; } = string.Empty;
            public string CompanySlug { get; set; } = string.Empty;
        }

        [HttpPost]
        [Authorize]
        public async Task<IActionResult> Create([FromBody] CreateTenantRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.CompanySlug))
            {
                return BadRequest(new { Error = "InvalidData", Message = "El nombre y la URL de la tienda son obligatorios." });
            }

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
            {
                return Unauthorized(new { Error = "Unauthorized", Message = "Usuario no autorizado." });
            }

            var slug = request.CompanySlug.ToLower().Trim();
            
            // Check availability
            var existingTenant = await _tenantService.GetTenantBySlugAsync(slug);
            if (existingTenant != null)
            {
                return BadRequest(new { Error = "SlugInUse", Message = "La URL de la tienda ya está en uso." });
            }

            var newTenant = new Tenant
            {
                Name = request.CompanyName,
                Slug = slug,
                IsActive = true, // Born active because user is already authenticated and verified
                OwnerId = userId
            };

            var createdTenant = await _tenantService.CreateTenantAsync(newTenant);
            return Ok(createdTenant);
        }
    }
}
