using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces.Services
{
    public interface IOrderService
    {
        Task<Order?> GetOrderByIdAsync(Guid id);
        Task<IEnumerable<Order>> GetOrdersByTenantAsync(Guid tenantId);
        Task<Order> CreateOrderAsync(Order order);
        Task UpdateOrderStatusAsync(Guid orderId, string status);
    }
}
