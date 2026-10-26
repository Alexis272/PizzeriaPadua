namespace PizzeriaPadua.API.Services
{
    public interface ISmsService
    {
        Task SendAsync(string to, string message);
    }
}
