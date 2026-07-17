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
    public class ProductRepository : Repository<Product>, IProductRepository
    {
        public ProductRepository(EFastCommerceContext context) : base(context)
        {
        }

        public async Task<IEnumerable<Product>> GetProductsByTenantAsync(Guid tenantId)
        {
            return await _dbSet.Where(p => p.TenantId == tenantId && p.IsActive)
                               .ToListAsync();
        }
    }
}
