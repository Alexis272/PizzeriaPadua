namespace PizzeriaPadua.API
{
    public class PizzeriaPaduaSettings
    {
        public string ConnectionStringPizzeriaPadua { get; set; } = string.Empty;
        public string EventBusConnection { get; set; } = string.Empty;
        public string EventBusUserName { get; set; } = string.Empty;
        public string EventBusPassword { get; set; } = string.Empty;
        public string SubscriptionClientName { get; set; } = string.Empty;
        public int EventBusRetryCount { get; set; } = 5;
    }
}
