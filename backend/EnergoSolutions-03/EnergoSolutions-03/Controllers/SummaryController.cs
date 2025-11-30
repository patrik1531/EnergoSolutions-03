using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Summary;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers;

[ApiController]
[Route("api/[controller]")]
public class SummaryController : ControllerBase
{
    private readonly ISummaryService _service;

    public SummaryController(ISummaryService service)
    {
        _service = service;
    }

    [HttpPost("{lat}/{lon}")]
    public async Task<IActionResult> GetSummary([FromRoute] float lat, [FromRoute] float lon)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.BuildSummaryAsync(lat, lon);
        return Ok(result);
    }
}