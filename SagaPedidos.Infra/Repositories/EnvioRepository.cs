using Microsoft.EntityFrameworkCore;
using SagaPedidos.Domain.Entities;
using SagaPedidos.Domain.Interfaces;

namespace SagaPedidos.Infra.Repositories
{
    public class EnvioRepository : IEnvioRepository
    {
        private readonly AppDbContext _context;
        public EnvioRepository(AppDbContext context)
        {
            _context = context;
        }

        public async Task<Envio?> ObterPorIdAsync(int id)
        {
            return await _context.Envios.FirstOrDefaultAsync(e => e.Id == id);
        }

        public async Task AdicionarAsync(Envio envio)
        {
            await _context.Envios.AddAsync(envio);
            await _context.SaveChangesAsync();
        }

        public async Task AtualizarAsync(Envio envio)
        {
            _context.Envios.Update(envio);
            await _context.SaveChangesAsync();
        }
    }
}