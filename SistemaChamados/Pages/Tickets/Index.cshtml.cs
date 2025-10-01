using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Pages.Tickets
{
    [Authorize(Roles = "Admin,Tecnico")] // APENAS ADMIN E T�CNICO PODEM ACESSAR
    public class IndexModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<IndexModel> _logger;

        public IndexModel(AppDbContext context, ILogger<IndexModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        public List<Ticket> Items { get; set; } = new();

        [BindProperty(SupportsGet = true)]
        public PriorityLevel? FiltrarPrioridade { get; set; }

        [BindProperty(SupportsGet = true)]
        public TicketStatus? FiltrarStatus { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // VERIFICA��O ADICIONAL DE SEGURAN�A
            if (User.IsInRole("Usuario"))
            {
                _logger.LogWarning("Usu�rio comum tentou acessar p�gina Index. Redirecionando para UsuarioTickets. User: {UserId}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                return RedirectToPage("/Tickets/UsuarioTickets");
            }

            try
            {
                _logger.LogInformation("Carregando tickets para usu�rio {UserType}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);

                var query = _context.Tickets
                    .Include(t => t.Solicitante)
                    .Include(t => t.Responsavel)
                    .Include(t => t.Categoria)
                    .AsQueryable();

                // Aplicar filtros se especificados
                if (FiltrarPrioridade.HasValue)
                {
                    query = query.Where(t => t.Prioridade == FiltrarPrioridade.Value);
                }

                if (FiltrarStatus.HasValue)
                {
                    query = query.Where(t => t.Status == FiltrarStatus.Value);
                }

                // Buscar todos os tickets (Admin/T�cnico podem ver todos)
                Items = await query
                    .OrderByDescending(t => t.CriadoEm)
                    .ToListAsync();

                _logger.LogInformation("Carregados {Count} tickets", Items.Count);

                return Page();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao carregar tickets");
                Items = new List<Ticket>();
                return Page();
            }
        }
    }
}
