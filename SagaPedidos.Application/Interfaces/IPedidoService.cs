using SagaPedidos.Application.Dtos;

namespace SagaPedidos.Application.Interfaces
{
    public interface IPedidoService
    {
        Task<int> CriarPedidoAsync(CriarPedidoDto dto);
        Task<bool> CancelarPedidoAsync(int pedidoId, string motivo);
        Task<PedidoDto?> ConsultarPedidoAsync(int pedidoId);
    }
}
