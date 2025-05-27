using Microsoft.EntityFrameworkCore;
using SagaPedidos.Domain.Entities;

namespace SagaPedidos.Infra
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options) { }

        public DbSet<Pedido> Pedidos { get; set; }
        public DbSet<Pagamento> Pagamentos { get; set; }
        public DbSet<Envio> Envios { get; set; }
        public DbSet<ItemPedido> ItensPedido { get; set; }
        public DbSet<Cliente> Clientes { get; set; }
        // public DbSet<Endereco> Enderecos { get; set; } // Commented out or removed

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar entidade Pedido
            modelBuilder.Entity<Pedido>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.EnderecoEntrega).HasColumnName("EnderecoEntregaStr");
                entity.Property(p => p.ValorTotal).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MotivoFalha).IsRequired(false); // Permite valores nulos

                // Configurar relacionamento com ItemPedido
                entity.HasMany(p => p.Itens)
                      .WithOne()
                      .HasForeignKey("PedidoId")
                      .OnDelete(DeleteBehavior.Cascade);
            });

            // Configurar entidade Envio
            modelBuilder.Entity<Envio>(entity =>
            {
                entity.HasKey(e => e.Id);
                entity.Property(e => e.EnderecoEntrega).HasColumnName("EnderecoEntregaStr");
                entity.Property(e => e.MotivoFalha).IsRequired(false); // Permite valores nulos
            });

            // Configurar entidade Pagamento
            modelBuilder.Entity<Pagamento>(entity =>
            {
                entity.HasKey(p => p.Id);
                entity.Property(p => p.Valor).HasColumnType("decimal(18,2)");
                entity.Property(p => p.MotivoFalha).IsRequired(false); // Permite valores nulos
            });

            // Configurar entidade ItemPedido
            modelBuilder.Entity<ItemPedido>(entity =>
            {
                entity.HasKey(i => i.Id);
                entity.Property(i => i.PrecoUnitario).HasColumnType("decimal(18,2)");
            });

            // Configurar entidade Cliente
            modelBuilder.Entity<Cliente>(entity =>
            {
                entity.HasKey(c => c.Id);
            });

        }
    }
}
