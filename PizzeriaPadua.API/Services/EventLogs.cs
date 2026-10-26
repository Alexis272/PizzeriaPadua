namespace PizzeriaPadua.API.Services
{
    public class EventLogs
    {
        public void sendLog(Microsoft.Extensions.Logging.ILogger logger, string source, string userId, string userName, string logType, string message)
        {
            logger.LogInformation("[{Source}] [{LogType}] User: {UserName} - {Message}", source, logType, userName, message);
        }
    }
}
