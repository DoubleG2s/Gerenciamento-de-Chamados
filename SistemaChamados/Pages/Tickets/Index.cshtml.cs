using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaChamados.Models;
using SistemaChamados.Data;
using static SistemaChamados.Models.Enums;
using Microsoft.EntityFrameworkCore;

namespace SistemaChamados.Pages.Tickets
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;

        public IndexModel(AppDbContext context) => _context = context;

        public IEnumerable<Ticket> Items { get; private set; } = [];
        public Enums.TicketStatus? FiltrarStatus { get; private set; }
        public Enums.PriorityLevel? FiltrarPrioridade { get; private set; }

        public async Task OnGetAsync(Enums.PriorityLevel? prioridade, Enums.TicketStatus? status)
        {
            FiltrarPrioridade = prioridade;
            FiltrarStatus = status;

            // Buscar tickets do banco com relacionamentos
            var query = _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Solicitante)
                .AsQueryable();

            // Filtrar por prioridade se fornecida
            if (prioridade.HasValue)
                query = query.Where(t => t.Prioridade == prioridade.Value);

            // Filtrar por status se fornecido
            if (status.HasValue)
                query = query.Where(t => t.Status == status.Value);

            // Ordenar por data de criação (mais recente primeiro)
            Items = await query.OrderByDescending(t => t.CriadoEm).ToListAsync();
        }
    }
}
