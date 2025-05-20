using SagaPedidos.Domain.Entities;
using System;

namespace SagaPedidos.Domain.Events
{
    // Evento disparado para iniciar o processamento de envio
    public class ProcessarEnvioEvent : Event
    {
        public int PedidoId { get; private set; }
        public string EnderecoEntrega { get; private set; }

        public ProcessarEnvioEvent(int pedidoId, string enderecoEntrega)
        {
            PedidoId = pedidoId;
            EnderecoEntrega = enderecoEntrega;
        }
    }

    // Evento disparado quando um envio é processado com sucesso
    public class EnvioProcessadoEvent : Event
    {
        public int EnvioId { get; private set; }
        public int PedidoId { get; private set; }

        public EnvioProcessadoEvent(int envioId, int pedidoId)
        {
            EnvioId = envioId;
            PedidoId = pedidoId;
        }
    }

    // Evento disparado quando um envio falha
    public class EnvioFalhadoEvent : Event
    {
        public int PedidoId { get; private set; }
        public string Motivo { get; private set; }

        public EnvioFalhadoEvent(int pedidoId, string motivo)
        {
            PedidoId = pedidoId;
            Motivo = motivo;
        }
    }
}