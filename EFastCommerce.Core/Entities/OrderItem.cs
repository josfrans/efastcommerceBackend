using System;

namespace EFastCommerce.Core.Entities
{
    public class OrderItem
    {
        public Guid Id { get; set; }
        public Guid OrderId { get; set; }
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
        public decimal Price { get; set; } // Historical price at purchase time

        // Navigation properties
        public Order? Order { get; set; }
        public Product? Product { get; set; }
    }
}
