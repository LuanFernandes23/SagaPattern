using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    public class EnvioSubscriber : Subscriber
    {
        private readonly PedidoSagaOrchestrator _sagaOrchestrator;
        private readonly Random _random = new Random();

        public EnvioSubscriber(
            RabbitMQConnection connection,
            IServiceProvider serviceProvider,
            PedidoSagaOrchestrator sagaOrchestrator,
            string exchangeName = "saga-pedidos",
            string queueName = "envio_queue")
            : base(connection, serviceProvider, exchangeName, queueName)
        {
            _sagaOrchestrator = sagaOrchestrator ?? throw new ArgumentNullException(nameof(sagaOrchestrator));
            Console.WriteLine("EnvioSubscriber inicializado para exchange '" + exchangeName + "' e fila '" + queueName + "'");
        }

        protected override async Task ProcessMessageAsync(SagaMessage message, IServiceProvider serviceProvider)
        {
            Console.WriteLine("EnvioSubscriber recebeu mensagem do tipo: " + message.Type);

            switch (message.Type)
            {
                case "ProcessarEnvio":
                    await ProcessarEnvio(message, serviceProvider);
                    break;

                default:
                    Console.WriteLine("Tipo de mensagem não tratado pelo EnvioSubscriber: " + message.Type);
                    break;
            }
        }

        private async Task ProcessarEnvio(SagaMessage message, IServiceProvider serviceProvider)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<ProcessarEnvioEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null)
                {
                    Console.WriteLine("Processando envio para pedido " + evento.PedidoId);

                    // Agora usamos diretamente a string de endereço sem conversões complexas
                    var dto = new ProcessarEnvioDto
                    {
                        PedidoId = evento.PedidoId,
                        Endereco = evento.EnderecoEntrega
                    };

                    // Obtém o serviço do scope atual
                    var envioService = serviceProvider.GetRequiredService<IEnvioService>();

                    // Simulação: 90% de chance de sucesso no envio
                    if (_random.Next(100) < 90)
                    {
                        // Processa o envio
                        var envioId = await envioService.ProcessarEnvioAsync(dto);

                        Console.WriteLine("Envio do pedido " + evento.PedidoId + " processado com sucesso. Envio ID: " + envioId);

                        // Notifica o orquestrador que o envio foi processado
                        var processadoEvent = new EnvioProcessadoEvent(envioId, evento.PedidoId);
                        _sagaOrchestrator.FinalizarSaga(processadoEvent);
                    }
                    else
                    {
                        Console.WriteLine("Falha ao processar envio do pedido " + evento.PedidoId);

                        // Notifica o orquestrador sobre a falha
                        var falhadoEvent = new EnvioFalhadoEvent(evento.PedidoId, "Falha ao processar envio");
                        _sagaOrchestrator.TratarFalhaEnvio(falhadoEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar envio: " + ex.Message);

                // Em caso de erro, notifica o orquestrador para tratar a falha
                if (message.Headers.TryGetValue("PedidoId", out var pedidoIdStr) && int.TryParse(pedidoIdStr, out var pedidoId))
                {
                    var falhadoEvent = new EnvioFalhadoEvent(pedidoId, "Erro ao processar envio: " + ex.Message);
                    _sagaOrchestrator.TratarFalhaEnvio(falhadoEvent);
                }

                throw;
            }
        }
    }
}