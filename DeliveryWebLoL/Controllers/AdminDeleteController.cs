using DeliveryWebLoL.Data;
using DeliveryWebLoL.Models;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using System.Security.Claims;

namespace DeliveryWebLoL.Controllers;

/// <summary>
/// Intentionally not wired to any UI flow.
/// Provides an admin-only hard delete endpoint for demo/testing.
/// </summary>
[ApiController]
[Route("admin")]
[Authorize]
public sealed class AdminDeleteController : ControllerBase
{
    private readonly ApplicationDbContext _db;

    public AdminDeleteController(ApplicationDbContext db)
    {
        _db = db;
    }

    /// <summary>
    /// Hard-delete a Location by id. This is destructive.
    /// Note: guarded by role check, but not used by the Razor Pages UI.
    /// </summary>
    [HttpDelete("locations/{locationId:guid}")]
    public async Task<IActionResult> DeleteLocation([FromRoute] Guid locationId)
    {
        if (locationId == Guid.Empty)
            return BadRequest(new { message = "locationId is required." });

        // JWT uses ClaimTypes.Role with the enum name (e.g. "Admin")
        var role = User.FindFirstValue(ClaimTypes.Role);
        if (!string.Equals(role, UserRole.Admin.ToString(), StringComparison.OrdinalIgnoreCase))
            return Forbid();

        var loc = await _db.Locations.SingleOrDefaultAsync(l => l.LocationID == locationId);
        if (loc == null) return NotFound(new { message = "Location not found." });

        _db.Locations.Remove(loc);

        try
        {
            await _db.SaveChangesAsync();
        }
        catch (DbUpdateException)
        {
            // Likely FK constraints; keep response generic.
            return BadRequest(new { message = "Unable to delete location due to existing references." });
        }

        return Ok(new { message = "Location deleted." });
    }
}
