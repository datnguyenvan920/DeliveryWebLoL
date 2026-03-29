using DeliveryWebLoL.DTO.Manager;
using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace DeliveryWebLoL.Controllers;

[ApiController]
[Route("manager")]
[Authorize]
public class ManagerController : ControllerBase
{
    private readonly IManagerService _managerService;

    public ManagerController(IManagerService managerService)
    {
        _managerService = managerService;
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

        var ok = await _managerService.AddDelivererAsync(userId.Value, req);
        if (!ok)
            return BadRequest(new { message = "Failed to add deliverer. Check warehouse ownership, deliverer role, and affiliation state." });

        return Ok(new { message = "Deliverer added" });
    }
}
