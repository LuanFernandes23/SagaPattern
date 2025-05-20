using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SagaPedidos.Domain.Messages;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    public abstract class Subscriber : IDisposable
    {
        private readonly RabbitMQConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly string _exchangeName;
        private bool _disposed;

        protected Subscriber(RabbitMQConnection connection, string exchangeName, string queueName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));
            _channel = _connection.CreateModel();
            
            SetupInfrastructure();
        }

        private void SetupInfrastructure()
        {
            _channel.QueueDeclare(
                queue: _queueName,
                durable: true,
                exclusive: false,
                autoDelete: false);

            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);

            _channel.QueueBind(
                queue: _queueName,
                exchange: _exchangeName,
                routingKey: "");
            
            _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);
        }

        public void Subscribe()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));

            var consumer = new EventingBasicConsumer(_channel);
            
            consumer.Received += async (model, ea) =>
            {
                try
                {
                    var body = ea.Body.ToArray();
                    var messageContent = Encoding.UTF8.GetString(body);
                    var message = JsonSerializer.Deserialize<SagaMessage>(messageContent);

                    await ProcessMessageAsync(message);

                    _channel.BasicAck(ea.DeliveryTag, multiple: false);
                }
                catch (Exception ex)
                {
                    // Em caso de falha, rejeitar a mensagem e possivelmente enfileirá-la novamente
                    _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: true);
                    Console.WriteLine($"Error processing message: {ex.Message}");
                }
            };

            _channel.BasicConsume(
                queue: _queueName,
                autoAck: false,
                consumer: consumer);
        }

        protected abstract Task ProcessMessageAsync(SagaMessage message);

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _channel?.Dispose();
        }
    }
}