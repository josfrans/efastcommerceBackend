using System;

namespace EFastCommerce.Core.Entities
{
    public class StoreReferralLink
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid ReferrerUserId { get; set; }
        public string Token { get; set; } = string.Empty;
        public bool IsUsed { get; set; } = false;
        public DateTime ExpiresAt { get; set; }
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Tenant? Tenant { get; set; }
        public User? ReferrerUser { get; set; }
    }
}
