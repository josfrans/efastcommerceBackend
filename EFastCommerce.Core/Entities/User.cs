using System;

namespace EFastCommerce.Core.Entities
{
    public static class UserRoles
    {
        public const string SystemAdmin = "SystemAdmin";
        public const string VendorAdmin = "VendorAdmin";
        public const string Client = "Client";
    }

    public class User
    {
        public Guid Id { get; set; }
        // TenantId is removed because a user can now be part of multiple tenants
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string PasswordHash { get; set; } = string.Empty;
        public string Role { get; set; } = UserRoles.Client;
        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        // Password Reset properties
        public string? PasswordResetCode { get; set; }
        public DateTime? PasswordResetCodeExpiration { get; set; }

        // Navigation properties
        public ICollection<Tenant> OwnedTenants { get; set; } = new List<Tenant>();
        public ICollection<TenantVendor> TenantVendors { get; set; } = new List<TenantVendor>();
        public ICollection<StoreSubscription> StoreSubscriptions { get; set; } = new List<StoreSubscription>();
    }
}
