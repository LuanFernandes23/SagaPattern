using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using SagaPedidos.Application.EventHandlers;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Application.Services;
using SagaPedidos.Domain.Interfaces;
using SagaPedidos.Domain.Messaging;
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
                // Configuração - versão simplificada
                var configuration = new ConfigurationBuilder()
                    .SetBasePath(Directory.GetCurrentDirectory())
                    .AddJsonFile("appsettings.json", optional: true, reloadOnChange: true)
                    .Build();

                // Configuração de serviços
                var serviceProvider = ConfigureServices(configuration);
                
                Console.WriteLine("Configuração de serviços concluída com sucesso.");
                
                // Inicializa os subscribers (filas de mensagens)
                InitializeSubscribers(serviceProvider);
                
                // Mantém o programa em execução até que seja interrompido manualmente
                Console.WriteLine("Aplicação iniciada com sucesso! Pressione Ctrl+C para encerrar.");
                
                // Aguardar sinal de cancelamento
                var cancellationTokenSource = new CancellationTokenSource();
                Console.CancelKeyPress += (sender, e) => {
                    e.Cancel = true;
                    cancellationTokenSource.Cancel();
                };
                
                await Task.Delay(Timeout.Infinite, cancellationTokenSource.Token).ContinueWith(t => { });
            }
            catch (TaskCanceledException)
            {
                Console.WriteLine("Aplicação sendo encerrada...");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro durante a execução da aplicação: {ex.Message}");
                Console.WriteLine(ex.ToString());
            }
        }

        private static ServiceProvider ConfigureServices(IConfiguration configuration)
        {
            var services = new ServiceCollection();

            // Configuração do DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // Registra os repositórios
            services.AddScoped<IPedidoRepository, PedidoRepository>();
            services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            services.AddScoped<IEnvioRepository, EnvioRepository>();

            // Registra os serviços de domínio
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IPagamentoService, PagamentoService>();
            services.AddScoped<IEnvioService, EnvioService>();

            // Configuração do RabbitMQ
            var rabbitMQConnectionString = configuration["RabbitMQ:ConnectionString"];
            Console.WriteLine($"String de conexão RabbitMQ: {rabbitMQConnectionString}");

            services.AddSingleton<RabbitMQConnection>(provider =>
                new RabbitMQConnection(rabbitMQConnectionString));

            // Registra o Publisher
            services.AddSingleton<Publisher>();
            services.AddSingleton<IPublisher>(provider => provider.GetRequiredService<Publisher>());

            // Registra o Orchestrator da Saga
            services.AddSingleton<PedidoSagaOrchestrator>();

            // Registra o Handler para iniciar a Saga
            services.AddSingleton<PedidoCriadoHandler>();

            // Registra os Subscribers
            services.AddSingleton<PedidoSubscriber>(provider =>
            {
                var connection = provider.GetRequiredService<RabbitMQConnection>();
                var pedidoService = provider.GetRequiredService<IPedidoService>();
                var publisher = provider.GetRequiredService<Publisher>();
                return new PedidoSubscriber(connection, pedidoService, publisher);
            });

            services.AddSingleton<PagamentoSubscriber>(provider =>
            {
                var connection = provider.GetRequiredService<RabbitMQConnection>();
                var pagamentoService = provider.GetRequiredService<IPagamentoService>();
                var publisher = provider.GetRequiredService<Publisher>();
                var orchestrator = provider.GetRequiredService<PedidoSagaOrchestrator>();
                return new PagamentoSubscriber(connection, pagamentoService, publisher, orchestrator);
            });

            services.AddSingleton<EnvioSubscriber>(provider =>
            {
                var connection = provider.GetRequiredService<RabbitMQConnection>();
                var envioService = provider.GetRequiredService<IEnvioService>();
                var orchestrator = provider.GetRequiredService<PedidoSagaOrchestrator>();
                return new EnvioSubscriber(connection, envioService, orchestrator);
            });

            // Construção do provedor de serviços
            return services.BuildServiceProvider();
        }

        private static void InitializeSubscribers(ServiceProvider serviceProvider)
        {
            try
            {
                Console.WriteLine("Iniciando subscribers...");
                
                var pedidoSubscriber = serviceProvider.GetRequiredService<PedidoSubscriber>();
                var pagamentoSubscriber = serviceProvider.GetRequiredService<PagamentoSubscriber>();
                var envioSubscriber = serviceProvider.GetRequiredService<EnvioSubscriber>();
                
                pedidoSubscriber.Subscribe();
                pagamentoSubscriber.Subscribe();
                envioSubscriber.Subscribe();
                
                Console.WriteLine("Todos os subscribers foram iniciados com sucesso.");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao iniciar subscribers: {ex.Message}");
                throw;
            }
        }
    }
}
