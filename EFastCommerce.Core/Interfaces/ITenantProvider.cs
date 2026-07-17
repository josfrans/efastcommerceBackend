using System;

namespace EFastCommerce.Core.Interfaces
{
    public interface ITenantProvider
    {
        Guid? TenantId { get; set; }
        string? TenantSlug { get; set; }
    }
}
