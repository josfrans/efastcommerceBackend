using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface IOrderRepository : IRepository<Order>
    {
        Task<IEnumerable<Order>> GetOrdersByTenantAsync(Guid tenantId);
        Task<Order?> GetOrderDetailsAsync(Guid orderId);
    }
}
