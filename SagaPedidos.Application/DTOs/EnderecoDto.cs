namespace SagaPedidos.Application.Dtos
{
    // Simplified EnderecoDto - uses only a single string format
    public class EnderecoDto
    {
        // Single line representation of address
        public string EnderecoCompleto { get; set; }

        // Constructor with default value
        public EnderecoDto() 
        {
            EnderecoCompleto = "";
        }

        // Constructor that accepts a complete address string
        public EnderecoDto(string enderecoCompleto)
        {
            EnderecoCompleto = enderecoCompleto;
        }

        // Override ToString to return the full address string
        public override string ToString()
        {
            return EnderecoCompleto;
        }
    }
}