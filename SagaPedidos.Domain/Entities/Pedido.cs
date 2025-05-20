using System;
using System.Collections.Generic;

namespace SagaPedidos.Domain.Entities
{
    public class Pedido
    {
        public int Id { get; private set; }
        public int ClienteId { get; private set; }
        public DateTime DataCriacao { get; private set; }
        public StatusPedido Status { get; private set; }
        public decimal ValorTotal { get; private set; }
        public string EnderecoEntrega { get; private set; }
        public List<ItemPedido> Itens { get; private set; }
        public string MotivoFalha { get; private set; }

        // Construtor 
        public Pedido(Cliente clienteId, string enderecoEntrega)
        {
            ClienteId = clienteId.Id;
            EnderecoEntrega = enderecoEntrega;
            DataCriacao = DateTime.UtcNow;
            Status = StatusPedido.Criado;
            Itens = new List<ItemPedido>();
            ValorTotal = 0;
        }

        public void AdicionarItem(ItemPedido item)
        {
            Itens.Add(item);
            CalcularValorTotal();
        }

        public void CalcularValorTotal()
        {
            ValorTotal = 0;
            foreach (var item in Itens)
            {
                ValorTotal += item.Quantidade * item.PrecoUnitario;
            }
        }

        private void AtualizarStatus(StatusPedido novoStatus)
        {
            // Validações
            if (Status == StatusPedido.Cancelado)
                throw new InvalidOperationException("Não é possível alterar o status de um pedido cancelado");

            if (Status == StatusPedido.Entregue && novoStatus != StatusPedido.Entregue)
                throw new InvalidOperationException("Não é possível alterar o status de um pedido já entregue");

            Status = novoStatus;
        }

        public void Cancelar(string motivo)
        {
            MotivoFalha = motivo;
            AtualizarStatus(StatusPedido.Cancelado);
        }
    }

    public enum StatusPedido
    {
        Criado,
        Entregue,
        Cancelado
    }

}