using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using SistemaChamados.Models;
using SistemaChamados.Data;
using static SistemaChamados.Models.Enums;
using System.ComponentModel.DataAnnotations;
using Microsoft.EntityFrameworkCore;
using Microsoft.AspNetCore.Hosting;

namespace SistemaChamados.Pages.Tickets
{
    public class CreateModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly IWebHostEnvironment _webHostEnvironment;
        private readonly ILogger<CreateModel> _logger;

        public CreateModel(AppDbContext context, IWebHostEnvironment webHostEnvironment, ILogger<CreateModel> logger)
        {
            _context = context;
            _webHostEnvironment = webHostEnvironment;
            _logger = logger;
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
            // Recarregar categorias para o caso de erro de valida��o
            Categorias = await _context.Categorias
                .Where(c => c.Ativo)
                .OrderBy(c => c.Nome)
                .ToListAsync();

            if (!ModelState.IsValid)
            {
                return Page();
            }

            // Buscar o usu�rio admin
            var usuarioAdmin = await _context.Usuarios
                .Where(u => u.TipoUsuario == "Admin" || u.Id == 1)
                .FirstOrDefaultAsync();

            if (usuarioAdmin == null)
            {
                ModelState.AddModelError("", "Nenhum usu�rio encontrado no sistema.");
                return Page();
            }

            // ===== VALIDA��O DE ANEXOS =====
            if (Input.Anexos != null && Input.Anexos.Any())
            {
                const int maxFiles = 10;
                const long maxSizeBytes = 10 * 1024 * 1024; // 10MB
                var allowedExtensions = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar", ".xlsx", ".xls", ".pptx", ".ppt" };

                // Validar n�mero de arquivos
                if (Input.Anexos.Count > maxFiles)
                {
                    ModelState.AddModelError("Input.Anexos", $"Voc� pode enviar no m�ximo {maxFiles} arquivos.");
                    return Page();
                }

                // Validar cada arquivo
                foreach (var file in Input.Anexos)
                {
                    // Validar tamanho
                    if (file.Length > maxSizeBytes)
                    {
                        ModelState.AddModelError("Input.Anexos", $"O arquivo '{file.FileName}' excede o tamanho m�ximo de 10MB.");
                        return Page();
                    }

                    // Validar extens�o
                    var extension = Path.GetExtension(file.FileName).ToLowerInvariant();
                    if (!allowedExtensions.Contains(extension))
                    {
                        ModelState.AddModelError("Input.Anexos", $"O arquivo '{file.FileName}' possui um tipo n�o permitido. Tipos aceitos: {string.Join(", ", allowedExtensions)}");
                        return Page();
                    }
                }
            }

            try
            {
                // Criar o ticket
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

                _logger.LogInformation("Ticket criado com sucesso. ID: {TicketId}", novoTicket.Id);

                // ===== PROCESSAR ANEXOS =====
                if (Input.Anexos != null && Input.Anexos.Any())
                {
                    var uploadPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tickets", novoTicket.Id.ToString());

                    // Criar diret�rio se n�o existir
                    if (!Directory.Exists(uploadPath))
                    {
                        Directory.CreateDirectory(uploadPath);
                    }

                    foreach (var file in Input.Anexos)
                    {
                        // Gerar nome �nico para o arquivo
                        var uniqueFileName = $"{Guid.NewGuid()}_{Path.GetFileName(file.FileName)}";
                        var filePath = Path.Combine(uploadPath, uniqueFileName);

                        // Salvar arquivo fisicamente
                        using (var stream = new FileStream(filePath, FileMode.Create))
                        {
                            await file.CopyToAsync(stream);
                        }

                        // Criar registro no banco de dados
                        var anexo = new Anexo
                        {
                            TicketId = novoTicket.Id,
                            NomeOriginal = file.FileName,
                            NomeArquivo = uniqueFileName,
                            CaminhoArquivo = $"/uploads/tickets/{novoTicket.Id}/{uniqueFileName}",
                            TipoConteudo = file.ContentType,
                            TamanhoBytes = file.Length,
                            CriadoPor = usuarioAdmin.Id,
                            CriadoEm = DateTime.UtcNow
                        };

                        _context.Anexos.Add(anexo);

                        _logger.LogInformation("Anexo salvo: {FileName} ({Size} bytes)", file.FileName, file.Length);
                    }

                    await _context.SaveChangesAsync();
                    _logger.LogInformation("Total de {Count} anexos salvos para o ticket {TicketId}", Input.Anexos.Count, novoTicket.Id);
                }

                TempData["SuccessMessage"] = $"Chamado #{novoTicket.Id} criado com sucesso!";
                return RedirectToPage("Index");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao criar ticket");
                ModelState.AddModelError("", "Ocorreu um erro ao criar o chamado. Tente novamente.");
                return Page();
            }
        }

        public class TicketInput
        {
            [Required(ErrorMessage = "O t�tulo � obrigat�rio")]
            [StringLength(120, ErrorMessage = "O t�tulo deve ter no m�ximo 120 caracteres")]
            [Display(Name = "T�tulo")]
            public string Titulo { get; set; } = string.Empty;

            [Required(ErrorMessage = "A descri��o � obrigat�ria")]
            [StringLength(2000, MinimumLength = 10, ErrorMessage = "A descri��o deve ter entre 10 e 2000 caracteres")]
            [Display(Name = "Descri��o")]
            public string Descricao { get; set; } = string.Empty;

            [Required(ErrorMessage = "Selecione uma categoria")]
            [Range(1, int.MaxValue, ErrorMessage = "Selecione uma categoria v�lida")]
            [Display(Name = "Categoria")]
            public int CategoriaId { get; set; }

            [Display(Name = "Prioridade")]
            public PriorityLevel Prioridade { get; set; } = PriorityLevel.Média;

            [Display(Name = "Anexos")]
            public List<IFormFile>? Anexos { get; set; }
        }
    }
}
