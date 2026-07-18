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
        private readonly IInvitationService _invitationService;
        private readonly IEmailService _emailService;

        public AuthController(IUserService userService, ITenantService tenantService, IConfiguration configuration, IInvitationService invitationService, IEmailService emailService)
        {
            _userService = userService;
            _tenantService = tenantService;
            _configuration = configuration;
            _invitationService = invitationService;
            _emailService = emailService;
        }

        [HttpPost("register")]
        public async Task<IActionResult> Register([FromBody] RegisterRequest request)
        {
            try
            {
                var existingUser = await _userService.GetUserByEmailAsync(request.Email);
                User registeredUser;

                if (existingUser != null)
                {
                    // Existing user flow
                    if (!_userService.VerifyUserPassword(existingUser, request.Password))
                    {
                        return BadRequest(new { Error = "ExistingUserWrongPassword", Message = "El correo ya está registrado, pero la contraseña es incorrecta." });
                    }
                    registeredUser = existingUser;
                    // If the user was a client and is now registering as a VendorAdmin, upgrade role
                    if (existingUser.Role == UserRoles.Client && request.Role == UserRoles.VendorAdmin)
                    {
                        // In a real app we'd update the role, but for this demo, keeping it simple
                    }
                }
                else
                {
                    // New user flow
                    var user = new User
                    {
                        Username = request.Username,
                        Email = request.Email,
                        Role = request.Role
                    };
                    registeredUser = await _userService.RegisterAsync(user, request.Password);
                }

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
                else if (request.Role == UserRoles.Client)
                {
                    if (string.IsNullOrWhiteSpace(request.InvitationToken))
                    {
                        // Clean up the created user if token is missing
                        // Assuming simple delete for now, but a real app might use transactions
                        return BadRequest(new { Error = "Se requiere un código de invitación para registrarse como cliente." });
                    }

                    var validToken = await _invitationService.ValidateTokenAsync(request.InvitationToken);
                    if (validToken == null)
                    {
                        return BadRequest(new { Error = "El código de invitación no es válido o ha expirado." });
                    }
                    await _invitationService.SubscribeUserAsync(registeredUser.Id, request.InvitationToken);
                }

                return Ok(new { Message = "User registered successfully", UserId = registeredUser.Id });
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPost("forgot-password")]
        public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
        {
            try
            {
                var code = await _userService.GeneratePasswordResetCodeAsync(request.Email);
                
                var body = $@"
<!DOCTYPE html>
<html lang='es'>
<head>
    <meta charset='UTF-8'>
    <meta name='viewport' content='width=device-width, initial-scale=1.0'>
    <style>
        body {{
            font-family: 'Helvetica Neue', Helvetica, Arial, sans-serif;
            background-color: #f4f6f8;
            margin: 0;
            padding: 0;
        }}
        .container {{
            max-width: 600px;
            margin: 40px auto;
            background-color: #ffffff;
            border-radius: 16px;
            box-shadow: 0 10px 30px rgba(0, 0, 0, 0.05);
            overflow: hidden;
        }}
        .header {{
            background-color: #3880ff;
            color: #ffffff;
            text-align: center;
            padding: 30px 20px;
        }}
        .header h1 {{
            margin: 0;
            font-size: 24px;
            font-weight: 700;
        }}
        .content {{
            padding: 40px 30px;
            color: #333333;
            line-height: 1.6;
        }}
        .content h2 {{
            font-size: 20px;
            color: #222428;
            margin-top: 0;
        }}
        .code-box {{
            background-color: #f4f6f8;
            border: 2px dashed #3880ff;
            border-radius: 8px;
            text-align: center;
            padding: 20px;
            margin: 30px 0;
            font-size: 32px;
            font-weight: 700;
            color: #3880ff;
            letter-spacing: 4px;
        }}
        .footer {{
            background-color: #f4f6f8;
            text-align: center;
            padding: 20px;
            color: #92949c;
            font-size: 14px;
        }}
    </style>
</head>
<body>
    <div class='container'>
        <div class='header'>
            <h1>E-Fast Commerce</h1>
        </div>
        <div class='content'>
            <h2>Recuperación de Contraseña</h2>
            <p>Hola,</p>
            <p>Hemos recibido una solicitud para restablecer la contraseña de tu cuenta en <strong>E-Fast Commerce</strong>.</p>
            <p>Por favor, utiliza el siguiente código de verificación de 6 dígitos para continuar con el proceso:</p>
            
            <div class='code-box'>
                {code}
            </div>
            
            <p>Este código <strong>expirará en 15 minutos</strong>. Si no solicitaste este cambio, puedes ignorar este correo de forma segura.</p>
        </div>
        <div class='footer'>
            &copy; {DateTime.UtcNow.Year} E-Fast Commerce. Todos los derechos reservados.
        </div>
    </div>
</body>
</html>";

                await _emailService.SendEmailAsync(request.Email, "Recuperación de contraseña - E-Fast Commerce", body);
                return Ok(new { Message = "Si el correo está registrado, se ha enviado un código de recuperación." });
            }
            catch (Exception ex)
            {
                if (ex.Message == "User not found.")
                {
                    return NotFound(new { Error = "El correo electrónico no está registrado en el sistema." });
                }
                return BadRequest(new { Error = "Error procesando la solicitud." });
            }
        }

        [HttpPost("reset-password")]
        public async Task<IActionResult> ResetPassword([FromBody] ResetPasswordRequest request)
        {
            var success = await _userService.ResetPasswordAsync(request.Email, request.Code, request.NewPassword);
            if (!success)
            {
                return BadRequest(new { Error = "El código es inválido o ha expirado." });
            }
            return Ok(new { Message = "Contraseña restablecida correctamente." });
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
            var accessibleTenantsDict = new System.Collections.Generic.Dictionary<Guid, AccessibleTenantDto>();
            
            // 1. Owners
            foreach (var tenant in user.OwnedTenants)
            {
                accessibleTenantsDict[tenant.Id] = new AccessibleTenantDto
                {
                    Id = tenant.Id,
                    Name = tenant.Name,
                    Slug = tenant.Slug,
                    Roles = new System.Collections.Generic.List<string> { "Owner" },
                    IsActive = tenant.IsActive
                };
            }
            
            // 2. Vendors
            foreach (var tv in user.TenantVendors)
            {
                if (tv.Status == "Approved" && tv.Tenant != null)
                {
                    if (accessibleTenantsDict.TryGetValue(tv.Tenant.Id, out var existing))
                    {
                        if (!existing.Roles.Contains("Vendor")) existing.Roles.Add("Vendor");
                    }
                    else
                    {
                        accessibleTenantsDict[tv.Tenant.Id] = new AccessibleTenantDto
                        {
                            Id = tv.Tenant.Id,
                            Name = tv.Tenant.Name,
                            Slug = tv.Tenant.Slug,
                            Roles = new System.Collections.Generic.List<string> { "Vendor" },
                            IsActive = tv.Tenant.IsActive
                        };
                    }
                }
            }

            // 3. Subscriptions (Clients)
            foreach (var sub in user.StoreSubscriptions)
            {
                if (sub.Status == "Active" && sub.Tenant != null)
                {
                    if (accessibleTenantsDict.TryGetValue(sub.Tenant.Id, out var existing))
                    {
                        if (!existing.Roles.Contains("Client")) existing.Roles.Add("Client");
                    }
                    else
                    {
                        accessibleTenantsDict[sub.Tenant.Id] = new AccessibleTenantDto
                        {
                            Id = sub.Tenant.Id,
                            Name = sub.Tenant.Name,
                            Slug = sub.Tenant.Slug,
                            Roles = new System.Collections.Generic.List<string> { "Client" },
                            IsActive = sub.Tenant.IsActive
                        };
                    }
                }
            }

            var accessibleTenants = accessibleTenantsDict.Values.Select(t => new
            {
                t.Id,
                t.Name,
                t.Slug,
                Roles = t.Roles.ToArray(),
                t.IsActive
            }).ToList();

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
        public string? InvitationToken { get; set; }
    }

    public class LoginRequest
    {
        public string UsernameOrEmail { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }

    public class ForgotPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
    }

    public class ResetPasswordRequest
    {
        public string Email { get; set; } = string.Empty;
        public string Code { get; set; } = string.Empty;
        public string NewPassword { get; set; } = string.Empty;
    }

    public class AccessibleTenantDto
    {
        public Guid Id { get; set; }
        public string Name { get; set; } = string.Empty;
        public string Slug { get; set; } = string.Empty;
        public System.Collections.Generic.List<string> Roles { get; set; } = new System.Collections.Generic.List<string>();
        public bool IsActive { get; set; }
    }
}
