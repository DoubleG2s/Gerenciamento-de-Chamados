using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaChamados.Models;
using SistemaChamados.Data;
using static SistemaChamados.Models.Enums;
using Microsoft.EntityFrameworkCore;
using System.ComponentModel.DataAnnotations;

namespace SistemaChamados.Pages.Tickets
{
    public class DetalhesModel : PageModel
    {
        private readonly AppDbContext _context;

        public DetalhesModel(AppDbContext context)
        {
            _context = context;
        }

        public Ticket? Ticket { get; set; }

        [BindProperty]
        public StatusUpdateInput Input { get; set; } = new();

        public async Task<IActionResult> OnGetAsync(int id)
        {
            Ticket = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Solicitante)
                .Include(t => t.Responsavel)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (Ticket == null)
            {
                return NotFound();
            }

            Input.NovoStatus = Ticket.Status;
            return Page();
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            ticket.Status = Input.NovoStatus;
            ticket.AtualizadoEm = DateTime.UtcNow;

            if (Input.NovoStatus == TicketStatus.Resolvido)
            {
                ticket.ResolvidoEm = DateTime.UtcNow;
            }
            else if (Input.NovoStatus == TicketStatus.Fechado)
            {
                ticket.FechadoEm = DateTime.UtcNow;
            }

            await _context.SaveChangesAsync();

            return RedirectToPage("Detalhes", new { id });
        }

        public class StatusUpdateInput
        {
            [Required]
            public TicketStatus NovoStatus { get; set; }
        }
    }
}
