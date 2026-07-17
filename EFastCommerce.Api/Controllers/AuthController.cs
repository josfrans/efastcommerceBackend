using System;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using System.Threading.Tasks;
using System.Linq;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;
using Microsoft.IdentityModel.Tokens;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IUserService _userService;
        private readonly ITenantService _tenantService;
        private readonly IConfiguration _configuration;

        public AuthController(IUserService userService, ITenantService tenantService, IConfiguration configuration)
        {
            _userService = userService;
            _tenantService = tenantService;
            _configuration = configuration;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var user = new User
                {
                    Username = request.Username,
                    Email = request.Email,
                    Role = request.Role
                };

                var registeredUser = await _userService.RegisterAsync(user, request.Password);

                if (request.Role == UserRoles.VendorAdmin)
                {
                    if (string.IsNullOrWhiteSpace(request.CompanyName) || string.IsNullOrWhiteSpace(request.CompanySlug))
                    {
                        return BadRequest("Company name and slug are required for vendor registration.");
                    }

                    // Check if slug is already taken
                    var existingTenant = await _tenantService.GetTenantBySlugAsync(request.CompanySlug);
                    if (existingTenant != null)
                    {
                        return BadRequest("This company slug is already taken.");
                    }

                    var newTenant = new Tenant
                    {
                        Name = request.CompanyName,
                        Slug = request.CompanySlug.ToLower().Trim(),
                        IsActive = true,
                        OwnerId = registeredUser.Id
                    };

                    await _tenantService.CreateTenantAsync(newTenant);
                }
                else if (request.Role == UserRoles.Client && request.TenantId.HasValue)
                {
                    // A client might register via an invitation token or direct link,
                    // but the StoreSubscription is typically handled in a separate service.
                    // For now, no strict action is required here since subscriptions manage Client tenant links.
                }

                return Ok(new { Message = "User registered successfully", UserId = registeredUser.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            var authenticatedUser = await _userService.AuthenticateAsync(request.UsernameOrEmail, request.Password);
            if (authenticatedUser == null)
            {
                return Unauthorized(new { Error = "Invalid username/email or password." });
            }

            var user = await _userService.GetUserWithTenantsAsync(authenticatedUser.Id);
            if (user == null)
            {
                return Unauthorized(new { Error = "User details not found." });
            }

            var token = GenerateJwtToken(user);

            // Fetch AccessibleTenants for ALL associations
            var accessibleTenantsDict = new System.Collections.Generic.Dictionary<Guid, object>();
            
            // 1. Owners
            foreach (var tenant in user.OwnedTenants)
            {
                accessibleTenantsDict[tenant.Id] = new { tenant.Id, tenant.Name, tenant.Slug, Role = "Owner", IsActive = tenant.IsActive };
            }
            
            // 2. Vendors
            foreach (var tv in user.TenantVendors)
            {
                if (tv.Status == "Approved" && tv.Tenant != null)
                {
                    // Avoid duplicate if they are owner AND vendor
                    if (!accessibleTenantsDict.ContainsKey(tv.Tenant.Id))
                    {
                        accessibleTenantsDict[tv.Tenant.Id] = new { tv.Tenant.Id, tv.Tenant.Name, tv.Tenant.Slug, Role = "Vendor", IsActive = tv.Tenant.IsActive };
                    }
                }
            }

            // 3. Subscriptions (Clients)
            foreach (var sub in user.StoreSubscriptions)
            {
                if (sub.Status == "Active" && sub.Tenant != null)
                {
                    // Add as client if not already Owner or Vendor
                    if (!accessibleTenantsDict.ContainsKey(sub.Tenant.Id))
                    {
                        accessibleTenantsDict[sub.Tenant.Id] = new { sub.Tenant.Id, sub.Tenant.Name, sub.Tenant.Slug, Role = "Client", IsActive = sub.Tenant.IsActive };
                    }
                }
            }

            var accessibleTenants = accessibleTenantsDict.Values.ToList();

            return Ok(new
            {
                Token = token,
                User = new
                {
                    user.Id,
                    user.Username,
                    user.Email,
                    user.Role
                },
                AccessibleTenants = accessibleTenants
            });
        }

        private string GenerateJwtToken(User user)
        {
            var tokenHandler = new JwtSecurityTokenHandler();
            
            // Read key from environment or configuration, fallback if not set
            var secretKey = Environment.GetEnvironmentVariable("JWT_KEY") 
                            ?? _configuration["Jwt:Key"] 
                            ?? "super_secret_key_that_is_at_least_32_characters_long_for_security";
            var key = Encoding.UTF8.GetBytes(secretKey);

            var issuer = Environment.GetEnvironmentVariable("JWT_ISSUER") 
                         ?? _configuration["Jwt:Issuer"] 
                         ?? "EFastCommerce";
            var audience = Environment.GetEnvironmentVariable("JWT_AUDIENCE") 
                           ?? _configuration["Jwt:Audience"] 
                           ?? "EFastCommerceApp";

            var claims = new[]
            {
                new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                new Claim(ClaimTypes.Name, user.Username),
                new Claim(ClaimTypes.Email, user.Email),
                new Claim(ClaimTypes.Role, user.Role)
            };

            var tokenDescriptor = new SecurityTokenDescriptor
            {
                Subject = new ClaimsIdentity(claims),
                Expires = DateTime.UtcNow.AddDays(7),
                Issuer = issuer,
                Audience = audience,
                SigningCredentials = new SigningCredentials(new SymmetricSecurityKey(key), SecurityAlgorithms.HmacSha256Signature)
            };

            var token = tokenHandler.CreateToken(tokenDescriptor);
            return tokenHandler.WriteToken(token);
        }
    }

    public class RegisterRequest
    {
        public string Username { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public string Role { get; set; } = UserRoles.Client;

        // Vendor specific
        public string? CompanyName { get; set; }
        public string? CompanySlug { get; set; }

        // Client specific (optional context)
        public Guid? TenantId { get; set; }
    }

    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
