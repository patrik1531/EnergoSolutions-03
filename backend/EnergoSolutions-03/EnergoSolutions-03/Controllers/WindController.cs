using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Wind;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers.Wind;

[ApiController]
[Route("api/[controller]")]
public class WindController : ControllerBase
{
    private readonly IWindService _service;

    public WindController(IWindService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Calculate([FromBody] WindRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.GetWindStatsAsync(dto);
        if (result is null)
            return BadRequest();

        return Ok(result);
    }
}