using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Pages.Tickets
{
    public class DetalhesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<DetalhesModel> _logger;

        public DetalhesModel(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<DetalhesModel> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
        }

        public List<Anexo> Anexos { get; set; } = new();
        public List<ComentarioTicket> Comentarios { get; set; } = new();
        public Ticket? Ticket { get; set; }

        [BindProperty]
        public StatusUpdateInput Input { get; set; } = new();

        [BindProperty]
        public ComentarioForm ComentarioInput { get; set; } = new();

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

            var userRole = User.FindFirst(ClaimTypes.Role)?.Value;
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");

            if (userRole == "Usuario" && Ticket.SolicitanteId != userId)
            {
                return Forbid();
            }

            Anexos = await _context.Anexos
                .Include(a => a.UsuarioCriador)
                .Where(a => a.TicketId == id)
                .OrderBy(a => a.CriadoEm)
                .ToListAsync();

            var comentariosQuery = _context.ComentariosTicket
                .Include(c => c.Usuario)
                .Where(c => c.TicketId == id);

            if (userRole == "Usuario")
            {
                comentariosQuery = comentariosQuery.Where(c => c.VisivelSolicitante);
            }

            Comentarios = await comentariosQuery
                .OrderBy(c => c.CriadoEm)
                .ToListAsync();

            Input.NovoStatus = Ticket.Status;
            return Page();
        }

        public async Task<IActionResult> OnPostAdicionarComentarioAsync(int id)
        {
            if (string.IsNullOrWhiteSpace(ComentarioInput.Comentario))
            {
                ModelState.AddModelError("ComentarioInput.Comentario", "O comentário não pode estar vazio");
                return await OnGetAsync(id);
            }

            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            if (ticket.Status == TicketStatus.Fechado)
            {
                TempData["Erro"] = "Não é possível adicionar comentários em tickets fechados";
                return RedirectToPage("Detalhes", new { id });
            }

            try
            {
                var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
                var userRole = User.FindFirst(ClaimTypes.Role)?.Value;

                if (userRole == "Usuario" && ticket.SolicitanteId != userId)
                {
                    return Forbid();
                }

                var comentario = new ComentarioTicket
                {
                    TicketId = id,
                    UsuarioId = userId,
                    Comentario = ComentarioInput.Comentario.Trim(),
                    Tipo = TipoComentario.Comentario,
                    VisivelSolicitante = true,
                    CriadoEm = DateTime.UtcNow
                };

                _context.ComentariosTicket.Add(comentario);
                ticket.AtualizadoEm = DateTime.UtcNow;
                await _context.SaveChangesAsync();

                _logger.LogInformation("Comentário adicionado ao ticket {TicketId} por usuário {UserId}", id, userId);
                TempData["Sucesso"] = "Comentário adicionado com sucesso!";
                return RedirectToPage("Detalhes", new { id });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao adicionar comentário ao ticket {TicketId}", id);
                TempData["Erro"] = "Erro ao adicionar comentário. Tente novamente.";
                return RedirectToPage("Detalhes", new { id });
            }
        }

        public async Task<IActionResult> OnPostUpdateStatusAsync(int id)
        {
            var ticket = await _context.Tickets.FindAsync(id);
            if (ticket == null)
            {
                return NotFound();
            }

            var statusAnterior = ticket.Status;
            ticket.Status = Input.NovoStatus;
            ticket.AtualizadoEm = DateTime.UtcNow;

            if (Input.NovoStatus == TicketStatus.Resolvido && statusAnterior != TicketStatus.Resolvido)
            {
                ticket.ResolvidoEm = DateTime.UtcNow;
            }
            else if (Input.NovoStatus == TicketStatus.Fechado && statusAnterior != TicketStatus.Fechado)
            {
                ticket.FechadoEm = DateTime.UtcNow;
            }

            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value ?? "0");
            var comentarioStatus = new ComentarioTicket
            {
                TicketId = id,
                UsuarioId = userId,
                Comentario = $"Status alterado de {statusAnterior} para {Input.NovoStatus}",
                Tipo = TipoComentario.MudancaStatus,
                VisivelSolicitante = true,
                CriadoEm = DateTime.UtcNow
            };

            _context.ComentariosTicket.Add(comentarioStatus);
            await _context.SaveChangesAsync();

            TempData["Sucesso"] = "Status atualizado com sucesso!";
            return RedirectToPage("Detalhes", new { id });
        }

        public async Task<IActionResult> OnGetDownloadAnexoAsync(int anexoId)
        {
            try
            {
                if (anexoId <= 0)
                {
                    return BadRequest("ID do anexo inválido");
                }

                var anexo = await _context.Anexos.FindAsync(anexoId);
                if (anexo == null)
                {
                    return NotFound("Anexo não encontrado");
                }

                if (string.IsNullOrWhiteSpace(anexo.CaminhoArquivo) || string.IsNullOrWhiteSpace(anexo.NomeOriginal))
                {
                    return NotFound("Informações do arquivo incompletas");
                }

                if (string.IsNullOrWhiteSpace(anexo.TipoConteudo))
                {
                    anexo.TipoConteudo = "application/octet-stream";
                }

                if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
                {
                    return StatusCode(500, "Caminho raiz da aplicação não configurado");
                }

                var caminhoRelativo = anexo.CaminhoArquivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, caminhoRelativo);

                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Arquivo físico não encontrado: {anexo.NomeOriginal}");
                }

                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                return File(memory, anexo.TipoConteudo, anexo.NomeOriginal);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao fazer download do anexo {AnexoId}", anexoId);
                return StatusCode(500, "Erro interno ao processar download");
            }
        }

        public class StatusUpdateInput
        {
            [Required]
            public TicketStatus NovoStatus { get; set; }
        }

        public class ComentarioForm
        {
            [Required(ErrorMessage = "O comentário é obrigatório")]
            [StringLength(2000, MinimumLength = 1, ErrorMessage = "O comentário deve ter entre 1 e 2000 caracteres")]
            public string Comentario { get; set; } = string.Empty;
        }
    }
}
