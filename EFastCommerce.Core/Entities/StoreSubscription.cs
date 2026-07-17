using System;

namespace EFastCommerce.Core.Entities
{
    public class StoreSubscription
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid UserId { get; set; }
        public Guid? ReferredByUserId { get; set; } // Identifica si fue referido por alguien
        public DateTime SubscribedAt { get; set; } = DateTime.UtcNow;
        public string Status { get; set; } = "Active";

        // Navigation properties
        public Tenant? Tenant { get; set; }
        public User? User { get; set; }
        public User? ReferredByUser { get; set; }
    }
}
