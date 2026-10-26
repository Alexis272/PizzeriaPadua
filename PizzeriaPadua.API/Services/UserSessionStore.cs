using System.Collections.Concurrent;
using System.Security.Cryptography;
using Microsoft.AspNetCore.WebUtilities;

namespace PizzeriaPadua.API.Services
{
    public class UserSessionStore
    {
        private readonly ConcurrentDictionary<string, SessionEntry> _sessions = new();
        private readonly ConcurrentDictionary<int, SessionEntry> _lastLoginByUser = new();
        private static readonly TimeSpan OnlineWindow = TimeSpan.FromSeconds(45);
        private static readonly TimeSpan RetentionWindow = TimeSpan.FromHours(24);

        private record SessionEntry(int UserId, string Provider, DateTime LastSeenAt);

        public record SessionStatus(bool IsOnline, string? Provider, DateTime? LastSeenAt);

        public string Create(int userId, string provider)
        {
            Cleanup();
            var token = WebEncoders.Base64UrlEncode(RandomNumberGenerator.GetBytes(32));
            var session = new SessionEntry(userId, provider, DateTime.UtcNow);
            _sessions[token] = session;
            _lastLoginByUser[userId] = session;
            return token;
        }

        public bool Touch(int userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var session) || session.UserId != userId)
                return false;

            var updated = session with { LastSeenAt = DateTime.UtcNow };
            _sessions[token] = updated;
            _lastLoginByUser[userId] = updated;
            return true;
        }

        public bool IsValid(int userId, string token)
        {
            Cleanup();
            return !string.IsNullOrWhiteSpace(token)
                && _sessions.TryGetValue(token, out var session)
                && session.UserId == userId;
        }

        public bool End(int userId, string token)
        {
            if (string.IsNullOrWhiteSpace(token) || !_sessions.TryGetValue(token, out var session) || session.UserId != userId)
                return false;

            return _sessions.TryRemove(token, out _);
        }

        public int RevokeUser(int userId)
        {
            var tokens = _sessions
                .Where(pair => pair.Value.UserId == userId)
                .Select(pair => pair.Key)
                .ToList();

            var removed = 0;
            foreach (var token in tokens)
            {
                if (_sessions.TryRemove(token, out _)) removed++;
            }

            return removed;
        }

        public SessionStatus GetStatus(int userId)
        {
            Cleanup();
            var latest = _sessions.Values
                .Where(session => session.UserId == userId)
                .OrderByDescending(session => session.LastSeenAt)
                .FirstOrDefault();

            if (latest == null)
            {
                if (_lastLoginByUser.TryGetValue(userId, out var lastLogin))
                    return new SessionStatus(false, lastLogin.Provider, lastLogin.LastSeenAt);

                return new SessionStatus(false, null, null);
            }

            return new SessionStatus(
                latest.LastSeenAt >= DateTime.UtcNow.Subtract(OnlineWindow),
                latest.Provider,
                latest.LastSeenAt);
        }

        private void Cleanup()
        {
            var cutoff = DateTime.UtcNow.Subtract(RetentionWindow);
            foreach (var pair in _sessions)
            {
                if (pair.Value.LastSeenAt < cutoff)
                    _sessions.TryRemove(pair.Key, out _);
            }
        }
    }
}
