using System.Collections.Concurrent;

namespace PizzeriaPadua.API.Services
{
    public class EventBroadcaster
    {
        private readonly ConcurrentDictionary<StreamWriter, byte> _clients = new();
        private readonly SemaphoreSlim _notifyLock = new(1, 1);

        public void AddClient(StreamWriter writer)
        {
            _clients.TryAdd(writer, 0);
        }

        public void RemoveClient(StreamWriter writer)
        {
            _clients.TryRemove(writer, out _);
        }

        public async Task NotifyAsync(string eventType, string data)
        {
            await _notifyLock.WaitAsync();
            try
            {
                var dead = new List<StreamWriter>();
                foreach (var writer in _clients.Keys)
                {
                    try
                    {
                        await writer.WriteAsync($"event: {eventType}\n");
                        await writer.WriteAsync($"data: {data}\n\n");
                        await writer.FlushAsync();
                    }
                    catch
                    {
                        dead.Add(writer);
                    }
                }
                foreach (var writer in dead)
                    _clients.TryRemove(writer, out _);
            }
            finally
            {
                _notifyLock.Release();
            }
        }
    }
}
