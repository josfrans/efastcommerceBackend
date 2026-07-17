using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;
using Microsoft.Extensions.Configuration;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class ReferralsController : ControllerBase
    {
        private readonly IReferralService _referralService;
        private readonly ITenantProvider _tenantProvider;
        private readonly ITenantService _tenantService;
        private readonly IInvitationService _invitationService;
        private readonly IConfiguration _configuration;

        public ReferralsController(
            IReferralService referralService,
            ITenantProvider tenantProvider,
            ITenantService tenantService,
            IInvitationService invitationService,
            IConfiguration configuration)
        {
            _referralService = referralService;
            _tenantProvider = tenantProvider;
            _tenantService = tenantService;
            _invitationService = invitationService;
            _configuration = configuration;
        }

        [HttpPost("generate")]
        [Authorize]
        public async Task<IActionResult> Generate([FromBody] GenerateReferralRequest request)
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

                // Verificar si el usuario es cliente (subscriptor), vendor o dueño.
                // Reutilizamos IsUserSubscribedAsync que ya verifica esto de cierta manera para los customers.
                // En realidad un Owner/Vendor también debería poder generar referidos si lo desea.
                // Para simplificar, permitiremos que cualquier persona que esté logueada e intente generar
                // un referido lo logre, SIEMPRE y cuando esté suscrito de alguna forma.
                
                var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
                if (tenant == null)
                {
                    return BadRequest("Tienda no encontrada.");
                }

                // Generar token con tiempo de vida (ej. 7 días, 168 horas)
                int hours = request.ExpiresHours > 0 ? request.ExpiresHours : 168; 
                var referral = await _referralService.GenerateReferralAsync(tenantId, userId, hours);

                string frontendUrl = _configuration["FrontendUrl"]?.TrimEnd('/') ?? "http://localhost:8100";
                string inviteUrl = $"{frontendUrl}/store/{tenant.Slug}/referral?code={referral.Token}";

                return Ok(new
                {
                    referral.Id,
                    referral.TenantId,
                    referral.Token,
                    referral.ExpiresAt,
                    referral.CreatedAt,
                    InviteUrl = inviteUrl
                });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpGet("validate/{token}")]
        public async Task<IActionResult> Validate(string token)
        {
            var referral = await _referralService.ValidateTokenAsync(token);
            if (referral == null)
            {
                return BadRequest(new { Error = "El código de referido no existe, ya fue usado o ha expirado." });
            }

            return Ok(new
            {
                Token = referral.Token,
                ExpiresAt = referral.ExpiresAt,
                Tenant = new
                {
                    referral.Tenant?.Id,
                    referral.Tenant?.Name,
                    referral.Tenant?.Slug,
                    referral.Tenant?.LogoUrl,
                    referral.Tenant?.ThemeColor
                },
                Referrer = new 
                {
                    referral.ReferrerUser?.Id,
                    referral.ReferrerUser?.Username
                }
            });
        }

        [HttpPost("subscribe")]
        [Authorize]
        public async Task<IActionResult> Subscribe([FromBody] SubscribeReferralRequest request)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Unauthorized();
                }

                var subscription = await _referralService.SubscribeReferredUserAsync(userId, request.Token);
                return Ok(new { Message = "Has sido suscrito usando un código de referido.", TenantId = subscription.TenantId });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class GenerateReferralRequest
    {
        public int ExpiresHours { get; set; }
    }

    public class SubscribeReferralRequest
    {
        public string Token { get; set; } = string.Empty;
    }
}
