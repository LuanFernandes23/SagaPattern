using Microsoft.AspNetCore.Builder;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.OpenApi.Models;
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
                // Criação do builder para o WebApplicationBuilder
                var builder = WebApplication.CreateBuilder(args);
                
                // Adiciona serviços ao container
                ConfigureServices(builder.Services, builder.Configuration);
                
                // Configuração de Swagger
                builder.Services.AddEndpointsApiExplorer();
                builder.Services.AddSwaggerGen(c =>
                {
                    c.SwaggerDoc("v1", new OpenApiInfo { 
                        Title = "SagaPedidos API", 
                        Version = "v1",
                        Description = "API para gerenciamento de pedidos utilizando o padrão Saga"
                    });
                });
                
                // Adiciona controllers
                builder.Services.AddControllers();
                
                // Adiciona configuração CORS
                builder.Services.AddCors(options =>
                {
                    options.AddDefaultPolicy(
                        policy =>
                        {
                            policy.AllowAnyOrigin()
                                  .AllowAnyHeader()
                                  .AllowAnyMethod();
                        });
                });
                
                // Constrói o app
                var app = builder.Build();
                
                // Teste rápido de conexão antes de iniciar toda a aplicação
                var connectionString = builder.Configuration["RabbitMQ:ConnectionString"];
                Console.WriteLine($"Testando conexão com RabbitMQ: {connectionString}");
                
                // Configure o pipeline de requisição HTTP
                if (app.Environment.IsDevelopment())
                {
                    app.UseDeveloperExceptionPage();
                    app.UseSwagger();
                    app.UseSwaggerUI(c => 
                    {
                        c.SwaggerEndpoint("/swagger/v1/swagger.json", "SagaPedidos API V1");
                        c.RoutePrefix = "swagger";
                    });
                }
                
                // Usar CORS antes de outras middlewares de endpoint
                app.UseCors();
                
                app.UseHttpsRedirection();
                app.UseRouting();
                app.UseAuthorization();
                app.MapControllers();
                
                // Inicializa os subscribers para o RabbitMQ depois que o app está construído
                using (var scope = app.Services.CreateScope())
                {
                    var serviceProvider = scope.ServiceProvider;
                    InitializeSubscribers(serviceProvider);
                    
                    // Verificar se o banco de dados existe, caso contrário criar
                    try
                    {
                        var context = serviceProvider.GetRequiredService<AppDbContext>();
                        context.Database.EnsureCreated();
                        Console.WriteLine("Banco de dados verificado/criado com sucesso!");
                    }
                    catch (Exception ex)
                    {
                        Console.WriteLine($"Erro ao verificar/criar banco de dados: {ex.Message}");
                    }
                }
                
                Console.WriteLine("Aplicação iniciada com sucesso! A API está rodando...");
                Console.WriteLine($"Swagger disponível em: https://localhost:5004/swagger");
                Console.WriteLine($"Swagger também disponível em: http://localhost:5003/swagger");
                
                // Inicia o app
                await app.RunAsync();
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

        private static void ConfigureServices(IServiceCollection services, IConfiguration configuration)
        {
            // Injetar a configuração diretamente
            services.AddSingleton<IConfiguration>(configuration);

            // DbContext
            services.AddDbContext<AppDbContext>(options =>
                options.UseSqlServer(configuration.GetConnectionString("DefaultConnection")));

            // RabbitMQ
            var rabbitConnStr = configuration["RabbitMQ:ConnectionString"];
            var rabbitExchange = configuration["RabbitMQ:ExchangeName"];
            Console.WriteLine($"String de conexão RabbitMQ: {rabbitConnStr}");
            Console.WriteLine($"Exchange RabbitMQ: {rabbitExchange}");

            services.AddSingleton<RabbitMQConnection>(sp =>
                new RabbitMQConnection(rabbitConnStr ?? "amqp://localhost"));

            // Publisher (injeção do nome da exchange)
            services.AddSingleton<Publisher>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                return new Publisher(conn, rabbitExchange ?? "saga-pedidos");
            });
            services.AddSingleton<IPublisher>(sp =>
                sp.GetRequiredService<Publisher>());

            // Orchestrator da Saga e Handlers - com factory como parâmetro
            services.AddSingleton<PedidoSagaOrchestrator>(sp =>
            {
                var publisher = sp.GetRequiredService<IPublisher>();
                
                // Criamos um factory explícito para obter o repositório
                Func<IPedidoRepository> pedidoRepositoryFactory = () =>
                {
                    // Criamos um scope temporário para obter o serviço scoped
                    var scope = sp.CreateScope();
                    return scope.ServiceProvider.GetRequiredService<IPedidoRepository>();
                };
                
                return new PedidoSagaOrchestrator(publisher, pedidoRepositoryFactory);
            });
            
            services.AddSingleton<PedidoCriadoHandler>();

            // Repositórios - Agora com injeção de Publisher e Handler
            services.AddScoped<IPedidoRepository>(sp => 
            {
                var context = sp.GetRequiredService<AppDbContext>();
                var publisher = sp.GetRequiredService<IPublisher>();
                var handler = sp.GetRequiredService<PedidoCriadoHandler>();
                return new PedidoRepository(context, publisher, handler);
            });
            
            services.AddScoped<IPagamentoRepository, PagamentoRepository>();
            services.AddScoped<IEnvioRepository, EnvioRepository>();

            // Serviços de domínio
            services.AddScoped<IPedidoService, PedidoService>();
            services.AddScoped<IPagamentoService, PagamentoService>();
            services.AddScoped<IEnvioService, EnvioService>();

            // Subscribers - adaptados para trabalhar com serviços scoped
            services.AddSingleton<PedidoSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var publisher = sp.GetRequiredService<Publisher>();
                
                // Criamos um scope temporário para inicializar o subscriber
                using var scope = sp.CreateScope();
                var pedidoSrv = scope.ServiceProvider.GetRequiredService<IPedidoService>();
                
                return new PedidoSubscriber(conn, pedidoSrv, publisher, 
                    rabbitExchange ?? "saga-pedidos", "pedido_queue");
            });

            services.AddSingleton<PagamentoSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var publisher = sp.GetRequiredService<Publisher>();
                var orchestrator = sp.GetRequiredService<PedidoSagaOrchestrator>();
                
                // Criamos um scope temporário para inicializar o subscriber
                using var scope = sp.CreateScope();
                var pagamentoSrv = scope.ServiceProvider.GetRequiredService<IPagamentoService>();
                
                return new PagamentoSubscriber(conn, pagamentoSrv, publisher, orchestrator,
                    rabbitExchange ?? "saga-pedidos", "pagamento_queue");
            });

            services.AddSingleton<EnvioSubscriber>(sp =>
            {
                var conn = sp.GetRequiredService<RabbitMQConnection>();
                var orchestrator = sp.GetRequiredService<PedidoSagaOrchestrator>();
                
                // Criamos um scope temporário para inicializar o subscriber
                using var scope = sp.CreateScope();
                var envioSrv = scope.ServiceProvider.GetRequiredService<IEnvioService>();
                
                return new EnvioSubscriber(conn, envioSrv, orchestrator,
                    rabbitExchange ?? "saga-pedidos", "envio_queue");
            });
        }

        private static void InitializeSubscribers(IServiceProvider serviceProvider)
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