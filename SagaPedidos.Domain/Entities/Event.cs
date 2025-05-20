using System;

namespace SagaPedidos.Domain.Entities
{
    // Classe base para todos os eventos do sistema
    public abstract class Event
    {
        public Guid Id { get; private set; }
        public DateTime Timestamp { get; private set; }
        public string EventType { get; protected set; }

        protected Event()
        {
            Id = Guid.NewGuid();
            Timestamp = DateTime.UtcNow;
            EventType = GetType().Name;
        }
    }
}