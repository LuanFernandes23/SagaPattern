using SagaPedidos.Application.Dtos;

namespace SagaPedidos.Application.Interfaces
{
    public interface IPagamentoService
    {
        Task<string> ProcessarPagamentoAsync(ProcessarPagamentoDto dto);
        Task<bool> EstornarPagamentoAsync(int transacaoId, string motivo);
    }
}
