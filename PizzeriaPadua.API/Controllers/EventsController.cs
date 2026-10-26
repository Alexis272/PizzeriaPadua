using PizzeriaPadua.API.Services;
using Microsoft.AspNetCore.Mvc;

namespace PizzeriaPadua.API.Controllers
{
    [Route("api/v1/[controller]")]
    [ApiController]
    public class EventsController : ControllerBase
    {
        private readonly EventBroadcaster _broadcaster;

        public EventsController(EventBroadcaster broadcaster)
        {
            _broadcaster = broadcaster;
        }

        [HttpGet("stream")]
        public async Task Stream()
        {
            Response.Headers["Content-Type"] = "text/event-stream";
            Response.Headers["Cache-Control"] = "no-cache";
            Response.Headers["Connection"] = "keep-alive";

            var writer = new StreamWriter(Response.Body);
            _broadcaster.AddClient(writer);

            try
            {
                await writer.WriteAsync("event: connected\ndata: {}\n\n");
                await writer.FlushAsync();

                await Task.Delay(Timeout.Infinite, HttpContext.RequestAborted);
            }
            catch (OperationCanceledException) { }
            finally
            {
                _broadcaster.RemoveClient(writer);
            }
        }
    }
}
