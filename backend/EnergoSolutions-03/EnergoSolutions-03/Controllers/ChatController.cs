using EnergoSolutions_03.Agents;
using EnergoSolutions_03.Models.Chat;
using Microsoft.AspNetCore.Mvc;

[ApiController]
[Route("api/[controller]")]
public class ChatController : ControllerBase
{
    private readonly IAgentOrchestrator _orchestrator;

    public ChatController(IAgentOrchestrator orchestrator)
    {
        _orchestrator = orchestrator;
    }

    [HttpPost("message")]
    public async Task<IActionResult> SendMessage([FromBody] ChatRequest request)
    {
        try
        {
            var response = await _orchestrator.ProcessMessage(request.SessionId, request.Message);
            return Ok(new ChatResponse 
            { 
                Message = response.Message,
                IsComplete = response.IsComplete,
                SessionId = response.SessionId,
                Progress = response.Progress
            });
        }
        catch (Exception ex)
        {
            return BadRequest(new { error = ex.Message });
        }
    }

    [HttpPost("start")]
    public async Task<IActionResult> StartChat()
    {
        var sessionId = await _orchestrator.StartNewSession();
        var welcomeMessage = await _orchestrator.ProcessMessage(sessionId, "");
            
        return Ok(new ChatResponse
        {
            SessionId = sessionId,
            Message = welcomeMessage.Message,
            IsComplete = false,
            Progress = 0
        });
    }

    [HttpGet("status/{sessionId}")]
    public async Task<IActionResult> GetStatus(string sessionId)
    {
        var status = await _orchestrator.GetSessionStatus(sessionId);
        return Ok(status);
    }
}