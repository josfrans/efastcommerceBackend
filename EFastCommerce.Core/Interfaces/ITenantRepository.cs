using System.Threading.Tasks;
using EFastCommerce.Core.Entities;

namespace EFastCommerce.Core.Interfaces
{
    public interface ITenantRepository : IRepository<Tenant>
    {
        Task<Tenant?> GetBySlugAsync(string slug);
        Task<Tenant?> GetByNameAsync(string name);
    }
}
