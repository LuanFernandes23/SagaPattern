using RabbitMQ.Client;
using SagaPedidos.Domain.Messages;
using System;
using System.Text;
using System.Text.Json;

namespace SagaPedidos.Infra.Messaging
{
    public class Publisher : IDisposable
    {
        private readonly RabbitMQConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private bool _disposed;

        public Publisher(RabbitMQConnection connection, string exchangeName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _channel = _connection.CreateModel();
            
            DeclareExchange();
        }

        private void DeclareExchange()
        {
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);
        }

        public void Publish(SagaMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Publisher));

            var body = Encoding.UTF8.GetBytes(JsonSerializer.Serialize(message));

            var properties = _channel.CreateBasicProperties();
            properties.DeliveryMode = 2; // persistent
            properties.MessageId = message.Id.ToString();
            properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
            properties.ContentType = "application/json";

            _channel.BasicPublish(
                exchange: _exchangeName,
                routingKey: "",  // Irrelevante para exchange tipo fanout
                basicProperties: properties,
                body: body);
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _channel?.Dispose();
        }
    }
}
