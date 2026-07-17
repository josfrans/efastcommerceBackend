using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.EntityFrameworkCore;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Infrastructure.Data;

namespace EFastCommerce.Infrastructure.Repositories
{
    public class OrderRepository : Repository<Order>, IOrderRepository
    {
        public OrderRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Order>> GetOrdersByTenantAsync(Guid tenantId)
        {
            return await _dbSet.Where(o => o.TenantId == tenantId)
                               .Include(o => o.OrderItems)
                               .OrderByDescending(o => o.CreatedAt)
                               .ToListAsync();
        }

        public async Task<Order?> GetOrderDetailsAsync(Guid orderId)
        {
            return await _dbSet.Include(o => o.OrderItems)
                                 .ThenInclude(oi => oi.Product)
                               .FirstOrDefaultAsync(o => o.Id == orderId);
        }
    }
}
