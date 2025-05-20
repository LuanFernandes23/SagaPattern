using SagaPedidos.Domain.Messages;

namespace SagaPedidos.Domain.Messaging
{
    // Interface para publicação de mensagens
    public interface IPublisher
    {
        void Publish(SagaMessage message);
    }
}