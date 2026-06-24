using DeliveryWebLoL.DTO.Affiliate;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeliveryWebLoL.Controllers
{
    [ApiController]
    [Route("affiliate")]
    [Authorize]
    public class AffiliateController : ControllerBase
    {
        private readonly IAffiliateService _affiliateService;

        public AffiliateController(IAffiliateService affiliateService)
        {
            _affiliateService = affiliateService;
        }

        private Guid? GetUserIdFromClaims()
        {
            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        [HttpGet("context")]
        public async Task<IActionResult> GetMyContext()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var ctx = await _affiliateService.GetMyContextAsync(userId.Value);
            return Ok(ctx);
        }

        [HttpGet("warehouse-items")]
        public async Task<IActionResult> GetMyWarehouseItems()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var items = await _affiliateService.GetMyWarehouseItemsAsync(userId.Value);
            return Ok(items);
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetMyOrders()
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var orders = await _affiliateService.GetMyOrdersAsync(userId.Value);
            return Ok(orders);
        }

        [HttpPost("order")]
        public async Task<IActionResult> CreateOrder([FromBody] CreateAffiliateOrderRequestDto req)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var created = await _affiliateService.CreateOrderAsync(userId.Value, req);
            return Ok(created);
        }

        [HttpPost("orders/complete")]
        public async Task<IActionResult> CompleteOrder([FromBody] CompleteOrderRequestDto req)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            if (req == null || req.OrderId == Guid.Empty) return BadRequest(new { message = "OrderId is required." });

            var ok = await _affiliateService.CompleteOrderAsync(userId.Value, req.OrderId);
            if (!ok) return BadRequest(new { message = "Unable to complete order. Only Delivered orders for your location can be completed." });

            return Ok(new { message = "Order completed" });
        }

        [HttpPost("orders/cancel")]
        public async Task<IActionResult> CancelOrder([FromBody] CancelOrderRequestDto req)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            if (req == null || req.OrderId == Guid.Empty) return BadRequest(new { message = "OrderId is required." });

            var ok = await _affiliateService.CancelOrderAsync(userId.Value, req.OrderId);
            if (!ok) return BadRequest(new { message = "Unable to cancel order. Only your own Pending orders can be cancelled." });

            return Ok(new { message = "Order cancelled" });
        }
    }
}
