using System;

namespace EFastCommerce.Core.Entities
{
    public class Tenant
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty; // Subdomain or url path identifier
        public string LogoUrl { get; set; } = string.Empty;
        public string ThemeColor { get; set; } = "#3880ff"; // Default ionic primary color
        public bool IsActive { get; set; } = true;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public Guid OwnerId { get; set; } // The user who created/owns this tenant

        // Navigation properties
        public User? Owner { get; set; }
        public ICollection<TenantVendor> Vendors { get; set; } = new List<TenantVendor>();
    }
}
