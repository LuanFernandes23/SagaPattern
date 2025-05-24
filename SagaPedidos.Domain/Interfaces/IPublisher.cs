using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Domain.Interfaces
{
    // Interface para publica��o de mensagens
    public interface IPublisher
    {
        void Publish(SagaMessage message);
    }
}