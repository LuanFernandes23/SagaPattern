namespace SagaPedidos.Application.Dtos
{
    public class ProcessarPagamentoDto
    {
        public int PedidoId { get; set; }
        public decimal Valor { get; set; }
        public string MetodoPagamento { get; set; }
    }

    public class EstornarPagamentoDto
    {
        public string TransacaoId { get; set; }
        public int PedidoId { get; set; }
        public string Motivo { get; set; }
    }
}
