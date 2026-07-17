using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class InvitationsController : ControllerBase
    {
        private readonly IInvitationService _invitationService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ITenantService _tenantService;
        private readonly IVendorService _vendorService;
        private readonly IConfiguration _configuration;

        public InvitationsController(
            IInvitationService invitationService,
            ITenantProvider tenantProvider,
            ITenantService tenantService,
            IVendorService vendorService,
            IConfiguration configuration)
        {
            _invitationService = invitationService;
            _tenantProvider = tenantProvider;
            _tenantService = tenantService;
            _vendorService = vendorService;
            _configuration = configuration;
        }

        private async Task<bool> IsOwnerVendorOrAdminAsync(Guid tenantId, Guid userId)
        {
            if (User.IsInRole(UserRoles.SystemAdmin))
                return true;

            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant != null && tenant.OwnerId == userId)
                return true;

            var vendor = await _vendorService.GetVendorAsync(tenantId, userId);
            if (vendor != null && vendor.Status == "Approved")
                return true;

            return false;
        }

        private async Task<bool> IsCustomerAsync(Guid tenantId, Guid userId)
        {
            return await _invitationService.IsUserSubscribedAsync(userId, tenantId);
        }

        [HttpPost("generate")]
        [Authorize]
        public async Task<IActionResult> Generate([FromBody] GenerateInvitationRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Forbid();
                }

                if (!_tenantProvider.TenantId.HasValue)
                {
                    return BadRequest("Tenant context is required.");
                }

                var tenantId = _tenantProvider.TenantId.Value;

                if (!await IsOwnerVendorOrAdminAsync(tenantId, userId) && !await IsCustomerAsync(tenantId, userId))
                {
                    return StatusCode(403, new { Error = "You are not authorized to manage invitations for this store." });
                }

                var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
                if (tenant == null)
                {
                    return BadRequest("Tienda no encontrada.");
                }

                int hours = request.ExpiresHours > 0 ? request.ExpiresHours : 168; // Default 7 days (168 hours)
                var invitation = await _invitationService.GenerateInvitationAsync(tenantId, hours);

                // Build invitation link URL using configured FrontendUrl
                string frontendUrl = _configuration["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:8100";
                string inviteUrl = $"{frontendUrl}/store/{tenant.Slug}/invite?token={invitation.Token}";

                return Ok(new
                {
                    invitation.Id,
                    invitation.TenantId,
                    invitation.Token,
                    invitation.ExpiresAt,
                    invitation.CreatedAt,
                    InviteUrl = inviteUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("links")]
        [Authorize]
        public async Task<IActionResult> GetLinks()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;

            if (!await IsOwnerVendorOrAdminAsync(tenantId, userId))
            {
                return StatusCode(403, new { Error = "You are not authorized to view invitations for this store." });
            }

            var invitations = await _invitationService.GetInvitationsByTenantAsync(tenantId);
            return Ok(invitations);
        }

        [HttpDelete("links/{id}")]
        [Authorize]
        public async Task<IActionResult> DeleteLink(Guid id)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;

            if (!await IsOwnerVendorOrAdminAsync(tenantId, userId))
            {
                return StatusCode(403, new { Error = "You are not authorized to manage invitations for this store." });
            }

            bool deleted = await _invitationService.DeleteInvitationAsync(id, tenantId);
            if (!deleted)
            {
                return NotFound("Enlace de invitación no encontrado o no pertenece a tu tienda.");
            }

            return NoContent();
        }

        [HttpGet("customers")]
        [Authorize]
        public async Task<IActionResult> GetCustomers()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;

            if (!await IsOwnerVendorOrAdminAsync(tenantId, userId))
            {
                return StatusCode(403, new { Error = "You are not authorized to view customers for this store." });
            }

            var subscriptions = await _invitationService.GetCustomersByTenantAsync(tenantId);
            
            var customers = new List<object>();
            foreach (var sub in subscriptions)
            {
                if (sub.User != null)
                {
                    customers.Add(new
                    {
                        sub.UserId,
                        sub.User.Username,
                        sub.User.Email,
                        JoinedAt = sub.SubscribedAt,
                        ReferredByUsername = sub.ReferredByUser?.Username
                    });
                }
            }

            return Ok(customers);
        }

        [HttpDelete("customers/{targetUserId}")]
        [Authorize]
        public async Task<IActionResult> RemoveCustomer(Guid targetUserId)
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;

            if (!await IsOwnerVendorOrAdminAsync(tenantId, userId))
            {
                return StatusCode(403, new { Error = "You are not authorized to manage customers for this store." });
            }

            bool removed = await _invitationService.RemoveCustomerAsync(targetUserId, tenantId);
            if (!removed)
            {
                return NotFound("Cliente no encontrado o no está suscrito a esta tienda.");
            }

            return NoContent();
        }

        [HttpGet("validate/{token}")]
        public async Task<IActionResult> Validate(string token)
        {
            var invitation = await _invitationService.ValidateTokenAsync(token);
            if (invitation == null)
            {
                return BadRequest(new { Error = "El enlace de invitación no existe o ha expirado." });
            }

            return Ok(new
            {
                Token = invitation.Token,
                ExpiresAt = invitation.ExpiresAt,
                Tenant = new
                {
                    invitation.Tenant?.Id,
                    invitation.Tenant?.Name,
                    invitation.Tenant?.Slug,
                    invitation.Tenant?.LogoUrl,
                    invitation.Tenant?.ThemeColor
                }
            });
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                var subscription = await _invitationService.SubscribeUserAsync(userId, request.Token);
                return Ok(new { Message = "Suscripción completada con éxito.", TenantId = subscription.TenantId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("subscription-status")]
        [Authorize]
        public async Task<IActionResult> GetSubscriptionStatus()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Unauthorized();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Contexto de tienda (Tenant) no especificado.");
            }

            bool isSubscribed = await _invitationService.IsUserSubscribedAsync(userId, _tenantProvider.TenantId.Value);
            return Ok(new { IsSubscribed = isSubscribed });
        }
    }

    public class GenerateInvitationRequest
    {
        public int ExpiresHours { get; set; }
    }

    public class SubscribeRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
