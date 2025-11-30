using EnergoSolutions_03.Abstraction;
using EnergoSolutions_03.Models.Agent;

namespace EnergoSolutions_03.Services;

public class SessionManager : ISessionManager
{
    private readonly Dictionary<string, Session> _sessions = new();

    public Task<string> CreateSession()
    {
        // var sessionId = Guid.NewGuid().ToString();
        var sessionId = "1";
        _sessions[sessionId] = new Session { SessionId = sessionId };
        return Task.FromResult(sessionId);
    }

    public Task<Session> GetSession(string sessionId)
    {
        if (_sessions.TryGetValue(sessionId, out var session))
        {
            return Task.FromResult(session);
        }
        throw new Exception("Session not found");
    }

    public Task UpdateSession(Session session)
    {
        session.UpdatedAt = DateTime.Now;
        _sessions[session.SessionId] = session;
        return Task.CompletedTask;
    }
}