using RabbitMQ.Client;
using RabbitMQ.Client.Events;
using SagaPedidos.Domain.Messages;
using System;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    public abstract class Subscriber : IDisposable
    {
        private readonly RabbitMQConnection _connection;
        private readonly IModel _channel;
        private readonly string _queueName;
        private readonly string _exchangeName;
        protected readonly IServiceProvider _serviceProvider;
        private bool _disposed;

        protected Subscriber(RabbitMQConnection connection, IServiceProvider serviceProvider, string exchangeName, string queueName)
        {
            _connection = connection ?? throw new ArgumentNullException(nameof(connection));
            _serviceProvider = serviceProvider ?? throw new ArgumentNullException(nameof(serviceProvider));
            _exchangeName = exchangeName ?? throw new ArgumentNullException(nameof(exchangeName));
            _queueName = queueName ?? throw new ArgumentNullException(nameof(queueName));

            Console.WriteLine($"Inicializando subscriber para exchange '{_exchangeName}' e fila '{_queueName}'...");

            try
            {
                _channel = _connection.CreateModel();
                SetupInfrastructure();
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao inicializar subscriber: {ex.Message}");
                throw;
            }
        }

        private void SetupInfrastructure()
        {
            Console.WriteLine("Configurando infraestrutura de mensageria...");

            try
            {
                // Declara a exchange
                Console.WriteLine($"Declarando exchange '{_exchangeName}'...");
                _channel.ExchangeDeclare(
                    exchange: _exchangeName,
                    type: ExchangeType.Fanout,
                    durable: true,
                    autoDelete: false);

                // Declara a fila
                Console.WriteLine($"Declarando fila '{_queueName}'...");
                _channel.QueueDeclare(
                    queue: _queueName,
                    durable: true,
                    exclusive: false,
                    autoDelete: false);

                // Liga a fila à exchange
                Console.WriteLine($"Bindando fila '{_queueName}' à exchange '{_exchangeName}'...");
                _channel.QueueBind(
                    queue: _queueName,
                    exchange: _exchangeName,
                    routingKey: "");

                _channel.BasicQos(prefetchSize: 0, prefetchCount: 1, global: false);

                Console.WriteLine("Infraestrutura configurada com sucesso!");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao configurar infraestrutura: {ex.Message}");
                throw;
            }
        }

        public void Subscribe()
        {
            if (_disposed)
                throw new ObjectDisposedException(nameof(Subscriber));

            try
            {
                Console.WriteLine($"Inicializando consumidor para fila '{_queueName}'...");

                var consumer = new EventingBasicConsumer(_channel);

                consumer.Received += async (model, ea) =>
                {
                    // Criamos um novo scope para cada mensagem processada
                    using var scope = _serviceProvider.CreateScope();
                    try
                    {
                        var body = ea.Body.ToArray();
                        var messageContent = Encoding.UTF8.GetString(body);
                        Console.WriteLine($"Mensagem recebida na fila '{_queueName}': {messageContent}");

                        var message = JsonSerializer.Deserialize<SagaMessage>(messageContent);

                        // Passamos o scope.ServiceProvider para o método de processamento
                        await ProcessMessageAsync(message, scope.ServiceProvider);

                        _channel.BasicAck(ea.DeliveryTag, multiple: false);
                        Console.WriteLine("Processamento concluído, confirmação enviada.");
                    }
                    catch (Exception ex)
                    {
                        // Em caso de falha, rejeitar a mensagem e possivelmente enfileirá-la novamente
                        _channel.BasicNack(ea.DeliveryTag, multiple: false, requeue: false); // Alterado para não reencaminhar para evitar loop infinito
                        Console.WriteLine("Erro ao processar mensagem: " + ex.Message);
                        Console.WriteLine("Stack trace: " + ex.StackTrace);
                    }
                };

                _channel.BasicConsume(
                    queue: _queueName,
                    autoAck: false,
                    consumer: consumer);

                Console.WriteLine($"Consumidor configurado com sucesso para a fila '{_queueName}'");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao configurar subscriber: {ex.Message}");
                throw;
            }
        }

        protected abstract Task ProcessMessageAsync(SagaMessage message, IServiceProvider serviceProvider);

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            try
            {
                _channel?.Close();
                _channel?.Dispose();
                Console.WriteLine($"Subscriber para fila '{_queueName}' liberado.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao liberar subscriber: {ex.Message}");
            }
        }
    }
}