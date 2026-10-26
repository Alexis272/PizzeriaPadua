namespace PizzeriaPadua.Core.Enums
{
    public enum OrderStatus
    {
        Pending = 0,
        InProgress = 1,
        Completed = 2,
        Cancelled = 3,
        ReadyForDelivery = 4,
        PaymentPending = 5,
        OpenAccount = 6,
        PartiallyRefunded = 7,
        Refunded = 8
    }
}
