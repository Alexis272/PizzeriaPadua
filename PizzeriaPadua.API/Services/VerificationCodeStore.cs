using System.Collections.Concurrent;

namespace PizzeriaPadua.API.Services
{
    public class CodeEntry
    {
        public string Code { get; set; } = "";
        public DateTime Expiry { get; set; }
        public bool Verified { get; set; }
    }

    public class VerificationCodeStore
    {
        private readonly ConcurrentDictionary<string, CodeEntry> _store = new(StringComparer.OrdinalIgnoreCase);

        public string Generate(string email)
        {
            var code = Random.Shared.Next(100000, 999999).ToString();
            _store[email] = new CodeEntry
            {
                Code = code,
                Expiry = DateTime.UtcNow.AddMinutes(10),
                Verified = false
            };
            return code;
        }

        public bool Verify(string email, string code)
        {
            if (_store.TryGetValue(email, out var entry))
            {
                if (entry.Code == code && entry.Expiry > DateTime.UtcNow)
                {
                    entry.Verified = true;
                    return true;
                }
            }
            return false;
        }

        public bool IsVerified(string email)
        {
            return _store.TryGetValue(email, out var entry) && entry.Verified;
        }

        public void Remove(string email)
        {
            _store.TryRemove(email, out _);
        }
    }
}
