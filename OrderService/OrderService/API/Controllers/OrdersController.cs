using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using OrderService.Domain.Entities;
using OrderService.Domain.Enum;
using OrderService.Domain.Events;
using OrderService.Infrastructure;
using OrderService.Infrastructure.EventBus;
using System.Security.Claims;

namespace OrderService.Features.Orders
{
    [Authorize]
    [ApiController]
    [Route("api/[controller]")]
    public class OrdersController : ControllerBase
    {
        private readonly OrderDbContext _context;
        private readonly IEventBus _eventBus;
        private readonly ILogger<OrdersController> _logger;

        public OrdersController(OrderDbContext context, IEventBus eventBus, ILogger<OrdersController> logger)
        {
            _context = context;
            _eventBus = eventBus;
            _logger = logger;
        }

        [HttpPost]
        public async Task<IActionResult> CreateOrder([FromBody] CreateOrderRequest request)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                if (request.Items == null || !request.Items.Any())
                {
                    return BadRequest(new { message = "Order must contain at least one item" });
                }

                if (string.IsNullOrWhiteSpace(request.ShippingAddress))
                {
                    return BadRequest(new { message = "Shipping address is required" });
                }

                foreach (var item in request.Items)
                {
                    if (item.Quantity <= 0)
                    {
                        return BadRequest(new { message = $"Invalid quantity for product {item.ProductId}" });
                    }
                    if (item.UnitPrice <= 0)
                    {
                        return BadRequest(new { message = $"Invalid price for product {item.ProductId}" });
                    }
                }

                var user = await _context.Users.FindAsync(userId);
                if (user == null)
                {
                    return NotFound(new { message = "User not found" });
                }

                var order = new Order
                {
                    Id = Guid.NewGuid(),
                    OrderNumber = GenerateOrderNumber(),
                    UserId = userId,
                    OrderDate = DateTime.UtcNow,
                    Status = OrderStatus.Pending,
                    ShippingAddress = request.ShippingAddress,
                    OrderItems = request.Items.Select(i => new OrderItem
                    {
                        Id = Guid.NewGuid(),
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.Quantity * i.UnitPrice
                    }).ToList()
                };

                order.TotalAmount = order.OrderItems.Sum(i => i.TotalPrice);

                _context.Orders.Add(order);
                await _context.SaveChangesAsync();

                // Publish OrderCreatedEvent
                var orderCreatedEvent = new OrderCreatedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = userId,
                    UserEmail = user.Email,
                    TotalAmount = order.TotalAmount,
                    ShippingAddress = order.ShippingAddress,
                    Items = order.OrderItems.Select(i => new OrderItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        Quantity = i.Quantity,
                        UnitPrice = i.UnitPrice,
                        TotalPrice = i.TotalPrice
                    }).ToList()
                };

                await _eventBus.PublishAsync("order.exchange", "order.created", orderCreatedEvent);

                _logger.LogInformation("Order created: {OrderNumber} for user {UserId}", order.OrderNumber, userId);

                return CreatedAtAction(nameof(GetOrder), new { id = order.Id }, new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    status = order.Status.ToString(),
                    totalAmount = order.TotalAmount,
                    items = order.OrderItems.Select(i => new
                    {
                        productId = i.ProductId,
                        productName = i.ProductName,
                        quantity = i.Quantity,
                        unitPrice = i.UnitPrice,
                        totalPrice = i.TotalPrice
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error creating order");
                return StatusCode(500, new { message = "An error occurred while creating the order" });
            }
        }

        [HttpGet("{id}")]
        public async Task<IActionResult> GetOrder(Guid id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (order.UserId != userId)
                {
                    return Forbid();
                }

                return Ok(new
                {
                    orderId = order.Id,
                    orderNumber = order.OrderNumber,
                    orderDate = order.OrderDate,
                    status = order.Status.ToString(),
                    totalAmount = order.TotalAmount,
                    shippingAddress = order.ShippingAddress,
                    items = order.OrderItems.Select(i => new
                    {
                        itemId = i.Id,
                        productId = i.ProductId,
                        productName = i.ProductName,
                        quantity = i.Quantity,
                        unitPrice = i.UnitPrice,
                        totalPrice = i.TotalPrice
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while retrieving the order" });
            }
        }

        [HttpGet]
        public async Task<IActionResult> GetUserOrders([FromQuery] int page = 1, [FromQuery] int pageSize = 10)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                if (page < 1 || pageSize < 1 || pageSize > 100)
                {
                    return BadRequest(new { message = "Invalid pagination parameters" });
                }

                var totalOrders = await _context.Orders.CountAsync(o => o.UserId == userId);
                var orders = await _context.Orders
                    .Include(o => o.OrderItems)
                    .Where(o => o.UserId == userId)
                    .OrderByDescending(o => o.OrderDate)
                    .Skip((page - 1) * pageSize)
                    .Take(pageSize)
                    .ToListAsync();

                return Ok(new
                {
                    page,
                    pageSize,
                    totalItems = totalOrders,
                    totalPages = (int)Math.Ceiling(totalOrders / (double)pageSize),
                    orders = orders.Select(o => new
                    {
                        orderId = o.Id,
                        orderNumber = o.OrderNumber,
                        orderDate = o.OrderDate,
                        status = o.Status.ToString(),
                        totalAmount = o.TotalAmount,
                        itemCount = o.OrderItems.Count
                    })
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error retrieving user orders");
                return StatusCode(500, new { message = "An error occurred while retrieving orders" });
            }
        }

        [HttpPost("{id}/reserve")]
        public async Task<IActionResult> ReserveOrder(Guid id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var order = await _context.Orders
                    .Include(o => o.OrderItems)
                    .FirstOrDefaultAsync(o => o.Id == id);

                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (order.UserId != userId)
                {
                    return Forbid();
                }

                if (order.Status != OrderStatus.Pending)
                {
                    return BadRequest(new { message = $"Cannot reserve order in {order.Status} status" });
                }

                var user = await _context.Users.FindAsync(userId);

                // Publish OrderReservedEvent
                var orderReservedEvent = new OrderReservedEvent
                {
                    OrderId = order.Id,
                    OrderNumber = order.OrderNumber,
                    UserId = userId,
                    UserEmail = user?.Email ?? string.Empty,
                    ReservedAt = DateTime.UtcNow,
                    ReservedItems = order.OrderItems.Select(i => new ReservedItemDto
                    {
                        ProductId = i.ProductId,
                        ProductName = i.ProductName,
                        ReservedQuantity = i.Quantity,
                        InventoryReservationId = Guid.NewGuid().ToString()
                    }).ToList()
                };

                await _eventBus.PublishAsync("order.exchange", "order.reserved", orderReservedEvent);

                _logger.LogInformation("Order reserved event published: {OrderNumber}", order.OrderNumber);

                return Ok(new
                {
                    message = "Order reservation initiated",
                    orderId = order.Id,
                    orderNumber = order.OrderNumber
                });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error reserving order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while reserving the order" });
            }
        }

        [HttpDelete("{id}")]
        public async Task<IActionResult> CancelOrder(Guid id)
        {
            try
            {
                var userId = GetUserId();
                if (userId == Guid.Empty)
                {
                    return Unauthorized(new { message = "Invalid user token" });
                }

                var order = await _context.Orders.FindAsync(id);
                if (order == null)
                {
                    return NotFound(new { message = "Order not found" });
                }

                if (order.UserId != userId)
                {
                    return Forbid();
                }

                if (order.Status == OrderStatus.Shipped || order.Status == OrderStatus.Delivered)
                {
                    return BadRequest(new { message = "Cannot cancel order that has been shipped or delivered" });
                }

                order.Status = OrderStatus.Cancelled;
                order.UpdatedAt = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("Order cancelled: {OrderNumber}", order.OrderNumber);

                return Ok(new { message = "Order cancelled successfully", orderId = order.Id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error cancelling order {OrderId}", id);
                return StatusCode(500, new { message = "An error occurred while cancelling the order" });
            }
        }

        private Guid GetUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                           ?? User.FindFirst(JwtRegisteredClaimNames.Sub)?.Value;

            return Guid.TryParse(userIdClaim, out var userId) ? userId : Guid.Empty;
        }

        private string GenerateOrderNumber()
        {
            return $"ORD-{DateTime.UtcNow:yyyyMMdd}-{Guid.NewGuid().ToString().Substring(0, 8).ToUpper()}";
        }
    }

    public class CreateOrderRequest
    {
        public string ShippingAddress { get; set; } = string.Empty;
        public List<CreateOrderItemRequest> Items { get; set; } = new();
    }

    public class CreateOrderItemRequest
    {
        public string ProductId { get; set; } = string.Empty;
        public string ProductName { get; set; } = string.Empty;
        public int Quantity { get; set; }
        public decimal UnitPrice { get; set; }
    }
}