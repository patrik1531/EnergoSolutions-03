namespace EnergoSolutions_03.Abstraction;

public interface IOpenAIService
{
    Task<string> GetCompletion(string prompt);

    Task<string> CreateResponseAsync(string systemMessage, string userPrompt, string model = "gpt-4.1");
}