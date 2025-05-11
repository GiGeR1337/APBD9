using apbd9.Models;
using apbd9.Services;
using Microsoft.AspNetCore.Mvc;

namespace apbd9.Controllers;

[ApiController]
[Route("api/[controller]")]
public class WarehouseController : ControllerBase
{
    private readonly IDbService _dbService;

    public WarehouseController(IDbService dbService)
    {
        _dbService = dbService;
    }

    [HttpPost("manual")]
    public async Task<IActionResult> AddProductManual([FromBody] Warehouse warehouse)
    {
        try
        {
            var result = await _dbService.AddProductManuallyAsync(warehouse);
            return Ok(new { NewId = result });
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch (Exception e)
        {
            // TEMP: For debugging only
            return StatusCode(500, $"Internal server error: {e.Message}");
        }
    }

    [HttpPost("procedure")]
    public async Task<IActionResult> AddProductUsingProcedure([FromBody] Warehouse warehouse)
    {
        try
        {
            var result = await _dbService.AddProductWithProcedureAsync(warehouse);
            return Ok(new { NewId = result });
        }
        catch (ArgumentException e)
        {
            return BadRequest(e.Message);
        }
        catch
        {
            return StatusCode(500, "Internal server error");
        }
    }
}