using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using EFastCommerce.Core.Entities;
using EFastCommerce.Core.Interfaces;
using EFastCommerce.Core.Interfaces.Services;

namespace EFastCommerce.Services
{
    public class OrderService : IOrderService
    {
        private readonly IOrderRepository _orderRepository;
        private readonly IProductRepository _productRepository;

        public OrderService(IOrderRepository orderRepository, IProductRepository productRepository)
        {
            _orderRepository = orderRepository;
            _productRepository = productRepository;
        }

        public async Task<Order?> GetOrderByIdAsync(Guid id)
        {
            return await _orderRepository.GetOrderDetailsAsync(id);
        }

        public async Task<IEnumerable<Order>> GetOrdersByTenantAsync(Guid tenantId)
        {
            return await _orderRepository.GetOrdersByTenantAsync(tenantId);
        }

        public async Task<Order> CreateOrderAsync(Order order)
        {
            if (order.Id == Guid.Empty)
            {
                order.Id = Guid.NewGuid();
            }
            order.CreatedAt = DateTime.UtcNow;
            order.Status = "Pending";

            decimal calculatedTotal = 0;

            foreach (var item in order.OrderItems)
            {
                if (item.Id == Guid.Empty)
                {
                    item.Id = Guid.NewGuid();
                }
                item.OrderId = order.Id;

                // Validate product stock & snap price
                var product = await _productRepository.GetByIdAsync(item.ProductId);
                if (product == null)
                {
                    throw new Exception($"Product with id {item.ProductId} not found.");
                }
                if (product.TenantId != order.TenantId)
                {
                    throw new Exception($"Product with id {product.Id} does not belong to Tenant {order.TenantId}");
                }
                if (product.Stock < item.Quantity)
                {
                    throw new Exception($"Insufficient stock for product '{product.Name}'. Available: {product.Stock}, Requested: {item.Quantity}");
                }

                // Snap the current price
                item.Price = product.Price;
                calculatedTotal += item.Price * item.Quantity;

                // Decrease stock
                product.Stock -= item.Quantity;
            }

            order.TotalAmount = calculatedTotal;

            await _orderRepository.AddAsync(order);
            return order;
        }

        public async Task UpdateOrderStatusAsync(Guid orderId, string status)
        {
            var order = await _orderRepository.GetByIdAsync(orderId);
            if (order == null)
            {
                throw new Exception($"Order with id {orderId} not found.");
            }

            order.Status = status;
            await _orderRepository.UpdateAsync(order);
        }
    }
}
