using System;

namespace SagaPedidos.Domain.Entities
{
    public class Endereco
    {
        public int Id { get; private set; }
        public string Rua { get; private set; }
        public string Numero { get; private set; }
        public string Cidade { get; private set; }

        // Construtor para EF Core
        protected Endereco() { }

        public Endereco(string rua, string numero, string cidade)
        {
            Rua = rua;
            Numero = numero;
            Cidade = cidade;
        }

        public override string ToString()
        {
            return $"{Rua}, {Numero}, {Cidade}";
        }

        public static Endereco FromString(string enderecoCompleto)
        {
            var partes = enderecoCompleto.Split(',', 3);
            return new Endereco(
                partes.Length > 0 ? partes[0].Trim() : "Rua Padrão",
                partes.Length > 1 ? partes[1].Trim() : "S/N",
                partes.Length > 2 ? partes[2].Trim() : "Cidade Padrão"
            );
        }
    }
}