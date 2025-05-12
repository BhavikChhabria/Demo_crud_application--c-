public interface IMessageBusService
{
    Task SendMessageAsync(string message);
}
