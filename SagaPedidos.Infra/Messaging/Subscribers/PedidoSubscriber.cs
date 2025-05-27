using System;
using System.Text.Json;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    // Subscriber que processa mensagens relacionadas a Pedidos
    public class PedidoSubscriber : Subscriber
    {
        private readonly Publisher _publisher;

        public PedidoSubscriber(
            RabbitMQConnection connection,
            IServiceProvider serviceProvider,
            Publisher publisher,
            string exchangeName = "saga-pedidos",
            string queueName = "pedido_queue")
            : base(connection, serviceProvider, exchangeName, queueName)
        {
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            Console.WriteLine($"PedidoSubscriber inicializado para exchange '{exchangeName}' e fila '{queueName}'");
        }

        protected override async Task ProcessMessageAsync(SagaMessage message, IServiceProvider serviceProvider)
        {
            Console.WriteLine($"PedidoSubscriber recebeu mensagem do tipo: {message.Type}");

            switch (message.Type)
            {
                case "PedidoCancelado":
                    await ProcessarCancelamentoPedido(message, serviceProvider);
                    break;

                default:
                    Console.WriteLine($"Tipo de mensagem não tratado pelo PedidoSubscriber: {message.Type}");
                    break;
            }
        }

        private async Task ProcessarCancelamentoPedido(SagaMessage message, IServiceProvider serviceProvider)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<PedidoCanceladoEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null)
                {
                    Console.WriteLine($"Cancelando pedido {evento.PedidoId}. Motivo: {evento.Motivo}");

                    // Obtém o serviço do scope atual
                    var pedidoService = serviceProvider.GetRequiredService<IPedidoService>();

                    // Chama o serviço para cancelar o pedido
                    var resultado = await pedidoService.CancelarPedidoAsync(evento.PedidoId, evento.Motivo);

                    if (resultado)
                    {
                        Console.WriteLine($"Pedido {evento.PedidoId} cancelado com sucesso");
                    }
                    else
                    {
                        Console.WriteLine($"Falha ao cancelar pedido {evento.PedidoId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine("Erro ao processar cancelamento de pedido: " + ex.Message);
                // Removida a linha de log do stack trace que estava causando problemas
                throw;
            }
        }
    }
}