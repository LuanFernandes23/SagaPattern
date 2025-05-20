using System.Collections.Generic;

namespace SagaPedidos.Application.Dtos
{
    public class ProcessarEnvioDto
    {
        public int PedidoId { get; set; }
        public EnderecoDto Endereco { get; set; }
    }

    public class EnderecoDto
    {
        public string Rua { get; set; }
        public string Numero { get; set; }
        public string Cidade { get; set; }
    }

    public class CancelarEnvioDto
    {
        public int PedidoId { get; set; }
        public string Motivo { get; set; }
    }
}