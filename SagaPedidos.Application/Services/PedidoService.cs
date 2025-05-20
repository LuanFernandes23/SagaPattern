using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Domain.Entities;
using SagaPedidos.Domain.Interfaces;

namespace SagaPedidos.Application.Services
{
    public class PedidoService : IPedidoService
    {
        private readonly IPedidoRepository _pedidoRepository;
        public PedidoService(IPedidoRepository pedidoRepository)
        {
            _pedidoRepository = pedidoRepository;
        }

        public async Task<int> CriarPedidoAsync(CriarPedidoDto dto)
        {
            var cliente = new Cliente { Id = dto.ClienteId };
            var pedido = new Pedido(cliente, dto.EnderecoEntrega);

            foreach (var item in dto.Itens)
            {
                pedido.AdicionarItem(new ItemPedido(
                    produtoId: item.ProdutoId.ToString(), // Convertendo para string
                    nomeProduto: $"Produto {item.ProdutoId}",
                    quantidade: item.Quantidade,
                    precoUnitario: item.Preco
                ));
            }

            await _pedidoRepository.AdicionarAsync(pedido);
            return pedido.Id;
        }

        public async Task<bool> CancelarPedidoAsync(int pedidoId, string motivo)
        {
            var pedido = await _pedidoRepository.ObterPorIdAsync(pedidoId);
            if (pedido == null) return false;
            pedido.Cancelar(motivo);
            await _pedidoRepository.AtualizarAsync(pedido);
            return true;
        }

        public async Task<PedidoDto?> ConsultarPedidoAsync(int pedidoId)
        {
            var pedido = await _pedidoRepository.ObterPorIdAsync(pedidoId);
            if (pedido == null) return null;
            
            return new PedidoDto
            {
                PedidoId = pedido.Id,
                ClienteId = pedido.ClienteId,
                Itens = pedido.Itens.Select(i => new ItemPedidoDto 
                { 
                    ProdutoId = int.TryParse(i.ProdutoId, out var prodId) ? prodId : 0, 
                    Quantidade = i.Quantidade, 
                    Preco = i.PrecoUnitario 
                }).ToList(),
                ValorTotal = pedido.ValorTotal,
                Status = pedido.Status.ToString(),
                EnderecoEntrega = pedido.EnderecoEntrega,
                MotivoFalha = pedido.MotivoFalha,
                DataCriacao = pedido.DataCriacao
            };
        }
    }
}
