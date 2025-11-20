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
    [Authorize(Roles = "Usuario, Tecnico, Admin")]
    public class UsuarioTicketsModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<UsuarioTicketsModel> _logger;
        private readonly IWebHostEnvironment _webHostEnvironment;

        public UsuarioTicketsModel(AppDbContext context, ILogger<UsuarioTicketsModel> logger, IWebHostEnvironment webHostEnvironment)
        {
            _context = context;
            _logger = logger;
            _webHostEnvironment = webHostEnvironment;
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
            _logger.LogInformation("=== TENTATIVA DE CRIAR TICKET COM ANEXOS ===");
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

                // Validar anexos
                if (Input.Anexos != null && Input.Anexos.Count > 0)
                {
                    var validationResult = ValidarAnexos(Input.Anexos);
                    if (!string.IsNullOrEmpty(validationResult))
                    {
                        ErrorMessage = validationResult;
                        await CarregarDadosAsync();
                        return Page();
                    }
                }

                // Criar ticket
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

                _logger.LogInformation("✅ Ticket criado com sucesso: ID {TicketId}", novoTicket.Id);

                // Processar anexos se existirem
                var anexosSalvos = 0;
                if (Input.Anexos != null && Input.Anexos.Count > 0)
                {
                    anexosSalvos = await ProcessarAnexosAsync(Input.Anexos, novoTicket.Id, usuarioId);
                }

                var mensagem = anexosSalvos > 0
                    ? $"Chamado '{Input.Titulo}' criado com sucesso! {anexosSalvos} arquivo(s) anexado(s)."
                    : $"Chamado '{Input.Titulo}' criado com sucesso!";

                TempData["SuccessMessage"] = mensagem;

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

        private async Task<int> ProcessarAnexosAsync(List<IFormFile> anexos, int ticketId, int usuarioId)
        {
            try
            {
                // Criar diretório de uploads se não existir
                var uploadsPath = Path.Combine(_webHostEnvironment.WebRootPath, "uploads", "tickets", ticketId.ToString());
                if (!Directory.Exists(uploadsPath))
                {
                    Directory.CreateDirectory(uploadsPath);
                    _logger.LogInformation("Diretório criado: {Path}", uploadsPath);
                }

                var anexosSalvos = 0;

                foreach (var arquivo in anexos)
                {
                    if (arquivo.Length > 0)
                    {
                        // Gerar nome único para o arquivo
                        var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                        var nomeUnico = $"{Guid.NewGuid()}{extensao}";
                        var caminhoCompleto = Path.Combine(uploadsPath, nomeUnico);

                        // Salvar arquivo fisicamente
                        using (var stream = new FileStream(caminhoCompleto, FileMode.Create))
                        {
                            await arquivo.CopyToAsync(stream);
                        }

                        // Salvar informações no banco
                        var anexo = new Anexo
                        {
                            TicketId = ticketId,
                            NomeArquivo = nomeUnico,
                            NomeOriginal = arquivo.FileName,
                            TamanhoBytes = arquivo.Length,
                            TipoConteudo = arquivo.ContentType,
                            CaminhoArquivo = $"/uploads/tickets/{ticketId}/{nomeUnico}",
                            CriadoEm = DateTime.UtcNow,
                            CriadoPor = usuarioId
                        };

                        _context.Anexos.Add(anexo);
                        anexosSalvos++;

                        _logger.LogInformation("Anexo salvo: {NomeOriginal} -> {NomeUnico}", arquivo.FileName, nomeUnico);
                    }
                }

                await _context.SaveChangesAsync();
                return anexosSalvos;
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro ao processar anexos");
                return 0;
            }
        }

        private string ValidarAnexos(List<IFormFile> anexos)
        {
            const long maxTamanhoBytes = 10 * 1024 * 1024; // 10MB
            const int maxArquivos = 5;

            var extensoesPermitidas = new[] { ".pdf", ".doc", ".docx", ".txt", ".jpg", ".jpeg", ".png", ".gif", ".zip", ".rar", ".xlsx", ".xls", ".pptx", ".ppt" };

            if (anexos.Count > maxArquivos)
            {
                return $"Máximo {maxArquivos} arquivos permitidos.";
            }

            foreach (var arquivo in anexos)
            {
                if (arquivo.Length > maxTamanhoBytes)
                {
                    return $"Arquivo '{arquivo.FileName}' excede o tamanho máximo de 10MB.";
                }

                var extensao = Path.GetExtension(arquivo.FileName).ToLowerInvariant();
                if (!extensoesPermitidas.Contains(extensao))
                {
                    return $"Tipo de arquivo '{extensao}' não permitido. Tipos permitidos: {string.Join(", ", extensoesPermitidas)}";
                }
            }

            return string.Empty; // Todos válidos
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
            public PriorityLevel Prioridade { get; set; } = PriorityLevel.Media;

            [Display(Name = "Anexos")]
            public List<IFormFile>? Anexos { get; set; } = new List<IFormFile>();
        }
    }
}
