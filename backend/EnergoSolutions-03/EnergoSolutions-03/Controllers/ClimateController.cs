using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO;
using EnergoSolutions_03.DTO.Climate;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers;

[ApiController]
[Route("api/[controller]")]
public class ClimateController : ControllerBase
{
    private readonly IClimateService _service;

    public ClimateController(IClimateService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] ClimateRequestDto dto)
    {
        ClimateResponseDto? result = await _service.GetClimateHeatingAsync(dto);
        if (result is null)
            return BadRequest();

        return Ok(result);
    }
}