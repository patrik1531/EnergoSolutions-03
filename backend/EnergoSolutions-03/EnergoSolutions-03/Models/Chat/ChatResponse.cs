namespace EnergoSolutions_03.Models.Chat;

public class ChatResponse
{
    public string SessionId { get; set; }
    public string Message { get; set; }
    public bool IsComplete { get; set; }
    public int Progress { get; set; }
}