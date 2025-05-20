using SagaPedidos.Application.Dtos;

namespace SagaPedidos.Application.Interfaces
{
    public interface IEnvioService
    {
        Task<int> ProcessarEnvioAsync(ProcessarEnvioDto dto);
        Task<bool> CancelarEnvioAsync(int envioId, string motivo);
    }
}
