using System;
using System.Threading.Tasks;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Interfaces;
using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Application.Sagas
{
    // Orquestrador do fluxo da saga de pedido
    public class PedidoSagaOrchestrator
    {
        private readonly IPublisher _publisher;
        private readonly Func<IPedidoRepository> _pedidoRepositoryFactory;

        public PedidoSagaOrchestrator(IPublisher publisher, Func<IPedidoRepository> pedidoRepositoryFactory)
        {
            _publisher = publisher;
            _pedidoRepositoryFactory = pedidoRepositoryFactory;
        }

        // Inicia o fluxo da saga quando um pedido é criado
        public void IniciarSaga(PedidoCriadoEvent pedidoCriado)
        {
            Console.WriteLine($"=== SAGA: Iniciando saga para pedido {pedidoCriado.PedidoId} ===");
            
            try
            {
                // Cria uma mensagem para processar o pagamento do pedido
                // Usa um método de pagamento padrão que pode ser alterado ou configurado externamente
                var metodoPagamento = pedidoCriado.MetodoPagamento ?? "cartao"; // Valor padrão em caso de null
                
                var processarPagamentoEvent = new ProcessarPagamentoEvent(
                    pedidoCriado.PedidoId, 
                    pedidoCriado.ValorTotal,
                    metodoPagamento
                );
                
                var sagaMessage = new SagaMessage("ProcessarPagamento", processarPagamentoEvent);
                sagaMessage.AddHeader("PedidoId", pedidoCriado.PedidoId.ToString());
                
                // Publica a mensagem para o serviço de pagamento
                Console.WriteLine($"SAGA: Publicando evento ProcessarPagamento para o pedido {pedidoCriado.PedidoId}");
                _publisher.Publish(sagaMessage);
                Console.WriteLine($"SAGA: Evento ProcessarPagamento publicado para o pedido {pedidoCriado.PedidoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO SAGA: Falha ao iniciar saga para pedido {pedidoCriado.PedidoId}: {ex.Message}");
                Console.WriteLine($"ERRO SAGA: StackTrace: {ex.StackTrace}");
            }
        }
        
        // Continua o fluxo da saga quando um pagamento é aprovado
        public async void ContinuarSagaAposPagamento(PagamentoAprovadoEvent pagamentoAprovado)
        {
            Console.WriteLine($"=== SAGA: Pagamento aprovado para pedido {pagamentoAprovado.PedidoId}. Iniciando envio... ===");
            
            try
            {
                // Busca o pedido do repositório para obter o endereço de entrega
                var pedidoRepository = _pedidoRepositoryFactory();
                var pedido = await pedidoRepository.ObterPorIdAsync(pagamentoAprovado.PedidoId);
                if (pedido == null)
                {
                    Console.WriteLine($"ERRO SAGA: Pedido {pagamentoAprovado.PedidoId} não encontrado para processamento de envio");
                    return;
                }
                
                Console.WriteLine($"SAGA: Pedido {pagamentoAprovado.PedidoId} encontrado com endereço: {pedido.EnderecoEntrega}");
                
                // O pedido foi pago, agora processa o envio
                var processarEnvioEvent = new ProcessarEnvioEvent(
                    pagamentoAprovado.PedidoId,
                    pedido.EnderecoEntrega // Usa o endereço real do pedido
                );
                
                var sagaMessage = new SagaMessage("ProcessarEnvio", processarEnvioEvent);
                sagaMessage.AddHeader("PedidoId", pagamentoAprovado.PedidoId.ToString());
                
                // Publica a mensagem para o serviço de envio
                Console.WriteLine($"SAGA: Publicando evento ProcessarEnvio para o pedido {pagamentoAprovado.PedidoId}");
                _publisher.Publish(sagaMessage);
                Console.WriteLine($"SAGA: Evento ProcessarEnvio publicado para o pedido {pagamentoAprovado.PedidoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO SAGA: Falha ao continuar saga após pagamento para pedido {pagamentoAprovado.PedidoId}: {ex.Message}");
                Console.WriteLine($"ERRO SAGA: StackTrace: {ex.StackTrace}");
            }
        }
        
        // Trata o caso de falha de pagamento
        public void TratarFalhaPagamento(PagamentoRecusadoEvent pagamentoRecusado)
        {
            Console.WriteLine($"=== SAGA: Pagamento recusado para pedido {pagamentoRecusado.PedidoId}. Cancelando pedido... ===");
            
            try
            {
                // O pagamento foi recusado, cancela o pedido
                var pedidoCanceladoEvent = new PedidoCanceladoEvent(
                    pagamentoRecusado.PedidoId, 
                    $"Pagamento recusado: {pagamentoRecusado.Motivo}"
                );
                
                var sagaMessage = new SagaMessage("PedidoCancelado", pedidoCanceladoEvent);
                sagaMessage.AddHeader("PedidoId", pagamentoRecusado.PedidoId.ToString());
                
                // Publica a mensagem para o serviço de pedido
                Console.WriteLine($"SAGA: Publicando evento PedidoCancelado para o pedido {pagamentoRecusado.PedidoId}");
                _publisher.Publish(sagaMessage);
                Console.WriteLine($"SAGA: Evento PedidoCancelado publicado para o pedido {pagamentoRecusado.PedidoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO SAGA: Falha ao tratar erro de pagamento para pedido {pagamentoRecusado.PedidoId}: {ex.Message}");
                Console.WriteLine($"ERRO SAGA: StackTrace: {ex.StackTrace}");
            }
        }
        
        // Finaliza a saga quando o envio é processado
        public void FinalizarSaga(EnvioProcessadoEvent envioProcessado)
        {
            Console.WriteLine($"=== SAGA: Envio processado para pedido {envioProcessado.PedidoId}. Saga concluída com sucesso! ===");
            
            // A saga foi concluída com sucesso
        }
        
        // Trata o caso de falha no envio
        public void TratarFalhaEnvio(EnvioFalhadoEvent envioFalhado)
        {
            Console.WriteLine($"=== SAGA: Falha no envio para pedido {envioFalhado.PedidoId}. Iniciando compensação... ===");
            
            try
            {
                // O envio falhou, precisa estornar o pagamento
                var estornarPagamentoEvent = new PagamentoEstornadoEvent(envioFalhado.PedidoId);
                
                var sagaMessage = new SagaMessage("EstornarPagamento", estornarPagamentoEvent);
                sagaMessage.AddHeader("PedidoId", envioFalhado.PedidoId.ToString());
                sagaMessage.AddHeader("Motivo", envioFalhado.Motivo);
                
                // Publica a mensagem para o serviço de pagamento
                Console.WriteLine($"SAGA: Publicando evento EstornarPagamento para o pedido {envioFalhado.PedidoId}");
                _publisher.Publish(sagaMessage);
                Console.WriteLine($"SAGA: Evento EstornarPagamento publicado para o pedido {envioFalhado.PedidoId}");
                
                // Também cancela o pedido
                var pedidoCanceladoEvent = new PedidoCanceladoEvent(
                    envioFalhado.PedidoId,
                    $"Falha no envio: {envioFalhado.Motivo}"
                );
                
                var cancelarPedidoMessage = new SagaMessage("PedidoCancelado", pedidoCanceladoEvent);
                cancelarPedidoMessage.AddHeader("PedidoId", envioFalhado.PedidoId.ToString());
                
                // Publica a mensagem para o serviço de pedido
                Console.WriteLine($"SAGA: Publicando evento PedidoCancelado para o pedido {envioFalhado.PedidoId}");
                _publisher.Publish(cancelarPedidoMessage);
                Console.WriteLine($"SAGA: Evento PedidoCancelado publicado para o pedido {envioFalhado.PedidoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"ERRO SAGA: Falha ao tratar erro de envio para pedido {envioFalhado.PedidoId}: {ex.Message}");
                Console.WriteLine($"ERRO SAGA: StackTrace: {ex.StackTrace}");
            }
        }
    }
}