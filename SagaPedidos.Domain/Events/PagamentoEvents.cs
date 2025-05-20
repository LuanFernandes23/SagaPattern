using SagaPedidos.Domain.Entities;
using System;

namespace SagaPedidos.Domain.Events
{
    // Evento disparado para iniciar o processamento de pagamento
    public class ProcessarPagamentoEvent : Event
    {
        public int PedidoId { get; private set; }
        public decimal Valor { get; private set; }
        public string MetodoPagamento { get; private set; }

        public ProcessarPagamentoEvent(int pedidoId, decimal valor, string metodoPagamento)
        {
            PedidoId = pedidoId;
            Valor = valor;
            MetodoPagamento = metodoPagamento;
        }
    }

    // Evento disparado quando um pagamento é aprovado
    public class PagamentoAprovadoEvent : Event
    {
        public int PedidoId { get; private set; }

        public PagamentoAprovadoEvent(int pedidoId)
        {
            PedidoId = pedidoId;
        }
    }

    // Evento disparado quando um pagamento é recusado
    public class PagamentoRecusadoEvent : Event
    {
        public int PedidoId { get; private set; }
        public string Motivo { get; private set; }

        public PagamentoRecusadoEvent(int pedidoId, string motivo)
        {
            PedidoId = pedidoId;
            Motivo = motivo;
        }
    }

    // Evento disparado quando um pagamento é estornado
    public class PagamentoEstornadoEvent : Event
    {
        public int PedidoId { get; private set; }

        public PagamentoEstornadoEvent(int pedidoId)
        {
            PedidoId = pedidoId;
        }
    }
}