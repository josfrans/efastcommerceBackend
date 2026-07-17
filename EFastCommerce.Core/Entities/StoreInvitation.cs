using System;

namespace EFastCommerce.Core.Entities
{
    public class StoreInvitation
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Token { get; set; } = string.Empty;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Tenant? Tenant { get; set; }
    }
}
