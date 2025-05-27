using System;

namespace SagaPedidos.Domain.Entities
{
    public class Envio
    {
        public int Id { get; private set; }
        public int PedidoId { get; private set; }
        public string EnderecoEntrega { get; private set; } 
        public StatusEnvio Status { get; private set; }
        public DateTime DataCriacao { get; private set; }
        public DateTime? DataEnvio { get; private set; }
        public DateTime? DataEntrega { get; private set; }
        public string MotivoFalha { get; private set; }

        // Construtor para EF Core
        protected Envio() { }

        // Construtor recebendo string de endereço
        public Envio(Pedido pedido, string enderecoEntrega)
        {
            PedidoId = pedido.Id;
            EnderecoEntrega = enderecoEntrega; // Directly assign the string
            Status = StatusEnvio.Pendente;
            DataCriacao = DateTime.UtcNow;
        }

        public void Cancelar(string motivo)
        {
            if (Status == StatusEnvio.Entregue)
                throw new InvalidOperationException("Não é possível cancelar um envio já entregue");

            MotivoFalha = motivo;
            Status = StatusEnvio.Cancelado;
        }
    }

    public enum StatusEnvio
    {
        Pendente,
        Entregue,
        Cancelado
    }
}
