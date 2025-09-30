using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaChamados.Models;
using SistemaChamados.Data;
using static SistemaChamados.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;

namespace SistemaChamados.Pages.Tickets
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;

        public CreateModel(AppDbContext context)
        {
            _context = context;
        }

        [BindProperty]
        public TicketInput Input { get; set; } = new();

        public IEnumerable<Models.Categoria> Categorias { get; private set; } = new List<Categoria>();

        public async Task OnGetAsync()
        {
            // Buscar categorias do banco de dados
            Categorias = await _context.Categorias
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .ToListAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            // Recarregar categorias para o caso de erro de validação
            Categorias = await _context.Categorias
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Buscar o usuário admin
            var usuarioAdmin = await _context.Usuarios
                .Where(u => u.TipoUsuario == "Admin" || u.Id == 1)
                .FirstOrDefaultAsync();

            if (usuarioAdmin == null)
            {
                ModelState.AddModelError("", "Nenhum usuário encontrado no sistema.");
                return Page();
            }

            var novoTicket = new Models.Ticket
            {
                Titulo = Input.Titulo,
                Descricao = Input.Descricao,
                CategoriaId = Input.CategoriaId,
                Prioridade = Input.Prioridade,
                Status = TicketStatus.Aberto,
                SolicitanteId = usuarioAdmin.Id,
                CriadoEm = DateTime.UtcNow,
                AtualizadoEm = DateTime.UtcNow,
                TempoRespostaHoras = 24
            };

            _context.Tickets.Add(novoTicket);
            await _context.SaveChangesAsync();

            return RedirectToPage("Index");
        }

        public class TicketInput
        {
            [Required, StringLength(120)]
            [Display(Name = "Título")]
            public string Titulo { get; set; } = string.Empty;

            [Required, StringLength(2000)]
            [Display(Name = "Descrição")]
            public string Descricao { get; set; } = string.Empty;

            [Display(Name = "Categoria"), Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria")]
            public int CategoriaId { get; set; }

            [Display(Name = "Prioridade")]
            public PriorityLevel Prioridade { get; set; } = PriorityLevel.Média;

            [Display(Name = "Anexos")]
            public List<IFormFile>? Anexos { get; set; }
        }
    }
}
