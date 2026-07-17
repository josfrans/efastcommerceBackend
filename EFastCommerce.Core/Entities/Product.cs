using System;
using System.ComponentModel.DataAnnotations;

namespace EFastCommerce.Core.Entities
{
    public enum MeasurementUnit { Piece, Size }

    public class Product
    {
        public Guid Id { get; set; }
        public Guid TenantId { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Description { get; set; } = string.Empty;
        public decimal Price { get; set; }
        public string ImageUrl { get; set; } = string.Empty;
        public int Stock { get; set; }
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Navigation property
        public Tenant? Tenant { get; set; }

        // Measurement unit (required)
        public MeasurementUnit MeasurementUnit { get; set; }

        // Size or measurement value (optional, e.g., "M", "38", "1 Litro")
        public string? Size { get; set; }
    }
}
