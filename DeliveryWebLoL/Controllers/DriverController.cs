using DeliveryWebLoL.DTO.Driver;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeliveryWebLoL.Controllers
{
    [ApiController]
    [Route("driver")]
    public class DriverController : ControllerBase
    {
        private readonly IDriverOrderService _driverOrderService;

        public DriverController(IDriverOrderService driverOrderService)
        {
            _driverOrderService = driverOrderService;
        }

        private Guid? GetUserIdFromClaims()
        {
            var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
            return Guid.TryParse(userId, out var id) ? id : null;
        }

        [HttpGet("orders")]
        public async Task<IActionResult> GetOrders([FromQuery] int take = 200)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();

            var rows = await _driverOrderService.GetOrdersForDriverAsync(userId.Value, take);
            return Ok(rows);
        }

        [HttpPost("orders/status")]
        public async Task<IActionResult> UpdateStatus([FromBody] UpdateDriverOrderStatusRequestDto req)
        {
            var userId = GetUserIdFromClaims();
            if (userId == null) return Unauthorized();
            if (req == null || req.OrderId == Guid.Empty) return BadRequest(new { message = "OrderId is required." });

            var ok = await _driverOrderService.UpdateOrderStatusAsync(userId.Value, req.OrderId, req.NewStatus);
            if (!ok) return BadRequest(new { message = "Invalid status update." });

            return Ok(new { message = "Status updated" });
        }
    }
}
