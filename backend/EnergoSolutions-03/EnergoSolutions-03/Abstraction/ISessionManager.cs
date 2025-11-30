using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Abstraction;

public interface ISessionManager
{
    Task<string> CreateSession();
    Task<Session> GetSession(string sessionId);
    Task UpdateSession(Session session);
}