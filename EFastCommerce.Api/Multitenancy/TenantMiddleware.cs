using System;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Http;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Multitenancy
{
    public class TenantMiddleware
    {
        private readonly RequestDelegate _next;

        public TenantMiddleware(RequestDelegate next)
        {
            _next = next;
        }

        public async Task InvokeAsync(HttpContext context, ITenantProvider tenantProvider, ITenantService tenantService)
        {
            // 1. Check for X-Tenant-Id header (Direct UUID)
            if (context.Request.Headers.TryGetValue("X-Tenant-Id", out var tenantIdHeader))
            {
                if (Guid.TryParse(tenantIdHeader, out var tenantId))
                {
                    tenantProvider.TenantId = tenantId;
                    var tenant = await tenantService.GetTenantByIdAsync(tenantId);
                    if (tenant != null)
                    {
                        tenantProvider.TenantSlug = tenant.Slug;
                    }
                }
            }
            // 2. Check for X-Tenant-Slug header (URL Friendly Slug)
            else if (context.Request.Headers.TryGetValue("X-Tenant-Slug", out var tenantSlugHeader))
            {
                string slug = tenantSlugHeader.ToString();
                var tenant = await tenantService.GetTenantBySlugAsync(slug);
                if (tenant != null)
                {
                    tenantProvider.TenantId = tenant.Id;
                    tenantProvider.TenantSlug = tenant.Slug;
                }
            }

            await _next(context);
        }
    }
}
