using System;
using System.Text.Json;
using System.Threading.Tasks;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.EventHandlers;
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
        private readonly PedidoCriadoHandler _pedidoCriadoHandler;

        public PedidoSubscriber(
            RabbitMQConnection connection,
            IPedidoService pedidoService,
            Publisher publisher,
            PedidoCriadoHandler pedidoCriadoHandler,
            string exchangeName = "saga-pedidos",
            string queueName = "pedido_queue")
            : base(connection, exchangeName, queueName)
        {
            _pedidoService = pedidoService ?? throw new ArgumentNullException(nameof(pedidoService));
            _publisher = publisher ?? throw new ArgumentNullException(nameof(publisher));
            _pedidoCriadoHandler = pedidoCriadoHandler ?? throw new ArgumentNullException(nameof(pedidoCriadoHandler));
            Console.WriteLine($"PedidoSubscriber inicializado para exchange '{exchangeName}' e fila '{queueName}'");
        }

        protected override async Task ProcessMessageAsync(SagaMessage message)
        {
            Console.WriteLine($"PedidoSubscriber recebeu mensagem do tipo: {message.Type}");

            switch (message.Type)
            {
                case "PedidoCriado":
                    await ProcessarPedidoCriado(message);
                    break;
                case "PedidoCancelado":
                    await ProcessarCancelamentoPedido(message);
                    break;
                default:
                    Console.WriteLine($"Tipo de mensagem não tratado pelo PedidoSubscriber: {message.Type}");
                    break;
            }
        }

        private Task ProcessarPedidoCriado(SagaMessage message)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<PedidoCriadoEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null)
                {
                    Console.WriteLine($"[PedidoSubscriber] Processando evento de pedido criado: {evento.PedidoId}");
                    _pedidoCriadoHandler.Handle(evento);
                    Console.WriteLine($"[PedidoSubscriber] Evento de pedido criado processado com sucesso: {evento.PedidoId}");
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PedidoSubscriber] Erro ao processar evento de pedido criado: {ex.Message}");
                Console.WriteLine($"[PedidoSubscriber] Stack trace: {ex.StackTrace}");
            }

            return Task.CompletedTask;
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
                Console.WriteLine($"Stack trace: {ex.StackTrace}");
                throw;
            }
        }
    }
}