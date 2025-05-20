using SagaPedidos.Domain.Entities;
using System.Threading.Tasks;

namespace SagaPedidos.Domain.Interfaces
{
    public interface IPagamentoRepository
    {
        Task<Pagamento?> ObterPorIdAsync(int id);
        Task AdicionarAsync(Pagamento pagamento);
        Task AtualizarAsync(Pagamento pagamento);
    }
}