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
        public Endereco Endereco { get; private set; }
        public string EnderecoEntrega { get; private set; } // Mantido para compatibilidade
        public List<ItemPedido> Itens { get; private set; }
        public string MotivoFalha { get; private set; }

        // Construtor para EF Core
        protected Pedido() { }

        // Construtor recebendo string de endereço
        public Pedido(Cliente cliente, string enderecoEntrega)
        {
            ClienteId = cliente.Id;
            EnderecoEntrega = enderecoEntrega;
            Endereco = Endereco.FromString(enderecoEntrega);
            DataCriacao = DateTime.UtcNow;
            Status = StatusPedido.Criado;
            Itens = new List<ItemPedido>();
            ValorTotal = 0;
        }

        // Construtor recebendo objeto Endereco
        public Pedido(Cliente cliente, Endereco endereco)
        {
            ClienteId = cliente.Id;
            Endereco = endereco;
            EnderecoEntrega = endereco.ToString();
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