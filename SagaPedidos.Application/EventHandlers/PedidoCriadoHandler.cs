using System;
using System.Threading.Tasks;
using SagaPedidos.Application.Sagas;
using SagaPedidos.Domain.Events;

namespace SagaPedidos.Application.EventHandlers
{
    /// <summary>
    /// Handler que inicia o processo de saga quando um pedido é criado
    /// </summary>
    public class PedidoCriadoHandler
    {
        private readonly PedidoSagaOrchestrator _sagaOrchestrator;

        public PedidoCriadoHandler(PedidoSagaOrchestrator sagaOrchestrator)
        {
            _sagaOrchestrator = sagaOrchestrator;
        }

        public void Handle(PedidoCriadoEvent evento)
        {
            Console.WriteLine($"Pedido criado: {evento.PedidoId}. Iniciando saga...");
            
            // Inicia o fluxo da saga
            _sagaOrchestrator.IniciarSaga(evento);
        }
    }
}