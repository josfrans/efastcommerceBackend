using System;
using System.Linq;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Controllers
{
    [Authorize]
    [ApiController]
    [Route("api/vendors")]
    public class VendorManagementController : ControllerBase
    {
        private readonly IVendorService _vendorService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ITenantService _tenantService;
        private readonly IUserService _userService;

        public VendorManagementController(IVendorService vendorService, ITenantProvider tenantProvider, ITenantService tenantService, IUserService userService)
        {
            _vendorService = vendorService;
            _tenantProvider = tenantProvider;
            _tenantService = tenantService;
            _userService = userService;
        }

        private async Task<bool> IsUserTenantOwnerAsync()
        {
            if (!_tenantProvider.TenantId.HasValue)
                return false;

            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdClaim) || !Guid.TryParse(userIdClaim, out var userId))
                return false;

            var tenant = await _tenantService.GetTenantByIdAsync(_tenantProvider.TenantId.Value);
            return tenant != null && tenant.OwnerId == userId;
        }

        [HttpGet]
        public async Task<IActionResult> GetVendors()
        {
            if (!await IsUserTenantOwnerAsync())
            {
                return Forbid("You are not the owner of this tenant.");
            }

            var vendors = await _vendorService.GetVendorsByTenantAsync(_tenantProvider.TenantId!.Value);
            
            var result = vendors.Select(v => new
            {
                v.Id,
                v.UserId,
                Username = v.User?.Username,
                Email = v.User?.Email,
                v.Status,
                v.CreatedAt,
                v.ApprovedAt
            });

            return Ok(result);
        }

        [HttpPost("{userId}/approve")]
        public async Task<IActionResult> ApproveVendor(Guid userId)
        {
            if (!await IsUserTenantOwnerAsync())
            {
                return Forbid("You are not the owner of this tenant.");
            }

            try
            {
                await _vendorService.ApproveVendorAsync(_tenantProvider.TenantId!.Value, userId);
                return Ok(new { Message = "Vendor approved successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("{userId}/revoke")]
        public async Task<IActionResult> RevokeVendor(Guid userId)
        {
            if (!await IsUserTenantOwnerAsync())
            {
                return Forbid("You are not the owner of this tenant.");
            }

            try
            {
                await _vendorService.RevokeVendorAsync(_tenantProvider.TenantId!.Value, userId);
                return Ok(new { Message = "Vendor revoked successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("create")]
        public async Task<IActionResult> CreateVendor([FromBody] EFastCommerce.Api.Models.CreateVendorDto dto)
        {
            if (!await IsUserTenantOwnerAsync())
            {
                return Forbid("You are not the owner of this tenant.");
            }

            var tenantId = _tenantProvider.TenantId!.Value;
            Guid userId;

            // Check if user already exists
            var existingUser = await _userService.GetUserByEmailAsync(dto.Email);
            if (existingUser != null)
            {
                userId = existingUser.Id;
            }
            else
            {
                // Register new user
                try
                {
                    var newUser = new EFastCommerce.Core.Entities.User
                    {
                        Username = dto.Username,
                        Email = dto.Email,
                        Role = "Client" // Base role, contextual role comes from TenantVendor
                    };
                    var createdUser = await _userService.RegisterAsync(newUser, dto.Password);
                    userId = createdUser.Id;
                }
                catch (Exception ex)
                {
                    return BadRequest(new { Error = ex.Message });
                }
            }

            // Link user as vendor
            try
            {
                await _vendorService.AddVendorAsync(tenantId, userId);
                return Ok(new { Message = "Vendor created and linked successfully." });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }
}
