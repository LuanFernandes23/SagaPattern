using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SagaPedidos.Application.EventHandlers;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Application.Services;
using SagaPedidos.Domain.Interfaces;
using SagaPedidos.Infra;
using SagaPedidos.Infra.Messaging;
using SagaPedidos.Infra.Messaging.Subscribers;
using SagaPedidos.Infra.Repositories;
using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace SagaPedidos.Presentation
{
    class Program
    {
        static async Task Main(string[] args)
        {
            Console.WriteLine("Iniciando a aplicação SagaPedidos...");

            try
            {
                // Carrega configurações
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: false, reloadOnChange: true)
                    .Build();

                // Teste rápido de conexão antes de iniciar toda a aplicação
                var connectionString = configuration["RabbitMQ:ConnectionString"];
                Console.WriteLine($"Testando conexão com RabbitMQ: {connectionString}");

                // Configura DI
                var serviceProvider = ConfigureServices(configuration);
                Console.WriteLine("Configuração de serviços concluída com sucesso.");

                // Inicializa os subscribers
                InitializeSubscribers(serviceProvider);

                Console.WriteLine("Aplicação iniciada com sucesso! Pressione Ctrl+C para encerrar.");

                var cts = new CancellationTokenSource();
                Console.CancelKeyPress += (s, e) =>
                {
                    e.Cancel = true;
                    cts.Cancel();
                };

                await Task.Delay(Timeout.Infinite, cts.Token)
                          .ContinueWith(t => { });
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Aplicação sendo encerrada...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a execução da aplicação: {ex.Message}");
                Console.WriteLine($"Stack Trace: {ex.StackTrace}");
                if (ex.InnerException != null)
                {
                    Console.WriteLine($"Inner Exception: {ex.InnerException.Message}");
                    Console.WriteLine($"Inner Exception Stack Trace: {ex.InnerException.StackTrace}");
                }
            }
            finally
            {
                Console.WriteLine("Pressione qualquer tecla para sair...");
                Console.ReadKey();
            }
        }

        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Injetar a configuração diretamente
            services.AddSingleton<IConfiguration>(configuration);

            // DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Repositórios
            services.AddScoped<IPedidoRepository, PedidoRepository>();
            services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            services.AddScoped<IEnvioRepository, EnvioRepository>();

            // Serviços de domínio
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IPagamentoService, PagamentoService>();
            services.AddScoped<IEnvioService, EnvioService>();

            // RabbitMQ
            var rabbitConnStr = configuration["RabbitMQ:ConnectionString"];
            var rabbitExchange = configuration["RabbitMQ:ExchangeName"];
            Console.WriteLine($"String de conexão RabbitMQ: {rabbitConnStr}");
            Console.WriteLine($"Exchange RabbitMQ: {rabbitExchange}");

            services.AddSingleton<RabbitMQConnection>(sp =>
                new RabbitMQConnection(rabbitConnStr));

            // Publisher (injeção do nome da exchange)
            services.AddSingleton<Publisher>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                return new Publisher(conn, rabbitExchange);
            });
            services.AddSingleton<IPublisher>(sp =>
                sp.GetRequiredService<Publisher>());

            // Orchestrator da Saga e Handlers
            services.AddSingleton<PedidoSagaOrchestrator>();
            services.AddSingleton<PedidoCriadoHandler>();

            // Subscribers - agora com o nome correto da exchange
            services.AddSingleton<PedidoSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var pedidoSrv = sp.GetRequiredService<IPedidoService>();
                var publisher = sp.GetRequiredService<Publisher>();
                return new PedidoSubscriber(conn, pedidoSrv, publisher, rabbitExchange, "pedido_queue");
            });

            services.AddSingleton<PagamentoSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var pagamentoSrv = sp.GetRequiredService<IPagamentoService>();
                var publisher = sp.GetRequiredService<Publisher>();
                var orchestrator = sp.GetRequiredService<PedidoSagaOrchestrator>();
                return new PagamentoSubscriber(conn, pagamentoSrv, publisher, orchestrator, rabbitExchange, "pagamento_queue");
            });

            services.AddSingleton<EnvioSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var envioSrv = sp.GetRequiredService<IEnvioService>();
                var orchestrator = sp.GetRequiredService<PedidoSagaOrchestrator>();
                return new EnvioSubscriber(conn, envioSrv, orchestrator, rabbitExchange, "envio_queue");
            });

            return services.BuildServiceProvider();
        }

        private static void InitializeSubscribers(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("Iniciando subscribers...");

                // Obtém as configurações do appsettings.json
                var configuration = serviceProvider.GetRequiredService<IConfiguration>();
                var exchangeName = configuration["RabbitMQ:ExchangeName"];

                Console.WriteLine($"Exchange configurada: {exchangeName}");

                var pedidoSub = serviceProvider.GetRequiredService<PedidoSubscriber>();
                var pagamentoSub = serviceProvider.GetRequiredService<PagamentoSubscriber>();
                var envioSub = serviceProvider.GetRequiredService<EnvioSubscriber>();

                pedidoSub.Subscribe();
                Console.WriteLine("Subscriber de Pedido iniciado com sucesso");

                pagamentoSub.Subscribe();
                Console.WriteLine("Subscriber de Pagamento iniciado com sucesso");

                envioSub.Subscribe();
                Console.WriteLine("Subscriber de Envio iniciado com sucesso");

                Console.WriteLine("Todos os subscribers foram iniciados com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar subscribers: {ex.Message}");
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}