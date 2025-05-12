using Azure.Messaging.ServiceBus;

public class AzureServiceBusService : IMessageBusService
{
    private readonly string _queueName;
    private readonly ServiceBusClient _client;
    private readonly ServiceBusSender _sender;

    public AzureServiceBusService(IConfiguration config)
    {
        var connectionString = config["ServiceBus:ConnectionString"];
        _queueName = config["ServiceBus:QueueName"];

        _client = new ServiceBusClient(connectionString);
        _sender = _client.CreateSender(_queueName);
    }

    public async Task SendMessageAsync(string message)
    {
        var msg = new ServiceBusMessage(message);
        await _sender.SendMessageAsync(msg);
    }
}
