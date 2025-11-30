namespace EnergoSolutions_03.Abstraction;

public interface IOpenAIService
{
    Task<string> GetCompletion(string prompt);
}