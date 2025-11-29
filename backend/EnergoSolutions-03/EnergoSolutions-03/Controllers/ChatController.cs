using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.DTO.Chat;
using Microsoft.AspNetCore.Mvc;

namespace EnergoSolutions_03.Controllers.Chat;

[ApiController]
[Route("api/chat")]
public class ChatController : ControllerBase
{
    private readonly IChatService _service;

    public ChatController(IChatService service)
    {
        _service = service;
    }

    [HttpPost]
    public async Task<IActionResult> Ask([FromBody] ChatRequestDto dto)
    {
        if (!ModelState.IsValid)
            return BadRequest(ModelState);

        var result = await _service.AskAsync(dto);
        if (result == null)
            return StatusCode(502, "Failed to reach OpenAI");

        return Ok(result);
    }
}