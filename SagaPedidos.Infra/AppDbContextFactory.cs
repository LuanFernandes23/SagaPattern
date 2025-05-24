using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SagaPedidos.Infra
{
    // Factory para criar instâncias do DbContext durante o design-time (migrações)
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("============== CRIANDO CONTEXTO PARA MIGRAÇÕES ==============");
            
            // Obter o diretório base do projeto
            var basePath = Directory.GetCurrentDirectory();
            Console.WriteLine($"Diretório atual: {basePath}");

            // Configuração simplificada para design-time
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SagaPedidos;Trusted_Connection=True;MultipleActiveResultSets=true";
            Console.WriteLine($"Usando conexão: {connectionString}");

            // Criar as opções para o contexto
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            Console.WriteLine("Contexto criado com sucesso!");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}