using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Solar;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers;

[ApiController]
[Route("api/solar")]
public class SolarController : ControllerBase
{
    private readonly ISolarService _service;

    public SolarController(ISolarService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] SolarRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        SolarResponseDto? result = await _service.GetSolarResourceAsync(dto);
        if (result is null)
            return BadRequest();

        return Ok(result);
    }
}