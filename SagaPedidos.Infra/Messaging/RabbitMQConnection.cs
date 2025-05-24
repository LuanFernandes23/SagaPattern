using RabbitMQ.Client;
using System;
using System.Net.Security;
using System.Security.Authentication;
using System.Threading;

namespace SagaPedidos.Infra.Messaging
{
    public class RabbitMQConnection : IDisposable
    {
        private readonly string _connectionString;
        private IConnection _connection;
        private bool _disposed;
        private readonly object _syncRoot = new object();

        public RabbitMQConnection(string connectionString)
        {
            _connectionString = connectionString ?? throw new ArgumentNullException(nameof(connectionString));
            Console.WriteLine($"Inicializando RabbitMQConnection com: {_connectionString}");
        }

        public bool IsConnected => _connection != null && _connection.IsOpen && !_disposed;

        public IConnection GetConnection()
        {
            if (IsConnected)
                return _connection;

            lock (_syncRoot)
            {
                if (IsConnected)
                    return _connection;

                var retryCount = 5;
                while (retryCount > 0)
                {
                    try
                    {
                        Console.WriteLine($"Tentativa {6 - retryCount} de conexão ao RabbitMQ...");

                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(_connectionString),
                            AutomaticRecoveryEnabled = true,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(10),
                            RequestedHeartbeat = TimeSpan.FromSeconds(30)
                        };

                        // Se utilizando amqps:// precisa configurar SSL
                        if (_connectionString.StartsWith("amqps://", StringComparison.OrdinalIgnoreCase))
                        {
                            Console.WriteLine("Configurando SSL para conexão segura...");
                            factory.Ssl = new SslOption
                            {
                                Enabled = true,
                                Version = SslProtocols.Tls12,
                                ServerName = new Uri(_connectionString).Host,
                                // Em ambiente de desenvolvimento/teste às vezes é necessário aceitar certos erros de certificado
                                AcceptablePolicyErrors = SslPolicyErrors.RemoteCertificateNameMismatch |
                                                        SslPolicyErrors.RemoteCertificateChainErrors
                            };
                        }

                        Console.WriteLine("Criando conexão...");
                        _connection = factory.CreateConnection();
                        Console.WriteLine($"Conexão estabelecida com sucesso! Conectado a: {_connection.Endpoint.HostName}");
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount--;
                        Console.WriteLine($"Falha na tentativa de conexão ao RabbitMQ: {ex.Message}");
                        Console.WriteLine($"StackTrace: {ex.StackTrace}");
                        Console.WriteLine($"Tentativas restantes: {retryCount}");

                        if (retryCount == 0)
                        {
                            Console.WriteLine("Número máximo de tentativas atingido. Lançando exceção.");
                            throw;
                        }

                        Thread.Sleep(2000);
                    }
                }

                return _connection;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
            {
                Console.WriteLine("Conexão não estabelecida, tentando reconectar...");
                GetConnection();
            }

            Console.WriteLine("Criando canal de comunicação...");
            var channel = _connection.CreateModel();
            Console.WriteLine("Canal criado com sucesso!");
            return channel;
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            try
            {
                _connection?.Close();
                _connection?.Dispose();
                Console.WriteLine("Conexão com RabbitMQ fechada e liberada com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao fechar conexão com RabbitMQ: {ex.Message}");
            }
        }
    }
}