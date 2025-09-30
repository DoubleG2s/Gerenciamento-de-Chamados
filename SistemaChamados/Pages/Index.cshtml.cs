using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;

namespace SistemaChamados.Pages
{
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        public IndexModel(AppDbContext context) => _context = context;

        // KPIs
        public int Abertos { get; private set; }
        public int EmAndamento { get; private set; }
        public int Resolvidos { get; private set; }
        public int Fechados { get; private set; }   // opcional exibir
        public int Total { get; private set; }

        public async Task OnGetAsync()
        {
            // ÚNICA consulta com agregações por condição
            var counters = await _context.Tickets
                .GroupBy(_ => 1)
                .Select(g => new
                {
                    Abertos = g.Count(t => t.Status == Enums.TicketStatus.Aberto),
                    EmAndamento = g.Count(t => t.Status == Enums.TicketStatus.Andamento),
                    Resolvidos = g.Count(t => t.Status == Enums.TicketStatus.Resolvido),
                    Fechados = g.Count(t => t.Status == Enums.TicketStatus.Fechado),
                    Total = g.Count()
                })
                .FirstOrDefaultAsync();

            if (counters is not null)
            {
                Abertos = counters.Abertos;
                EmAndamento = counters.EmAndamento;
                Resolvidos = counters.Resolvidos;
                Fechados = counters.Fechados;
                Total = counters.Total;
            }
        }
    }
}
