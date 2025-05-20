using SagaPedidos.Domain.Entities;
using System.Threading.Tasks;

namespace SagaPedidos.Domain.Interfaces
{
    public interface IEnvioRepository
    {
        Task<Envio?> ObterPorIdAsync(int id);
        Task AdicionarAsync(Envio envio);
        Task AtualizarAsync(Envio envio);
    }
}