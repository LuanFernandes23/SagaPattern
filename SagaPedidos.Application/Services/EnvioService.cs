using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Domain.Entities;
using SagaPedidos.Domain.Interfaces;
using System;
using System.Threading.Tasks;

namespace SagaPedidos.Application.Services
{
    public class EnvioService : IEnvioService
    {
        private readonly IEnvioRepository _envioRepository;
        private readonly IPedidoRepository _pedidoRepository;

        public EnvioService(IEnvioRepository envioRepository, IPedidoRepository pedidoRepository)
        {
            _envioRepository = envioRepository;
            _pedidoRepository = pedidoRepository;
        }

        public async Task<int> ProcessarEnvioAsync(ProcessarEnvioDto dto)
        {
            var pedido = await _pedidoRepository.ObterPorIdAsync(dto.PedidoId);
            if (pedido == null) throw new ArgumentException("Pedido não encontrado");

            // Usar a string de endereço diretamente
            var envio = new Envio(pedido, dto.Endereco);
            await _envioRepository.AdicionarAsync(envio);
            return envio.Id;
        }

        public async Task<bool> CancelarEnvioAsync(int envioId, string motivo)
        {
            var envio = await _envioRepository.ObterPorIdAsync(envioId);
            if (envio == null) return false;
            envio.Cancelar(motivo);
            await _envioRepository.AtualizarAsync(envio);
            return true;
        }
    }
}
