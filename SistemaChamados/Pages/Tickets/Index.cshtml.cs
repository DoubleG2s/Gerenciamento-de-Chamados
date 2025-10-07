using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Pages.Tickets
{
    [Authorize(Roles = "Admin,Tecnico")]
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

        // Propriedades para armazenar os filtros aplicados (usado na view)
        public PriorityLevel? FiltrarPrioridade { get; set; }
        public TicketStatus? FiltrarStatus { get; set; }

        // Parâmetros recebidos via query string (como strings)
        [BindProperty(SupportsGet = true)]
        public string Prioridade { get; set; }

        [BindProperty(SupportsGet = true)]
        public string Status { get; set; }

        public async Task<IActionResult> OnGetAsync()
        {
            // VERIFICAÇÃO ADICIONAL DE SEGURANÇA
            if (User.IsInRole("Usuario"))
            {
                _logger.LogWarning("Usuário comum tentou acessar página Index. Redirecionando para UsuarioTickets. User: {UserId}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.NameIdentifier)?.Value);

                return RedirectToPage("/Tickets/UsuarioTickets");
            }

            try
            {
                _logger.LogInformation("Carregando tickets para usuário {UserType}",
                    User.FindFirst(System.Security.Claims.ClaimTypes.Role)?.Value);

                var query = _context.Tickets
                    .Include(t => t.Solicitante)
                    .Include(t => t.Responsavel)
                    .Include(t => t.Categoria)
                    .AsQueryable();

                // Converter e aplicar filtro de prioridade
                if (!string.IsNullOrEmpty(Prioridade))
                {
                    if (Enum.TryParse<PriorityLevel>(Prioridade, true, out var prioridadeEnum))
                    {
                        query = query.Where(t => t.Prioridade == prioridadeEnum);
                        FiltrarPrioridade = prioridadeEnum;
                        _logger.LogInformation("Filtro de prioridade aplicado: {Prioridade}", prioridadeEnum);
                    }
                }

                // Converter e aplicar filtro de status
                if (!string.IsNullOrEmpty(Status))
                {
                    if (Enum.TryParse<TicketStatus>(Status, true, out var statusEnum))
                    {
                        query = query.Where(t => t.Status == statusEnum);
                        FiltrarStatus = statusEnum;
                        _logger.LogInformation("Filtro de status aplicado: {Status}", statusEnum);
                    }
                }

                // Buscar todos os tickets (Admin/Técnico podem ver todos)
                Items = await query
                    .OrderByDescending(t => t.CriadoEm)
                    .ToListAsync();

                _logger.LogInformation("Carregados {Count} tickets com filtros - Prioridade: {Prioridade}, Status: {Status}",
                    Items.Count, FiltrarPrioridade?.ToString() ?? "Todos", FiltrarStatus?.ToString() ?? "Todos");

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
