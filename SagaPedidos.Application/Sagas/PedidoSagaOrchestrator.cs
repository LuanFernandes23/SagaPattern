using System;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Messages;
using SagaPedidos.Infra.Messaging;

namespace SagaPedidos.Application.Sagas
{
    /// Orquestrador do fluxo da saga de pedido
    public class PedidoSagaOrchestrator
    {
        private readonly Publisher _publisher;

        public PedidoSagaOrchestrator(Publisher publisher)
        {
            _publisher = publisher;
        }

        /// Inicia o fluxo da saga quando um pedido é criado
        public void IniciarSaga(PedidoCriadoEvent pedidoCriado)
        {
            Console.WriteLine($"Iniciando saga para pedido {pedidoCriado.PedidoId}");
            
            // Cria uma mensagem para processar o pagamento do pedido
            var processarPagamentoEvent = new ProcessarPagamentoEvent(
                pedidoCriado.PedidoId, 
                pedidoCriado.ValorTotal, 
                "cartao"
            );
            
            var sagaMessage = new SagaMessage("ProcessarPagamento", processarPagamentoEvent);
            sagaMessage.AddHeader("PedidoId", pedidoCriado.PedidoId.ToString());
            
            // Publica a mensagem para o serviço de pagamento
            _publisher.Publish(sagaMessage);
        }
        
        /// Continua o fluxo da saga quando um pagamento é aprovado
        public void ContinuarSagaAposPagamento(PagamentoAprovadoEvent pagamentoAprovado)
        {
            Console.WriteLine($"Pagamento aprovado para pedido {pagamentoAprovado.PedidoId}. Iniciando envio...");
            
            // O pedido foi pago, agora processa o envio
            var processarEnvioEvent = new ProcessarEnvioEvent(
                pagamentoAprovado.PedidoId,
                "Buscar endereço do pedido" // Na implementação real, buscaria do repositório
            );
            
            var sagaMessage = new SagaMessage("ProcessarEnvio", processarEnvioEvent);
            sagaMessage.AddHeader("PedidoId", pagamentoAprovado.PedidoId.ToString());
            
            // Publica a mensagem para o serviço de envio
            _publisher.Publish(sagaMessage);
        }
        
        /// Trata o caso de falha de pagamento
        public void TratarFalhaPagamento(PagamentoRecusadoEvent pagamentoRecusado)
        {
            Console.WriteLine($"Pagamento recusado para pedido {pagamentoRecusado.PedidoId}. Cancelando pedido...");
            
            // O pagamento foi recusado, cancela o pedido
            var pedidoCanceladoEvent = new PedidoCanceladoEvent(
                pagamentoRecusado.PedidoId, 
                $"Pagamento recusado: {pagamentoRecusado.Motivo}"
            );
            
            var sagaMessage = new SagaMessage("PedidoCancelado", pedidoCanceladoEvent);
            sagaMessage.AddHeader("PedidoId", pagamentoRecusado.PedidoId.ToString());
            
            // Publica a mensagem para o serviço de pedido
            _publisher.Publish(sagaMessage);
        }
        
        /// Finaliza a saga quando o envio é processado
        public void FinalizarSaga(EnvioProcessadoEvent envioProcessado)
        {
            Console.WriteLine($"Envio processado para pedido {envioProcessado.PedidoId}. Saga concluída com sucesso!");
            
            // A saga foi concluída com sucesso
            // Aqui poderia notificar outras partes do sistema ou gerar relatórios
        }
        
        /// Trata o caso de falha no envio
        public void TratarFalhaEnvio(EnvioFalhadoEvent envioFalhado)
        {
            Console.WriteLine($"Falha no envio para pedido {envioFalhado.PedidoId}. Iniciando compensação...");
            
            // O envio falhou, precisa estornar o pagamento
            var estornarPagamentoEvent = new PagamentoEstornadoEvent(envioFalhado.PedidoId);
            
            var sagaMessage = new SagaMessage("EstornarPagamento", estornarPagamentoEvent);
            sagaMessage.AddHeader("PedidoId", envioFalhado.PedidoId.ToString());
            sagaMessage.AddHeader("Motivo", envioFalhado.Motivo);
            
            // Publica a mensagem para o serviço de pagamento
            _publisher.Publish(sagaMessage);
            
            // Também cancela o pedido
            var pedidoCanceladoEvent = new PedidoCanceladoEvent(
                envioFalhado.PedidoId,
                $"Falha no envio: {envioFalhado.Motivo}"
            );
            
            var cancelarPedidoMessage = new SagaMessage("PedidoCancelado", pedidoCanceladoEvent);
            cancelarPedidoMessage.AddHeader("PedidoId", envioFalhado.PedidoId.ToString());
            
            // Publica a mensagem para o serviço de pedido
            _publisher.Publish(cancelarPedidoMessage);
        }
    }
}