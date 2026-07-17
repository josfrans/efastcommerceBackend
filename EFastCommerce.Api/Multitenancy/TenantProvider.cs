using System;
using EFastCommerce.Core.Interfaces;

namespace EFastCommerce.Api.Multitenancy
{
    public class TenantProvider : ITenantProvider
    {
        public Guid? TenantId { get; set; }
        public string? TenantSlug { get; set; }
    }
}
