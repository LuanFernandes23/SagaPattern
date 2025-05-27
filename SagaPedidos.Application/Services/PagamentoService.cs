using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Domain.Entities;
using SagaPedidos.Domain.Interfaces;

namespace SagaPedidos.Application.Services
{
    public class PagamentoService : IPagamentoService
    {
        private readonly IPagamentoRepository _pagamentoRepository;
        private readonly IPedidoRepository _pedidoRepository;

        public PagamentoService(IPagamentoRepository pagamentoRepository, IPedidoRepository pedidoRepository)
        {
            _pagamentoRepository = pagamentoRepository;
            _pedidoRepository = pedidoRepository;
        }

        public async Task<string> ProcessarPagamentoAsync(ProcessarPagamentoDto dto)
        {
            var pedido = await _pedidoRepository.ObterPorIdAsync(dto.PedidoId);
            if (pedido == null) throw new ArgumentException("Pedido não encontrado");

            var pagamento = new Pagamento(pedido, dto.Valor, dto.MetodoPagamento);
            await _pagamentoRepository.AdicionarAsync(pagamento);
            return pagamento.Id.ToString();
        }

        public async Task<bool> EstornarPagamentoAsync(int transacaoId, string motivo)
        {
            var pagamento = await _pagamentoRepository.ObterPorIdAsync(transacaoId);
            if (pagamento == null) return false;

            pagamento.Estornar(motivo);
            await _pagamentoRepository.AtualizarAsync(pagamento);
            return true;
        }
    }
}
