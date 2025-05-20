using Microsoft.AspNetCore.Mvc;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Services;

namespace SagaPedidos.Presentation.Controllers
{
    [ApiController]
    [Route("api/pedidos")]
    public class PedidoController : ControllerBase
    {
        private readonly IPedidoService _pedidoService;
        public PedidoController(IPedidoService pedidoService)
        {
            _pedidoService = pedidoService;
        }

        [HttpPost]
        public async Task<IActionResult> CriarPedido([FromBody] CriarPedidoDto dto)
        {
            var pedidoId = await _pedidoService.CriarPedidoAsync(dto);
            return Ok(new { pedidoId, status = "criado", mensagem = "Pedido criado com sucesso" });
        }

        [HttpPut("{pedidoId}/cancelar")]
        public async Task<IActionResult> CancelarPedido(int pedidoId, [FromBody] CancelarPedidoDto dto)
        {
            var sucesso = await _pedidoService.CancelarPedidoAsync(pedidoId, dto.Motivo);
            if (!sucesso) return NotFound();
            return Ok(new { pedidoId, status = "cancelado", mensagem = "Pedido cancelado com sucesso" });
        }

        [HttpGet("{pedidoId}")]
        public async Task<IActionResult> ConsultarPedido(int pedidoId)
        {
            var pedido = await _pedidoService.ConsultarPedidoAsync(pedidoId);
            if (pedido == null) return NotFound();
            return Ok(pedido);
        }
    }
}
