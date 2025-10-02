using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using System.ComponentModel.DataAnnotations;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Pages.Tickets
{
    public class DetalhesModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment = null!;
        
        public DetalhesModel(AppDbContext context, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
        }

        public List<Anexo> Anexos { get; set; } = new();

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

            Anexos = await _context.Anexos
                .Include(a => a.UsuarioCriador)
                .Where(a => a.TicketId == id)
                .OrderBy(a => a.CriadoEm)
                .ToListAsync();

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

        // Método para download
        public async Task<IActionResult> OnGetDownloadAnexoAsync(int anexoId)
        {
            try
            {
                // Validar anexoId
                if (anexoId <= 0)
                {
                    return BadRequest("ID do anexo inválido");
                }

                // Buscar anexo
                var anexo = await _context.Anexos.FindAsync(anexoId);
                if (anexo == null)
                {
                    return NotFound("Anexo não encontrado");
                }

                // Validar campos do anexo
                if (string.IsNullOrWhiteSpace(anexo.CaminhoArquivo))
                {
                    return NotFound("Caminho do arquivo não encontrado");
                }

                if (string.IsNullOrWhiteSpace(anexo.NomeOriginal))
                {
                    return NotFound("Nome original do arquivo não encontrado");
                }

                if (string.IsNullOrWhiteSpace(anexo.TipoConteudo))
                {
                    anexo.TipoConteudo = "application/octet-stream"; // Tipo genérico como fallback
                }

                // Validar WebRootPath
                if (string.IsNullOrWhiteSpace(_webHostEnvironment.WebRootPath))
                {
                    return StatusCode(500, "Caminho raiz da aplicação não configurado");
                }

                // Construir caminho do arquivo
                var caminhoRelativo = anexo.CaminhoArquivo.TrimStart('/').Replace('/', Path.DirectorySeparatorChar);
                var filePath = Path.Combine(_webHostEnvironment.WebRootPath, caminhoRelativo);

                // Verificar se arquivo existe
                if (!System.IO.File.Exists(filePath))
                {
                    return NotFound($"Arquivo físico não encontrado: {anexo.NomeOriginal}");
                }

                // Ler arquivo para memória
                var memory = new MemoryStream();
                using (var stream = new FileStream(filePath, FileMode.Open, FileAccess.Read))
                {
                    await stream.CopyToAsync(memory);
                }
                memory.Position = 0;

                // Retornar arquivo
                return File(memory, anexo.TipoConteudo, anexo.NomeOriginal);
            }
            catch (Exception ex)
            {
                // Log do erro (se você tiver logger configurado)
                // _logger?.LogError(ex, "Erro ao fazer download do anexo {AnexoId}", anexoId);

                return StatusCode(500, "Erro interno ao processar download");
            }
        }

        public class StatusUpdateInput
        {
            [Required]
            public TicketStatus NovoStatus { get; set; }
        }
    }
}
