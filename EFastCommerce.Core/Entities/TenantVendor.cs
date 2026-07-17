using System;

namespace EFastCommerce.Core.Entities
{
    public class TenantVendor
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        
        // "Pending", "Approved", "Revoked"
        public string Status { get; set; } = "Pending";
        
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? ApprovedAt { get; set; }

        // Navigation properties
        public Tenant? Tenant { get; set; }
        public User? User { get; set; }
    }
}
