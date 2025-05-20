using Microsoft.AspNetCore.Mvc;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using System.Threading.Tasks;

namespace SagaPedidos.Presentation.Controllers
{
    [ApiController]
    [Route("api/envios")]
    public class EnvioController : ControllerBase
    {
        private readonly IEnvioService _envioService;
        public EnvioController(IEnvioService envioService)
        {
            _envioService = envioService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessarEnvio([FromBody] ProcessarEnvioDto dto)
        {
            var envioId = await _envioService.ProcessarEnvioAsync(dto);
            return Ok(new { envioId, pedidoId = dto.PedidoId, codigoRastreio = "BR12345678", status = "processado", mensagem = "Envio processado com sucesso" });
        }

        [HttpPut("{envioId}/cancelar")]
        public async Task<IActionResult> CancelarEnvio(int envioId, [FromBody] CancelarEnvioDto dto)
        {
            var sucesso = await _envioService.CancelarEnvioAsync(envioId, dto.Motivo);
            if (!sucesso) return NotFound();
            return Ok(new { envioId, pedidoId = dto.PedidoId, status = "cancelado", mensagem = "Envio cancelado com sucesso" });
        }
    }
}
