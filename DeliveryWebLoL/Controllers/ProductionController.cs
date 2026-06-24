using DeliveryWebLoL.Service.Interfaces;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace DeliveryWebLoL.Controllers
{
    [ApiController]
    [Route("production")]
    [Authorize]
    public class ProductionController : ControllerBase
    {
        private readonly IProductionService _productionService;

        public ProductionController(IProductionService productionService)
        {
            _productionService = productionService;
        }

        // Updates inventory for all items in a location based on LocationItemProduction rules.
        [HttpPost("apply")]
        public async Task<IActionResult> ApplyForLocation([FromQuery] Guid locationId)
        {
            if (locationId == Guid.Empty)
                return BadRequest(new { message = "locationId is required." });

            await _productionService.ApplyProductionForLocationAsync(locationId);
            return Ok(new { message = "Production applied", locationId });
        }
    }
}
