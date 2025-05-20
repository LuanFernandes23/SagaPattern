using System;
using System.Text.Json;
using System.Threading.Tasks;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    // Subscriber que processa mensagens relacionadas a Pedidos
    public class PedidoSubscriber : Subscriber
    {
        private readonly IPedidoService _pedidoService;
        private readonly Publisher _publisher;

        public PedidoSubscriber(
            RabbitMQConnection connection, 
            IPedidoService pedidoService,
            Publisher publisher,
            string exchangeName = "pedido_exchange",
            string queueName = "pedido_queue") 
            : base(connection, exchangeName, queueName)
        {
            _pedidoService = pedidoService;
            _publisher = publisher;
        }

        protected override async Task ProcessMessageAsync(SagaMessage message)
        {
            Console.WriteLine($"PedidoSubscriber recebeu mensagem do tipo: {message.Type}");

            switch (message.Type)
            {
                case "PedidoCancelado":
                    await ProcessarCancelamentoPedido(message);
                    break;
                
                default:
                    Console.WriteLine($"Tipo de mensagem não tratado: {message.Type}");
                    break;
            }
        }

        private async Task ProcessarCancelamentoPedido(SagaMessage message)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<PedidoCanceladoEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null)
                {
                    Console.WriteLine($"Cancelando pedido {evento.PedidoId}. Motivo: {evento.Motivo}");
                    
                    // Chama o serviço para cancelar o pedido
                    var resultado = await _pedidoService.CancelarPedidoAsync(evento.PedidoId, evento.Motivo);
                    
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
                Console.WriteLine($"Erro ao processar cancelamento de pedido: {ex.Message}");
                throw;
            }
        }
    }
}