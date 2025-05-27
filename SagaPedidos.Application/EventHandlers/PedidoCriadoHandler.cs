using System;
using System.Threading.Tasks;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Domain.Events;

namespace SagaPedidos.Application.EventHandlers
{
    // Handler que inicia o processo de saga quando um pedido é criado
    public class PedidoCriadoHandler
    {
        private readonly PedidoSagaOrchestrator _sagaOrchestrator;

        public PedidoCriadoHandler(PedidoSagaOrchestrator sagaOrchestrator)
        {
            _sagaOrchestrator = sagaOrchestrator;
        }

        public void Handle(PedidoCriadoEvent evento)
        {
            try
            {
                Console.WriteLine($"[PedidoCriadoHandler] Evento recebido para o pedido {evento.PedidoId}");
                Console.WriteLine($"[PedidoCriadoHandler] Valor total: {evento.ValorTotal}, Endereço: {evento.EnderecoEntrega}");
                
                // Inicia o fluxo da saga
                Console.WriteLine($"[PedidoCriadoHandler] Iniciando saga para o pedido {evento.PedidoId}...");
                _sagaOrchestrator.IniciarSaga(evento);
                Console.WriteLine($"[PedidoCriadoHandler] Saga iniciada para o pedido {evento.PedidoId}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"[PedidoCriadoHandler] ERRO ao processar evento pedido criado: {ex.Message}");
                Console.WriteLine($"[PedidoCriadoHandler] Stack trace: {ex.StackTrace}");
            }
        }
    }
}