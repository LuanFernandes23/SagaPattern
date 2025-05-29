using System;
using System.Collections.Generic;

namespace SagaPedidos.Domain.Messages
{
    /// <summary>
    /// Representa uma mensagem no padrão Saga
    /// </summary>
    public class SagaMessage
    {
        public Guid Id { get; set; }
        public string Type { get; set; }
        public object Payload { get; set; }
        public Dictionary<string, string> Headers { get; set; }
        public DateTime CreatedAt { get; set; }

        public SagaMessage()
        {
            Id = Guid.NewGuid();
            Headers = new Dictionary<string, string>();
            CreatedAt = DateTime.UtcNow;
        }

        public SagaMessage(string type, object payload) : this() 
        {
            Type = type;
            Payload = payload;
        }

        public void AddHeader(string key, string value)
        {
            Headers[key] = value;
        }
    }
}