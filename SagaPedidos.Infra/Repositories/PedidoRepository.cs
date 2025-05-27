using Microsoft.EntityFrameworkCore;
using SagaPedidos.Domain.Entities;
using SagaPedidos.Domain.Events;
using SagaPedidos.Domain.Interfaces;
using SagaPedidos.Domain.Messages;
using System;
using System.Threading.Tasks;

namespace SagaPedidos.Infra.Repositories
{
    public class PedidoRepository : IPedidoRepository
    {
        private readonly AppDbContext _context;
        private readonly IPublisher _publisher;
        private readonly Application.EventHandlers.PedidoCriadoHandler _pedidoCriadoHandler;

        public PedidoRepository(
            AppDbContext context, 
            IPublisher publisher,
            Application.EventHandlers.PedidoCriadoHandler pedidoCriadoHandler)
        {
            _context = context;
            _publisher = publisher;
            _pedidoCriadoHandler = pedidoCriadoHandler;
        }

        public async Task<Pedido?> ObterPorIdAsync(int id)
        {
            return await _context.Pedidos.Include(p => p.Itens).FirstOrDefaultAsync(p => p.Id == id);
        }

        public async Task AdicionarAsync(Pedido pedido)
        {
            await _context.Pedidos.AddAsync(pedido);
            await _context.SaveChangesAsync();
            
            Console.WriteLine($"Pedido {pedido.Id} salvo no banco de dados com sucesso.");
            
            // Publicar evento de pedido criado para iniciar a saga
            try
            {
                var evento = new PedidoCriadoEvent(
                    pedido.Id,
                    pedido.ClienteId,
                    pedido.ValorTotal,
                    pedido.EnderecoEntrega,
                    "cartao" // Método de pagamento padrão para teste
                );
                
                Console.WriteLine($"Publicando evento PedidoCriado para o pedido {pedido.Id}...");
                
                // Apenas publicar na fila, removendo a chamada direta ao handler
                var sagaMessage = new SagaMessage("PedidoCriado", evento);
                sagaMessage.AddHeader("PedidoId", pedido.Id.ToString());
                _publisher.Publish(sagaMessage);
                
                Console.WriteLine($"Evento PedidoCriado publicado com sucesso para o pedido {pedido.Id}");
            }
            catch (Exception ex)
            {
                Console.WriteLine($"Erro ao publicar evento PedidoCriado: {ex.Message}");
                Console.WriteLine($"StackTrace: {ex.StackTrace}");
            }
        }

        public async Task AtualizarAsync(Pedido pedido)
        {
            _context.Pedidos.Update(pedido);
            await _context.SaveChangesAsync();
        }
    }
}