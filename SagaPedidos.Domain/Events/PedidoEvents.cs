using SagaPedidos.Domain.Entities;
using System;
using System.Collections.Generic;

namespace SagaPedidos.Domain.Events
{
    // Evento disparado quando um novo pedido é criado
    public class PedidoCriadoEvent : Event
    {
        public int PedidoId { get; private set; }
        public int ClienteId { get; private set; }
        public decimal ValorTotal { get; private set; }
        public string EnderecoEntrega { get; private set; }
        public string MetodoPagamento { get; private set; }

        public PedidoCriadoEvent(int pedidoId, int clienteId, decimal valorTotal, string enderecoEntrega, string metodoPagamento = "cartao")
        {
            PedidoId = pedidoId;
            ClienteId = clienteId;
            ValorTotal = valorTotal;
            EnderecoEntrega = enderecoEntrega;
            MetodoPagamento = metodoPagamento;
        }
    }

    // Evento disparado quando um pedido é cancelado
    public class PedidoCanceladoEvent : Event
    {
        public int PedidoId { get; private set; }
        public string Motivo { get; private set; }

        public PedidoCanceladoEvent(int pedidoId, string motivo)
        {
            PedidoId = pedidoId;
            Motivo = motivo;
        }
    }
}