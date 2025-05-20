namespace SagaPedidos.Application.Dtos
{
    public class CriarPedidoDto
    {
        public int ClienteId { get; set; }
        public List<ItemPedidoDto> Itens { get; set; }
        public decimal ValorTotal { get; set; }
        public string EnderecoEntrega { get; set; }
    }

    public class ItemPedidoDto
    {
        public int ProdutoId { get; set; }
        public int Quantidade { get; set; }
        public decimal Preco { get; set; }
    }

    public class PedidoDto
    {
        public int PedidoId { get; set; }
        public int ClienteId { get; set; }
        public List<ItemPedidoDto> Itens { get; set; }
        public decimal ValorTotal { get; set; }
        public string Status { get; set; }
        public string EnderecoEntrega { get; set; }
        public string? MotivoFalha { get; set; }
        public DateTime DataCriacao { get; set; }
    }

    public class CancelarPedidoDto
    {
        public string Motivo { get; set; }
    }
}
