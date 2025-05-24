using RabbitMQ.Client;
using SagaPedidos.Domain.Interfaces;
using SagaPedidos.Domain.Messages;
using System;
using System.Text;
using System.Text.Json;

namespace SagaPedidos.Infra.Messaging
{
    public class Publisher : IPublisher, IDisposable
    {
        private readonly RabbitMQConnection _connection;
        private readonly IModel _channel;
        private readonly string _exchangeName;
        private bool _disposed;

        public Publisher(RabbitMQConnection connection, string exchangeName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));

            Console.WriteLine($"Inicializando Publisher para exchange '{_exchangeName}'...");

            try
            {
                _channel = _connection.CreateModel();
                DeclareExchange();
                Console.WriteLine($"Publisher inicializado com sucesso para exchange '{_exchangeName}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar Publisher: {ex.Message}");
                throw;
            }
        }

        private void DeclareExchange()
        {
            Console.WriteLine($"Declarando exchange '{_exchangeName}'...");
            _channel.ExchangeDeclare(
                exchange: _exchangeName,
                type: ExchangeType.Fanout,
                durable: true,
                autoDelete: false);
            Console.WriteLine($"Exchange '{_exchangeName}' declarada com sucesso");
        }

        public void Publish(SagaMessage message)
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Publisher));

            try
            {
                var messageJson = JsonSerializer.Serialize(message);
                Console.WriteLine($"Publicando mensagem: {messageJson}");

                var body = Encoding.UTF8.GetBytes(messageJson);

                var properties = _channel.CreateBasicProperties();
                properties.DeliveryMode = 2; // persistent
                properties.MessageId = message.Id.ToString();
                properties.Timestamp = new AmqpTimestamp(DateTimeOffset.UtcNow.ToUnixTimeSeconds());
                properties.ContentType = "application/json";

                // Adiciona os headers à mensagem
                if (message.Headers != null && message.Headers.Count > 0)
                {
                    properties.Headers = new System.Collections.Generic.Dictionary<string, object>();
                    foreach (var header in message.Headers)
                    {
                        properties.Headers.Add(header.Key, header.Value);
                    }
                }

                _channel.BasicPublish(
                    exchange: _exchangeName,
                    routingKey: "",  // Irrelevante para exchange tipo fanout
                    basicProperties: properties,
                    body: body);

                Console.WriteLine($"Mensagem publicada com sucesso na exchange '{_exchangeName}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao publicar mensagem: {ex.Message}");
                throw;
            }
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                Console.WriteLine("Publisher liberado com sucesso");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao liberar Publisher: {ex.Message}");
            }
        }
    }
}