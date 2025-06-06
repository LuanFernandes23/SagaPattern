﻿using System;

namespace SagaPedidos.Domain.Entities
{
    public class Pagamento
    {
        public int Id { get; private set; }
        public int PedidoId { get; private set; }
        public decimal Valor { get; private set; }
        public string FormaPagamento { get; private set; }
        public StatusPagamento Status { get; private set; }
        public DateTime DataProcessamento { get; private set; }
        public string MotivoFalha { get; private set; }

        // Construtor para EF Core
        protected Pagamento() { }

        // Construtor
        public Pagamento(Pedido pedido, decimal valor, string formaPagamento)
        {
            PedidoId = pedido.Id;
            Valor = valor;
            FormaPagamento = formaPagamento;
            Status = StatusPagamento.Pendente;
            DataProcessamento = DateTime.UtcNow;
        }

        public void Estornar(string motivo = null)
        {
            if (Status != StatusPagamento.Aprovado)
                throw new InvalidOperationException("Apenas pagamentos aprovados podem ser estornados");

            if (!string.IsNullOrEmpty(motivo))
                MotivoFalha = motivo;
                
            Status = StatusPagamento.Estornado;
        }
    }

    public enum StatusPagamento
    {
        Pendente,
        Aprovado,
        Estornado
    }
}