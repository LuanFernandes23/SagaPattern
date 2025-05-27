using System.Collections.Generic;

namespace SagaPedidos.Application.Dtos
{
    public class ProcessarEnvioDto
    {
        public int PedidoId { get; set; }
        public string Endereco { get; set; } // String format address
    }

    public class CancelarEnvioDto
    {
        public int PedidoId { get; set; }
        public string Motivo { get; set; }
    }
}