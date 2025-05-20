using SagaPedidos.Domain.Entities;
using System.Threading.Tasks;

namespace SagaPedidos.Domain.Interfaces
{
    public interface IPedidoRepository
    {
        Task<Pedido?> ObterPorIdAsync(int id);
        Task AdicionarAsync(Pedido pedido);
        Task AtualizarAsync(Pedido pedido);
    }
}