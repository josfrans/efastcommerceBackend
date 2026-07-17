using System;
using System.Collections.Generic;

namespace EFastCommerce.Core.Entities
{
    public class Order
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public Guid? UserId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public decimal TotalAmount { get; set; }
        public string Status { get; set; } = "Pending"; // Pending, Paid, Shipped, Cancelled
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation properties
        public Tenant? Tenant { get; set; }
        public User? User { get; set; }
        public ICollection<OrderItem> OrderItems { get; set; } = new List<OrderItem>();
    }
}
