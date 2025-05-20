using RabbitMQ.Client;
using System;
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
                        var factory = new ConnectionFactory
                        {
                            Uri = new Uri(_connectionString),
                            AutomaticRecoveryEnabled = true,
                            NetworkRecoveryInterval = TimeSpan.FromSeconds(10)
                        };

                        _connection = factory.CreateConnection();
                        break;
                    }
                    catch (Exception ex)
                    {
                        retryCount--;
                        Console.WriteLine($"RabbitMQ connection attempt failed: {ex.Message}. Retries left: {retryCount}");
                        
                        if (retryCount == 0)
                            throw;

                        Thread.Sleep(2000);
                    }
                }

                return _connection;
            }
        }

        public IModel CreateModel()
        {
            if (!IsConnected)
                throw new InvalidOperationException("No RabbitMQ connection is available");

            return _connection.CreateModel();
        }

        public void Dispose()
        {
            if (_disposed) return;

            _disposed = true;
            _connection?.Dispose();
        }
    }
}
