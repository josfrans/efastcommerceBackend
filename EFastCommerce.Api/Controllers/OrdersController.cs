using System;
using System.Collections.Generic;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly IOrderService _orderService;
        private readonly ITenantProvider _tenantProvider;
        private readonly IInvitationService _invitationService;
        private readonly ITenantService _tenantService;
        private readonly IVendorService _vendorService;

        public OrdersController(
            IOrderService orderService, 
            ITenantProvider tenantProvider,
            IInvitationService invitationService,
            ITenantService tenantService,
            IVendorService vendorService)
        {
            _orderService = orderService;
            _tenantProvider = tenantProvider;
            _invitationService = invitationService;
            _tenantService = tenantService;
            _vendorService = vendorService;
        }

        private async Task<bool> IsOwnerOrVendorAsync(Guid tenantId, Guid userId)
        {
            var tenant = await _tenantService.GetTenantByIdAsync(tenantId);
            if (tenant != null && tenant.OwnerId == userId)
                return true;

            var vendor = await _vendorService.GetVendorAsync(tenantId, userId);
            if (vendor != null && vendor.Status == "Approved")
                return true;

            return false;
        }

        [HttpGet]
        [Authorize]
        public async Task<ActionResult<IEnumerable<Order>>> GetOrders()
        {
            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (!_tenantProvider.TenantId.HasValue)
            {
                return BadRequest("Tenant context is required.");
            }

            var tenantId = _tenantProvider.TenantId.Value;
            var tenantOrders = await _orderService.GetOrdersByTenantAsync(tenantId);

            if (await IsOwnerOrVendorAsync(tenantId, userId))
            {
                // Owner or vendor can see all orders for this tenant
                return Ok(tenantOrders);
            }
            else
            {
                // Client only sees their own orders
                var clientOrders = new List<Order>();
                foreach (var order in tenantOrders)
                {
                    if (order.UserId == userId)
                    {
                        clientOrders.Add(order);
                    }
                }
                return Ok(clientOrders);
            }
        }

        [HttpGet("{id}")]
        [Authorize]
        public async Task<ActionResult<Order>> GetById(Guid id)
        {
            var order = await _orderService.GetOrderByIdAsync(id);
            if (order == null)
            {
                return NotFound();
            }

            var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
            {
                return Forbid();
            }

            if (order.UserId == userId)
            {
                return Ok(order);
            }

            if (await IsOwnerOrVendorAsync(order.TenantId, userId))
            {
                return Ok(order);
            }

            return StatusCode(403, new { Error = "You do not have permission to view this order." });
        }

        [HttpPost]
        [Authorize]
        public async Task<ActionResult<Order>> Create([FromBody] OrderCreateDto dto)
        {
            try
            {
                // Resolve tenant context
                Guid resolvedTenantId;
                if (dto.TenantId.HasValue)
                {
                    resolvedTenantId = dto.TenantId.Value;
                }
                else if (_tenantProvider.TenantId.HasValue)
                {
                    resolvedTenantId = _tenantProvider.TenantId.Value;
                }
                else
                {
                    return BadRequest("Tenant context is required to place an order.");
                }

                // If user is authenticated, attach their UserId
                Guid? userId = null;
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (!string.IsNullOrEmpty(userIdStr) && Guid.TryParse(userIdStr, out var parsedUserId))
                {
                    userId = parsedUserId;
                }

                if (!userId.HasValue)
                {
                    return Unauthorized("Inicia sesión para completar tu compra.");
                }

                // We no longer rely on global role. We just check if they are subscribed or owner/vendor
                if (!await IsOwnerOrVendorAsync(resolvedTenantId, userId.Value))
                {
                    bool isSubscribed = await _invitationService.IsUserSubscribedAsync(userId.Value, resolvedTenantId);
                    if (!isSubscribed)
                    {
                        return BadRequest(new { Error = "Acceso Denegado. Para poder comprar debes ser invitado a esta tienda." });
                    }
                }

                if (!dto.Items.Any())
                {
                    return BadRequest(new { Error = "La orden debe contener al menos un producto." });
                }

                var order = new Order
                {
                    TenantId = resolvedTenantId,
                    UserId = userId,
                    CustomerName = dto.CustomerName,
                    CustomerEmail = dto.CustomerEmail,
                };

                foreach (var itemDto in dto.Items)
                {
                    order.OrderItems.Add(new OrderItem
                    {
                        ProductId = itemDto.ProductId,
                        Quantity = itemDto.Quantity
                    });
                }

                var createdOrder = await _orderService.CreateOrderAsync(order);
                return CreatedAtAction(nameof(GetById), new { id = createdOrder.Id }, createdOrder);
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }

        [HttpPut("{id}/status")]
        [Authorize]
        public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] OrderStatusUpdateDto dto)
        {
            try
            {
                var userIdStr = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
                if (string.IsNullOrEmpty(userIdStr) || !Guid.TryParse(userIdStr, out var userId))
                {
                    return Forbid();
                }

                var order = await _orderService.GetOrderByIdAsync(id);
                if (order == null)
                {
                    return NotFound();
                }

                if (!await IsOwnerOrVendorAsync(order.TenantId, userId))
                {
                    return StatusCode(403, new { Error = "You can only modify orders belonging to your store." });
                }

                await _orderService.UpdateOrderStatusAsync(id, dto.Status);
                return NoContent();
            }
            catch (Exception ex)
            {
                return BadRequest(new { Error = ex.Message });
            }
        }
    }

    public class OrderCreateDto
    {
        public Guid? TenantId { get; set; }
        public string CustomerName { get; set; } = string.Empty;
        public string CustomerEmail { get; set; } = string.Empty;
        public List<OrderItemCreateDto> Items { get; set; } = new();
    }

    public class OrderItemCreateDto
    {
        public Guid ProductId { get; set; }
        public int Quantity { get; set; }
    }

    public class OrderStatusUpdateDto
    {
        public string Status { get; set; } = string.Empty;
    }
}
