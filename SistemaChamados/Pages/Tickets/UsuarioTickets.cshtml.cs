using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using static SistemaChamados.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;

namespace SistemaChamados.Pages.Tickets
{
    [Authorize(Roles = "Usuario")]
    public class UsuarioTicketsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsuarioTicketsModel> _logger;

        public UsuarioTicketsModel(AppDbContext context, ILogger<UsuarioTicketsModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public TicketInput Input { get; set; } = new();

        public List<Ticket> MeusTickets { get; set; } = new();
        public IEnumerable<Categoria> Categorias { get; private set; } = new List<Categoria>();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }

        public async Task OnGetAsync()
        {
            await CarregarDadosAsync();

            // Verificar mensagens do TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"].ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"].ToString();
            }
        }

        public async Task<IActionResult> OnPostCriarTicketAsync()
        {
            _logger.LogInformation("=== TENTATIVA DE CRIAR TICKET ===");
            _logger.LogInformation("Usuário: {UserId}, Título: {Titulo}", GetCurrentUserId(), Input.Titulo);

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor, corrija os erros no formulário.";
                await CarregarDadosAsync();
                return Page();
            }

            try
            {
                var usuarioId = GetCurrentUserId();
                if (usuarioId == 0)
                {
                    ErrorMessage = "Usuário não identificado.";
                    await CarregarDadosAsync();
                    return Page();
                }

                var novoTicket = new Ticket
                {
                    Titulo = Input.Titulo.Trim(),
                    Descricao = Input.Descricao.Trim(),
                    CategoriaId = Input.CategoriaId,
                    Prioridade = Input.Prioridade,
                    Status = TicketStatus.Aberto,
                    SolicitanteId = usuarioId,
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow,
                    TempoRespostaHoras = 24
                };

                _context.Tickets.Add(novoTicket);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Ticket criado com sucesso: ID {TicketId}, Título: {Titulo}",
                    novoTicket.Id, novoTicket.Titulo);

                TempData["SuccessMessage"] = $"Chamado '{Input.Titulo}' foi criado com sucesso!";

                return RedirectToPage("UsuarioTickets");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar ticket");
                ErrorMessage = "Erro interno ao criar chamado. Tente novamente.";
                await CarregarDadosAsync();
                return Page();
            }
        }

        private async Task CarregarDadosAsync()
        {
            var usuarioId = GetCurrentUserId();

            // Carregar tickets do usuário
            MeusTickets = await _context.Tickets
                .Include(t => t.Categoria)
                .Include(t => t.Solicitante)
                .Include(t => t.Responsavel)
                .Where(t => t.SolicitanteId == usuarioId)
                .OrderByDescending(t => t.CriadoEm)
                .ToListAsync();

            // Carregar categorias ativas
            Categorias = await _context.Categorias
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        public class TicketInput
        {
            [Required(ErrorMessage = "Título é obrigatório")]
            [StringLength(120, ErrorMessage = "Título deve ter no máximo 120 caracteres")]
            [Display(Name = "Título")]
            public string Titulo { get; set; } = string.Empty;

            [Required(ErrorMessage = "Descrição é obrigatória")]
            [StringLength(2000, ErrorMessage = "Descrição deve ter no máximo 2000 caracteres")]
            [Display(Name = "Descrição")]
            public string Descricao { get; set; } = string.Empty;

            [Display(Name = "Categoria")]
            [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
            public int CategoriaId { get; set; }

            [Display(Name = "Prioridade")]
            public PriorityLevel Prioridade { get; set; } = PriorityLevel.Média;
        }
    }
}
