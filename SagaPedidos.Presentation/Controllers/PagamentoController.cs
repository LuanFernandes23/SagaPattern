using Microsoft.AspNetCore.Mvc;
using SagaPedidos.Application.Dtos;
using SagaPedidos.Application.Interfaces;
using SagaPedidos.Application.Services;

namespace SagaPedidos.Presentation.Controllers
{
    [ApiController]
    [Route("api/pagamentos")]
    public class PagamentoController : ControllerBase
    {
        private readonly IPagamentoService _pagamentoService;
        public PagamentoController(IPagamentoService pagamentoService)
        {
            _pagamentoService = pagamentoService;
        }

        [HttpPost]
        public async Task<IActionResult> ProcessarPagamento([FromBody] ProcessarPagamentoDto dto)
        {
            var transacaoId = await _pagamentoService.ProcessarPagamentoAsync(dto);
            return Ok(new { transacaoId, pedidoId = dto.PedidoId, status = "aprovado", mensagem = "Pagamento processado com sucesso" });
        }

        [HttpPost("{transacaoId}/estornar")]
        public async Task<IActionResult> EstornarPagamento(int transacaoId, [FromBody] EstornarPagamentoDto dto)
        {
            var sucesso = await _pagamentoService.EstornarPagamentoAsync(transacaoId, dto.Motivo);
            if (!sucesso) return NotFound();
            return Ok(new { transacaoId, pedidoId = dto.PedidoId, status = "estornado", mensagem = "Pagamento estornado com sucesso" });
        }
    }
}
