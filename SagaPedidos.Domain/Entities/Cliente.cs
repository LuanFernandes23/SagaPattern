using System;

namespace SagaPedidos.Domain.Entities
{
    public class Cliente
    {
        public int Id { get; set; }
        public string Nome { get; set; }
        
        public Cliente() { }
        
        public Cliente(int id)
        {
            Id = id;
            Nome = $"Cliente {id}";
        }
    }
}
