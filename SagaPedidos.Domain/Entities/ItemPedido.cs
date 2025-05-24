using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace SagaPedidos.Domain.Entities
{
    public class ItemPedido
    {
        public int Id { get; private set; }
        public int PedidoId { get; private set; } // Chave estrangeira adicionada
        public string ProdutoId { get; private set; }
        public string NomeProduto { get; private set; }
        public int Quantidade { get; private set; }
        public decimal PrecoUnitario { get; private set; }

        // Construtor para EF Core
        protected ItemPedido() { }

        public ItemPedido(string produtoId, string nomeProduto, int quantidade, decimal precoUnitario)
        {
            ProdutoId = produtoId;
            NomeProduto = nomeProduto;
            Quantidade = quantidade;
            PrecoUnitario = precoUnitario;
        }
    }
}