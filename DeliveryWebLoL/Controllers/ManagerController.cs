using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;
using DeliveryWebLoL.Models;

namespace DeliveryWebLoL.Controllers;

[ApiController]
[Route("manager")]
[Authorize]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;
    private readonly IManagerItemService _managerItemService;
    private readonly IWarehouseInventoryService _warehouseInventoryService;
    private readonly IWarehouseItemUpdateService _warehouseItemUpdateService;
    private readonly IManagerOrderService _managerOrderService;
    private readonly IOrderApprovalService _orderApprovalService;

    public ManagerController(
        IManagerService managerService,
        IManagerItemService managerItemService,
        IWarehouseInventoryService warehouseInventoryService,
        IWarehouseItemUpdateService warehouseItemUpdateService,
        IManagerOrderService managerOrderService,
        IOrderApprovalService orderApprovalService)
    {
        _managerService = managerService;
        _managerItemService = managerItemService;
        _warehouseInventoryService = warehouseInventoryService;
        _warehouseItemUpdateService = warehouseItemUpdateService;
        _managerOrderService = managerOrderService;
        _orderApprovalService = orderApprovalService;
    }

    private Guid? GetUserIdFromClaims()
    {
        // Our JWT uses JwtRegisteredClaimNames.Sub ("sub") as the user id.
        var userId = User.FindFirstValue("sub") ?? User.FindFirstValue(ClaimTypes.NameIdentifier);
        return Guid.TryParse(userId, out var id) ? id : null;
    }

    [HttpGet("warehouses")]
    public async Task<IActionResult> GetOwnedWarehouses([FromQuery] ManagerListRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _managerService.GetOwnedWarehousesAsync(userId.Value, req);
        return Ok(result);
    }

    [HttpGet("deliverers")]
    public async Task<IActionResult> GetDeliverersForOwnedWarehouses([FromQuery] ManagerListRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _managerService.GetDeliverersForOwnedWarehousesAsync(userId.Value, req);
        return Ok(result);
    }

    [HttpPost("home")]
    public async Task<IActionResult> GetHome([FromBody] ManagerHomeRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        req ??= new ManagerHomeRequestDto();

        var warehouses = await _managerService.GetOwnedWarehousesAsync(userId.Value, req.Warehouses);
        var deliverers = await _managerService.GetDeliverersForOwnedWarehousesAsync(userId.Value, req.Deliverers);

        return Ok(new ManagerHomeResponseDto
        {
            Warehouses = warehouses,
            Deliverers = deliverers
        });
    }

    [HttpPost("warehouse")]
    public async Task<IActionResult> AddWarehouse([FromBody] AddWarehouseRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var created = await _managerService.AddWarehouseAsync(userId.Value, req);
        return Ok(created);
    }

    [HttpPost("deliverer")]
    public async Task<IActionResult> AddDeliverer([FromBody] AddDelivererRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        try
        {
            var ok = await _managerService.AddDelivererAsync(userId.Value, req);
            if (!ok)
                return BadRequest(new
                {
                    message = "Failed to add affiliate/deliverer. Possible reasons: warehouse not owned by you, warehouse already linked to an affiliate, invalid deliverer, or invalid affiliate primary location."
                });

            return Ok(new { message = "Deliverer added" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPost("warehouse-item")]
    public async Task<IActionResult> CreateWarehouseItem([FromBody] CreateWarehouseItemRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        try
        {
            var created = await _managerItemService.CreateWarehouseItemAsync(userId.Value, req);
            return Ok(created);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("warehouse-items")]
    public async Task<IActionResult> GetWarehouseItems([FromQuery] Guid warehouseLocationId)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();
        if (warehouseLocationId == Guid.Empty) return BadRequest(new { message = "warehouseLocationId is required." });

        try
        {
            var items = await _warehouseInventoryService.GetWarehouseItemsAsync(userId.Value, warehouseLocationId);
            return Ok(items);
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
        catch (InvalidOperationException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpPut("warehouse-item")]
    public async Task<IActionResult> UpdateWarehouseItem([FromBody] UpdateWarehouseItemRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        try
        {
            var ok = await _warehouseItemUpdateService.UpdateWarehouseItemAsync(userId.Value, req);
            if (!ok)
            {
                return BadRequest(new { message = "Failed to update warehouse item. Check warehouse ownership and item existence." });
            }

            return Ok(new { message = "Item updated" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("affiliate-users")]
    public async Task<IActionResult> GetAffiliateUsersForOwnedWarehouses([FromQuery] ManagerListRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var result = await _managerService.GetAffiliateUsersForOwnedWarehousesAsync(userId.Value, req);
        return Ok(result);
    }

    [HttpGet("orders")]
    public async Task<IActionResult> GetOrders([FromQuery] int take = 200)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var rows = await _managerOrderService.GetOrdersForOwnedWarehousesAsync(userId.Value, take);
        return Ok(rows);
    }

    [HttpGet("orders/{orderId:guid}")]
    public async Task<IActionResult> GetOrderDetail([FromRoute] Guid orderId)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();
        if (orderId == Guid.Empty) return BadRequest(new { message = "orderId is required." });

        var dto = await _orderApprovalService.GetOrderDetailForManagerAsync(userId.Value, orderId);
        if (dto == null) return NotFound();

        return Ok(dto);
    }

    [HttpPost("orders/approve")]
    public async Task<IActionResult> ApproveOrder([FromBody] ApproveOrderRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();
        if (req == null || req.OrderId == Guid.Empty) return BadRequest(new { message = "OrderId is required." });

        var ok = await _orderApprovalService.ApproveOrderAsync(userId.Value, req.OrderId);
        if (!ok) return BadRequest(new { message = "Unable to approve order. Only Pending orders from your warehouses can be approved." });

        return Ok(new { message = "Order approved", status = OrderStatus.ReadyForPickup.ToString() });
    }

    [HttpPut("warehouse")]
    public async Task<IActionResult> UpdateWarehouse([FromBody] UpdateWarehouseRequestDto req)
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        try
        {
            var ok = await _managerService.UpdateWarehouseAsync(userId.Value, req);
            if (!ok) return BadRequest(new { message = "Failed to update warehouse. Check ownership and payload." });
            return Ok(new { message = "Warehouse updated" });
        }
        catch (ArgumentException ex)
        {
            return BadRequest(new { message = ex.Message });
        }
    }

    [HttpGet("affiliate-warehouses")]
    public async Task<IActionResult> GetOwnedAffiliateWarehouses()
    {
        var userId = GetUserIdFromClaims();
        if (userId == null) return Unauthorized();

        var ids = await _managerService.GetOwnedWarehouseIdsWithAffiliateWarehouseLinkAsync(userId.Value);
        return Ok(ids);
    }
}
