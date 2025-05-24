using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;
using Microsoft.Extensions.Configuration;
using System;
using System.IO;

namespace SagaPedidos.Infra
{
    // Factory para criar inst�ncias do DbContext durante o design-time (migra��es)
    public class AppDbContextFactory : IDesignTimeDbContextFactory<AppDbContext>
    {
        public AppDbContext CreateDbContext(string[] args)
        {
            Console.WriteLine("============== CRIANDO CONTEXTO PARA MIGRA��ES ==============");
            
            // Obter o diret�rio base do projeto
            var basePath = Directory.GetCurrentDirectory();
            Console.WriteLine($"Diret�rio atual: {basePath}");

            // Configura��o simplificada para design-time
            var connectionString = "Server=(localdb)\\mssqllocaldb;Database=SagaPedidos;Trusted_Connection=True;MultipleActiveResultSets=true";
            Console.WriteLine($"Usando conex�o: {connectionString}");

            // Criar as op��es para o contexto
            var optionsBuilder = new DbContextOptionsBuilder<AppDbContext>();
            optionsBuilder.UseSqlServer(connectionString);
            
            Console.WriteLine("Contexto criado com sucesso!");

            return new AppDbContext(optionsBuilder.Options);
        }
    }
}