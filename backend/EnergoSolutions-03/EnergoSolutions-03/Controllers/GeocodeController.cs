using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO;
using EnergoSolutions_03.DTO.Geocode;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers;

[ApiController]
[Route("api/[controller]")]
public class GeocodeController : ControllerBase
{
    private readonly IGeocodingService _geo;

    public GeocodeController(IGeocodingService geo)
    {
        _geo = geo;
    }

    [HttpGet]
    [ProducesResponseType(typeof(GeocodeResponseDto), StatusCodes.Status200OK)]
    [ProducesResponseType(StatusCodes.Status404NotFound)]
    public async Task<IActionResult> Get([FromQuery] GeocodeRequestDto request)
    {
        if (string.IsNullOrWhiteSpace(request.Address))
            return BadRequest("Address is required.");

        var result = await _geo.GeocodeAsync(request.Address);

        if (result is null)
            return NotFound();

        return Ok(result);
    }
}