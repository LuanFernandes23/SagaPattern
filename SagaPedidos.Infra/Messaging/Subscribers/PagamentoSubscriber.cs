using System;
using System.Text.Json;
using System.Threading.Tasks;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Infra.Messaging.Subscribers
{
    /// <summary>
    /// Subscriber que processa mensagens relacionadas a Pagamentos
    /// </summary>
    public class PagamentoSubscriber : Subscriber
    {
        private readonly IPagamentoService _pagamentoService;
        private readonly Publisher _publisher;
        private readonly PedidoSagaOrchestrator _sagaOrchestrator;
        private readonly Random _random = new Random();

        public PagamentoSubscriber(
            RabbitMQConnection connection, 
            IPagamentoService pagamentoService,
            Publisher publisher,
            PedidoSagaOrchestrator sagaOrchestrator,
            string exchangeName = "pedido_exchange",
            string queueName = "pagamento_queue") 
            : base(connection, exchangeName, queueName)
        {
            _pagamentoService = pagamentoService;
            _publisher = publisher;
            _sagaOrchestrator = sagaOrchestrator;
        }

        protected override async Task ProcessMessageAsync(SagaMessage message)
        {
            Console.WriteLine($"PagamentoSubscriber recebeu mensagem do tipo: {message.Type}");

            switch (message.Type)
            {
                case "ProcessarPagamento":
                    await ProcessarPagamento(message);
                    break;
                
                case "EstornarPagamento":
                    await EstornarPagamento(message);
                    break;
                
                default:
                    Console.WriteLine($"Tipo de mensagem não tratado: {message.Type}");
                    break;
            }
        }

        private async Task ProcessarPagamento(SagaMessage message)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<ProcessarPagamentoEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null)
                {
                    Console.WriteLine($"Processando pagamento para pedido {evento.PedidoId}. Valor: {evento.Valor}");
                    
                    var dto = new ProcessarPagamentoDto
                    {
                        PedidoId = evento.PedidoId,
                        Valor = evento.Valor,
                        MetodoPagamento = evento.MetodoPagamento
                    };

                    // Processa o pagamento
                    var transacaoId = await _pagamentoService.ProcessarPagamentoAsync(dto);
                    
                    // Simulação: 80% de chance de aprovação do pagamento
                    if (_random.Next(100) < 80)
                    {
                        Console.WriteLine($"Pagamento do pedido {evento.PedidoId} aprovado. Transação: {transacaoId}");
                        
                        // Pagamento aprovado
                        var aprovadoEvent = new PagamentoAprovadoEvent(evento.PedidoId);
                        _sagaOrchestrator.ContinuarSagaAposPagamento(aprovadoEvent);
                    }
                    else
                    {
                        Console.WriteLine($"Pagamento do pedido {evento.PedidoId} recusado");
                        
                        // Pagamento recusado
                        var recusadoEvent = new PagamentoRecusadoEvent(evento.PedidoId, "Pagamento recusado pela operadora do cartão");
                        _sagaOrchestrator.TratarFalhaPagamento(recusadoEvent);
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao processar pagamento: {ex.Message}");
                
                // Em caso de erro, notifica o orquestrador para tratar a falha
                if (message.Headers.TryGetValue("PedidoId", out var pedidoIdStr) && int.TryParse(pedidoIdStr, out var pedidoId))
                {
                    var recusadoEvent = new PagamentoRecusadoEvent(pedidoId, $"Erro ao processar pagamento: {ex.Message}");
                    _sagaOrchestrator.TratarFalhaPagamento(recusadoEvent);
                }
                
                throw;
            }
        }

        private async Task EstornarPagamento(SagaMessage message)
        {
            try
            {
                var evento = JsonSerializer.Deserialize<PagamentoEstornadoEvent>(
                    JsonSerializer.Serialize(message.Payload));

                if (evento != null && message.Headers.TryGetValue("PedidoId", out var pedidoIdStr))
                {
                    var motivo = message.Headers.TryGetValue("Motivo", out var motivoValue)
                        ? motivoValue
                        : "Estorno solicitado pela compensação da saga";

                    Console.WriteLine($"Estornando pagamento para pedido {evento.PedidoId}. Motivo: {motivo}");

                    // Na implementação real, buscaria a transacaoId de um repositório
                    int transacaoId = evento.PedidoId;

                    // Estorna o pagamento
                    var resultado = await _pagamentoService.EstornarPagamentoAsync(transacaoId, motivo);

                    if (resultado)
                    {
                        Console.WriteLine($"Pagamento do pedido {evento.PedidoId} estornado com sucesso");
                    }
                    else
                    {
                        Console.WriteLine($"Falha ao estornar pagamento do pedido {evento.PedidoId}");
                    }
                }
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao estornar pagamento: {ex.Message}");
                throw;
            }
        }
    }
}