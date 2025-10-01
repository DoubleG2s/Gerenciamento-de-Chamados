using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using System.ComponentModel.DataAnnotations;
using System.Text.RegularExpressions;
using System.Security.Claims;

namespace SistemaChamados.Pages.Configs
{
    [Authorize] // Todos os usuários logados podem acessar
    public class ConfigurarModel : PageModel
    {
        private readonly AppDbContext _context;
        private readonly ILogger<ConfigurarModel> _logger;

        public ConfigurarModel(AppDbContext context, ILogger<ConfigurarModel> logger)
        {
            _context = context;
            _logger = logger;
        }

        [BindProperty]
        public CriarUsuarioInput CriarUsuario { get; set; } = new();

        [BindProperty]
        public EditarUsuarioInput EditarUsuario { get; set; } = new();

        [BindProperty]
        public AlterarSenhaInput AlterarSenha { get; set; } = new();

        public string? SuccessMessage { get; set; }
        public string? ErrorMessage { get; set; }
        public string ActiveTab { get; set; } = "pane-senha";

        // Lista de usuários para a tabela (apenas Admin pode ver)
        public List<Usuario> Usuarios { get; set; } = new();

        public async Task OnGetAsync(string? tab = null)
        {
            // Verificar se há mensagem de sucesso no TempData
            if (TempData["SuccessMessage"] != null)
            {
                SuccessMessage = TempData["SuccessMessage"].ToString();
            }
            if (TempData["ErrorMessage"] != null)
            {
                ErrorMessage = TempData["ErrorMessage"].ToString();
            }

            // Definir aba ativa
            ActiveTab = tab switch
            {
                "usuarios" => "pane-usuarios",
                "tema" => "pane-tema",
                _ => "pane-senha"
            };

            // Carregar usuários para a tabela (apenas Admin)
            if (User.IsInRole("Admin"))
            {
                await CarregarUsuariosAsync();
            }
        }

        public async Task<IActionResult> OnPostAlterarSenhaAsync()
        {
            // Manter na aba de senha
            ActiveTab = "pane-senha";

            _logger.LogInformation("=== TENTATIVA DE ALTERAR SENHA ===");

            var usuarioId = GetCurrentUserId();
            _logger.LogInformation("Usuário ID: {UserId}", usuarioId);

            // Limpar erros dos outros formulários
            var keysToRemove = ModelState.Keys.Where(k => !k.StartsWith("AlterarSenha")).ToList();
            foreach (var key in keysToRemove)
            {
                ModelState.Remove(key);
            }

            // VALIDAÇÕES MANUAIS DETALHADAS
            var errors = new List<string>();

            // 1. Verificar campos obrigatórios
            if (string.IsNullOrWhiteSpace(AlterarSenha.SenhaAtual))
            {
                errors.Add("Senha atual é obrigatória");
                ModelState.AddModelError("AlterarSenha.SenhaAtual", "Senha atual é obrigatória");
            }

            if (string.IsNullOrWhiteSpace(AlterarSenha.NovaSenha))
            {
                errors.Add("Nova senha é obrigatória");
                ModelState.AddModelError("AlterarSenha.NovaSenha", "Nova senha é obrigatória");
            }

            if (string.IsNullOrWhiteSpace(AlterarSenha.ConfirmarSenha))
            {
                errors.Add("Confirmação de senha é obrigatória");
                ModelState.AddModelError("AlterarSenha.ConfirmarSenha", "Confirmação de senha é obrigatória");
            }

            // 2. Validar força da nova senha
            if (!string.IsNullOrWhiteSpace(AlterarSenha.NovaSenha))
            {
                if (AlterarSenha.NovaSenha.Length < 8)
                {
                    errors.Add("Nova senha deve ter no mínimo 8 caracteres");
                    ModelState.AddModelError("AlterarSenha.NovaSenha", "Nova senha deve ter no mínimo 8 caracteres");
                }

                if (!ValidarSenhaForte(AlterarSenha.NovaSenha))
                {
                    errors.Add("Nova senha deve conter pelo menos um símbolo especial");
                    ModelState.AddModelError("AlterarSenha.NovaSenha", "Nova senha deve conter pelo menos um símbolo especial (!@$%^&*)");
                }
            }

            // 3. Validar confirmação de senha
            if (!string.IsNullOrWhiteSpace(AlterarSenha.NovaSenha) && !string.IsNullOrWhiteSpace(AlterarSenha.ConfirmarSenha))
            {
                if (AlterarSenha.NovaSenha != AlterarSenha.ConfirmarSenha)
                {
                    errors.Add("Nova senha e confirmação não coincidem");
                    ModelState.AddModelError("AlterarSenha.ConfirmarSenha", "A confirmação deve ser igual à nova senha");
                }
            }

            // Log dos erros de validação básica
            if (errors.Any())
            {
                _logger.LogWarning("Erros de validação básica: {Errors}", string.Join(", ", errors));
                ErrorMessage = $"Erros encontrados: {string.Join("; ", errors)}";
                if (User.IsInRole("Admin"))
                {
                    await CarregarUsuariosAsync();
                }
                return Page();
            }

            try
            {
                // 4. Verificar usuário existe
                if (usuarioId == 0)
                {
                    _logger.LogError("Usuário não identificado (ID = 0)");
                    ErrorMessage = "Erro: Usuário não identificado. Faça login novamente.";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                var usuario = await _context.Usuarios.FindAsync(usuarioId);
                if (usuario == null)
                {
                    _logger.LogError("Usuário ID {UserId} não encontrado no banco", usuarioId);
                    ErrorMessage = "Erro: Usuário não encontrado no sistema.";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                _logger.LogInformation("Usuário encontrado: {Nome} (ID: {Id})", usuario.Nome, usuario.Id);

                // CORREÇÃO: Tratamento seguro do hash
                if (!string.IsNullOrEmpty(usuario.SenhaHash))
                {
                    var hashLength = usuario.SenhaHash.Length;
                    var hashPreview = hashLength > 20 ? usuario.SenhaHash.Substring(0, 20) + "..." : usuario.SenhaHash;
                    _logger.LogInformation("Hash atual no banco: {Hash} (Length: {Length})", hashPreview, hashLength);
                }
                else
                {
                    _logger.LogWarning("Hash de senha está vazio para usuário ID {UserId}", usuarioId);
                }

                // 5. Validar senha atual - CRÍTICO
                _logger.LogInformation("Validando senha atual...");

                bool senhaAtualValida = await ValidarSenhaAsync(AlterarSenha.SenhaAtual, usuario.SenhaHash);

                _logger.LogInformation("Resultado validação senha atual: {IsValid}", senhaAtualValida);

                if (!senhaAtualValida)
                {
                    _logger.LogWarning("❌ Senha atual incorreta para usuário ID {UserId}", usuarioId);
                    ErrorMessage = "Senha atual incorreta. Verifique e tente novamente.";
                    ModelState.AddModelError("AlterarSenha.SenhaAtual", "Senha atual incorreta");
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // 6. Verificar se nova senha é diferente da atual
                _logger.LogInformation("Verificando se nova senha é diferente da atual...");

                bool novaSenhaIgualAtual = await ValidarSenhaAsync(AlterarSenha.NovaSenha, usuario.SenhaHash);

                if (novaSenhaIgualAtual)
                {
                    _logger.LogWarning("❌ Nova senha é igual à senha atual para usuário ID {UserId}", usuarioId);
                    ErrorMessage = "A nova senha deve ser diferente da senha atual.";
                    ModelState.AddModelError("AlterarSenha.NovaSenha", "A nova senha deve ser diferente da senha atual");
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // 7. Gerar novo hash e atualizar
                _logger.LogInformation("Gerando novo hash BCrypt...");

                var novoHash = BCrypt.Net.BCrypt.HashPassword(AlterarSenha.NovaSenha, 12);

                // CORREÇÃO: Tratamento seguro do novo hash
                var novoHashLength = novoHash.Length;
                var novoHashPreview = novoHashLength > 20 ? novoHash.Substring(0, 20) + "..." : novoHash;
                _logger.LogInformation("Novo hash gerado: {Hash} (Length: {Length})", novoHashPreview, novoHashLength);

                // Atualizar no banco
                usuario.SenhaHash = novoHash;
                usuario.AtualizadoEm = DateTime.UtcNow;

                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Senha alterada com sucesso para usuário ID {UserId}", usuarioId);

                TempData["SuccessMessage"] = "Senha alterada com sucesso!";

                return RedirectToPage("Configurar", new { tab = "senha" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao alterar senha para usuário ID {UserId}", usuarioId);
                ErrorMessage = $"Erro interno ao alterar senha: {ex.Message}";
                if (User.IsInRole("Admin"))
                {
                    await CarregarUsuariosAsync();
                }
                return Page();
            }
        }


        public async Task<IActionResult> OnPostCriarUsuarioAsync()
        {
            // VERIFICAÇÃO DE SEGURANÇA: Apenas Admin/Técnico podem criar usuários
            if (!User.IsInRole("Admin") && !User.IsInRole("Tecnico"))
            {
                return Forbid();
            }

            // Manter aba de usuários ativa
            ActiveTab = "pane-usuarios";

            _logger.LogInformation("=== TENTATIVA DE CRIAR USUÁRIO ===");
            _logger.LogInformation("Nome: {Nome}, Sobrenome: {Sobrenome}, Email: {Email}",
                CriarUsuario.Nome, CriarUsuario.Sobrenome, CriarUsuario.Email);

            // Limpar erros do formulário de senha
            ModelState.Remove("AlterarSenha.SenhaAtual");
            ModelState.Remove("AlterarSenha.NovaSenha");
            ModelState.Remove("AlterarSenha.ConfirmarSenha");

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Por favor, corrija os erros no formulário.";
                if (User.IsInRole("Admin"))
                {
                    await CarregarUsuariosAsync();
                }
                return Page();
            }

            try
            {
                // Validar telefone se fornecido
                if (!string.IsNullOrWhiteSpace(CriarUsuario.Telefone) && !ValidarTelefone(CriarUsuario.Telefone))
                {
                    ErrorMessage = "Formato de telefone inválido. Use: (99) 99999-9999";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // ===== VALIDAÇÃO 1: Email já existe =====
                var emailExiste = await _context.Usuarios
                    .AnyAsync(u => u.Email.ToLower() == CriarUsuario.Email.ToLower());

                if (emailExiste)
                {
                    ErrorMessage = "Já existe um usuário cadastrado com este e-mail.";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // ===== VALIDAÇÃO 2: Nome e sobrenome não têm números =====
                if (ContemNumeros(CriarUsuario.Nome) || ContemNumeros(CriarUsuario.Sobrenome))
                {
                    ErrorMessage = "Nome e sobrenome não podem conter números.";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // ===== VALIDAÇÃO 3: Nome e sobrenome únicos =====
                var nomeCompleto = $"{CriarUsuario.Nome.Trim()} {CriarUsuario.Sobrenome.Trim()}";
                var nomeExiste = await _context.Usuarios
                    .AnyAsync(u => u.Nome.ToLower() == nomeCompleto.ToLower());

                if (nomeExiste)
                {
                    ErrorMessage = "Já existe um usuário com este nome completo.";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // ===== VALIDAÇÃO 4: Senha forte =====
                if (!ValidarSenhaForte(CriarUsuario.Senha))
                {
                    ErrorMessage = "A senha deve ter no mínimo 8 caracteres e pelo menos um símbolo (!@@#$%^&*).";
                    if (User.IsInRole("Admin"))
                    {
                        await CarregarUsuariosAsync();
                    }
                    return Page();
                }

                // ===== CRIAR USUÁRIO =====
                var novoUsuario = new Usuario
                {
                    Nome = nomeCompleto,
                    Email = CriarUsuario.Email.ToLower().Trim(),
                    SenhaHash = BCrypt.Net.BCrypt.HashPassword(CriarUsuario.Senha, 12),
                    Telefone = LimparTelefone(CriarUsuario.Telefone),
                    Departamento = string.IsNullOrWhiteSpace(CriarUsuario.Departamento) ? null : CriarUsuario.Departamento.Trim(),
                    Cargo = string.IsNullOrWhiteSpace(CriarUsuario.Cargo) ? null : CriarUsuario.Cargo.Trim(),
                    TipoUsuario = CriarUsuario.TipoUsuario,
                    Ativo = true,
                    CriadoEm = DateTime.UtcNow,
                    AtualizadoEm = DateTime.UtcNow
                };

                _context.Usuarios.Add(novoUsuario);
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Usuário criado com sucesso: ID {UserId}, Nome: {Nome}",
                    novoUsuario.Id, novoUsuario.Nome);

                TempData["SuccessMessage"] = $"Usuário '{nomeCompleto}' foi criado com sucesso!";

                return RedirectToPage("Configurar", new { tab = "usuarios" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao criar usuário");
                ErrorMessage = "Erro interno ao criar usuário. Tente novamente.";
                if (User.IsInRole("Admin"))
                {
                    await CarregarUsuariosAsync();
                }
                return Page();
            }
        }

        public async Task<IActionResult> OnPostEditarUsuarioAsync()
        {
            // VERIFICAÇÃO DE SEGURANÇA: Apenas Admin pode editar
            if (!User.IsInRole("Admin"))
            {
                return Forbid();
            }

            ActiveTab = "pane-usuarios";

            _logger.LogInformation("=== TENTATIVA DE EDITAR USUÁRIO ID {UserId} ===", EditarUsuario.Id);

            // Limpar erros do formulário de senha
            ModelState.Remove("AlterarSenha.SenhaAtual");
            ModelState.Remove("AlterarSenha.NovaSenha");
            ModelState.Remove("AlterarSenha.ConfirmarSenha");

            // DEBUG: Log dos dados recebidos
            _logger.LogInformation("Dados recebidos - ID: {Id}, Nome: '{Nome}', Sobrenome: '{Sobrenome}', Email: '{Email}', TipoUsuario: '{TipoUsuario}'",
                EditarUsuario.Id, EditarUsuario.Nome ?? "NULL", EditarUsuario.Sobrenome ?? "NULL",
                EditarUsuario.Email ?? "NULL", EditarUsuario.TipoUsuario ?? "NULL");

            _logger.LogInformation("Dados opcionais - Telefone: '{Telefone}', Departamento: '{Departamento}', Cargo: '{Cargo}', NovaSenha: '{HasPassword}'",
                EditarUsuario.Telefone ?? "NULL", EditarUsuario.Departamento ?? "NULL",
                EditarUsuario.Cargo ?? "NULL", string.IsNullOrEmpty(EditarUsuario.NovaSenha) ? "NÃO" : "SIM");

            // ===== LIMPAR MODELSTATE COMPLETAMENTE E REVALIDAR APENAS EditarUsuario =====
            ModelState.Clear();

            // Validar manualmente apenas os campos obrigatórios do EditarUsuario
            if (string.IsNullOrWhiteSpace(EditarUsuario.Nome))
            {
                ModelState.AddModelError("EditarUsuario.Nome", "Nome é obrigatório");
            }
            else if (EditarUsuario.Nome.Length < 2 || EditarUsuario.Nome.Length > 50)
            {
                ModelState.AddModelError("EditarUsuario.Nome", "Nome deve ter entre 2 e 50 caracteres");
            }

            if (string.IsNullOrWhiteSpace(EditarUsuario.Sobrenome))
            {
                ModelState.AddModelError("EditarUsuario.Sobrenome", "Sobrenome é obrigatório");
            }
            else if (EditarUsuario.Sobrenome.Length < 2 || EditarUsuario.Sobrenome.Length > 50)
            {
                ModelState.AddModelError("EditarUsuario.Sobrenome", "Sobrenome deve ter entre 2 e 50 caracteres");
            }

            if (string.IsNullOrWhiteSpace(EditarUsuario.Email))
            {
                ModelState.AddModelError("EditarUsuario.Email", "E-mail é obrigatório");
            }
            else if (!IsValidEmail(EditarUsuario.Email))
            {
                ModelState.AddModelError("EditarUsuario.Email", "E-mail inválido");
            }

            if (string.IsNullOrWhiteSpace(EditarUsuario.TipoUsuario))
            {
                ModelState.AddModelError("EditarUsuario.TipoUsuario", "Tipo de usuário é obrigatório");
            }

            // Validar telefone se fornecido
            if (!string.IsNullOrWhiteSpace(EditarUsuario.Telefone) && !ValidarTelefone(EditarUsuario.Telefone))
            {
                ModelState.AddModelError("EditarUsuario.Telefone", "Formato de telefone inválido. Use: (99) 99999-9999");
            }

            // Validar nova senha se fornecida
            if (!string.IsNullOrWhiteSpace(EditarUsuario.NovaSenha))
            {
                if (EditarUsuario.NovaSenha.Length < 8 || !ValidarSenhaForte(EditarUsuario.NovaSenha))
                {
                    ModelState.AddModelError("EditarUsuario.NovaSenha", "A nova senha deve ter no mínimo 8 caracteres e pelo menos um símbolo");
                }
            }

            // DEBUG: Verificar erros de validação APÓS limpeza
            if (!ModelState.IsValid)
            {
                _logger.LogWarning("❌ ModelState inválido após revalidação. Erros encontrados:");
                foreach (var modelError in ModelState)
                {
                    var key = modelError.Key;
                    var errors = modelError.Value.Errors;
                    foreach (var error in errors)
                    {
                        _logger.LogWarning("Campo: {Field} | Erro: {Error}", key, error.ErrorMessage);
                    }
                }

                ErrorMessage = "Por favor, corrija os erros no formulário de edição.";
                await CarregarUsuariosAsync();
                return Page();
            }

            try
            {
                _logger.LogInformation("✅ ModelState válido. Buscando usuário ID {UserId}...", EditarUsuario.Id);

                var usuario = await _context.Usuarios.FindAsync(EditarUsuario.Id);
                if (usuario == null)
                {
                    _logger.LogWarning("❌ Usuário ID {UserId} não encontrado no banco", EditarUsuario.Id);
                    ErrorMessage = "Usuário não encontrado.";
                    await CarregarUsuariosAsync();
                    return Page();
                }

                _logger.LogInformation("✅ Usuário encontrado: {Nome} ({Email})", usuario.Nome, usuario.Email);

                // Validar email único (exceto o próprio usuário)
                var emailExiste = await _context.Usuarios
                    .AnyAsync(u => u.Email.ToLower() == EditarUsuario.Email.ToLower() && u.Id != EditarUsuario.Id);

                if (emailExiste)
                {
                    _logger.LogWarning("❌ Email {Email} já existe para outro usuário", EditarUsuario.Email);
                    ErrorMessage = "Já existe outro usuário com este e-mail.";
                    await CarregarUsuariosAsync();
                    return Page();
                }

                // Atualizar dados do usuário
                var nomeCompleto = $"{EditarUsuario.Nome.Trim()} {EditarUsuario.Sobrenome.Trim()}";
                _logger.LogInformation("Atualizando usuário. Nome completo: '{NomeCompleto}'", nomeCompleto);

                usuario.Nome = nomeCompleto;
                usuario.Email = EditarUsuario.Email.ToLower().Trim();
                usuario.Telefone = LimparTelefone(EditarUsuario.Telefone);
                usuario.Departamento = string.IsNullOrWhiteSpace(EditarUsuario.Departamento) ? null : EditarUsuario.Departamento.Trim();
                usuario.Cargo = string.IsNullOrWhiteSpace(EditarUsuario.Cargo) ? null : EditarUsuario.Cargo.Trim();
                usuario.TipoUsuario = EditarUsuario.TipoUsuario;
                usuario.AtualizadoEm = DateTime.UtcNow;

                // Atualizar senha se fornecida
                if (!string.IsNullOrWhiteSpace(EditarUsuario.NovaSenha))
                {
                    _logger.LogInformation("Nova senha fornecida. Atualizando...");
                    usuario.SenhaHash = BCrypt.Net.BCrypt.HashPassword(EditarUsuario.NovaSenha, 12);
                    _logger.LogInformation("✅ Senha atualizada com sucesso");
                }

                _logger.LogInformation("Salvando alterações no banco de dados...");
                await _context.SaveChangesAsync();

                _logger.LogInformation("✅ Usuário editado com sucesso: ID {UserId}, Nome: {Nome}",
                    usuario.Id, usuario.Nome);

                TempData["SuccessMessage"] = $"Usuário '{nomeCompleto}' foi atualizado com sucesso!";

                return RedirectToPage("Configurar", new { tab = "usuarios" });
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "❌ Erro ao editar usuário ID {UserId}", EditarUsuario.Id);
                ErrorMessage = "Erro interno ao editar usuário. Tente novamente.";
                await CarregarUsuariosAsync();
                return Page();
            }
        }

        // MÉTODOS AUXILIARES
        private int GetCurrentUserId()
        {
            var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
            return int.TryParse(userIdClaim, out var userId) ? userId : 0;
        }

        private async Task CarregarUsuariosAsync()
        {
            Usuarios = await _context.Usuarios
                .OrderBy(u => u.Nome)
                .ToListAsync();
        }

        private bool ContemNumeros(string texto)
        {
            return !string.IsNullOrWhiteSpace(texto) && texto.Any(char.IsDigit);
        }

        private bool ValidarSenhaForte(string senha)
        {
            if (string.IsNullOrWhiteSpace(senha) || senha.Length < 8)
                return false;

            // Verificar se tem pelo menos um símbolo
            var simbolos = "!@#$%^&*()_+-=[]{}|;:,.<>?";
            return senha.Any(c => simbolos.Contains(c));
        }

        // Método ValidarSenhaAsync melhorado com mais logs
        private async Task<bool> ValidarSenhaAsync(string senhaInput, string senhaHash)
        {
            return await Task.Run(() =>
            {
                try
                {
                    _logger.LogInformation("Validando senha - Entrada: {Length} chars, Hash: {HashPrefix}...",
                        senhaInput?.Length ?? 0, senhaHash?.Substring(0, Math.Min(10, senhaHash?.Length ?? 0)));

                    if (string.IsNullOrWhiteSpace(senhaInput))
                    {
                        _logger.LogWarning("Senha de entrada está vazia");
                        return false;
                    }

                    if (string.IsNullOrWhiteSpace(senhaHash))
                    {
                        _logger.LogWarning("Hash armazenado está vazio");
                        return false;
                    }

                    // Verificar se é hash BCrypt
                    if (senhaHash.StartsWith("$2a$") || senhaHash.StartsWith("$2b$") || senhaHash.StartsWith("$2y$"))
                    {
                        _logger.LogInformation("Usando validação BCrypt");
                        var resultado = BCrypt.Net.BCrypt.Verify(senhaInput, senhaHash);
                        _logger.LogInformation("Resultado BCrypt: {Resultado}", resultado);
                        return resultado;
                    }
                    else
                    {
                        _logger.LogWarning("Usando validação texto puro (não recomendado)");
                        var resultado = senhaInput == senhaHash;
                        _logger.LogInformation("Resultado texto puro: {Resultado}", resultado);
                        return resultado;
                    }
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro na validação de senha");
                    return false;
                }
            });
        }

        private bool ValidarTelefone(string telefone)
        {
            if (string.IsNullOrWhiteSpace(telefone))
                return true;

            var regex = new Regex(@"^\(\d{2}\)\s\d{5}-\d{4}$");
            return regex.IsMatch(telefone);
        }

        private string? LimparTelefone(string? telefone)
        {
            return string.IsNullOrWhiteSpace(telefone) ? null : telefone.Trim();
        }

        // Método auxiliar para validar email
        private bool IsValidEmail(string email)
        {
            if (string.IsNullOrWhiteSpace(email))
                return false;

            try
            {
                var addr = new System.Net.Mail.MailAddress(email);
                return addr.Address == email;
            }
            catch
            {
                return false;
            }
        }

        // CLASSES DE INPUT
        public class AlterarSenhaInput
        {
            [Required(ErrorMessage = "Senha atual é obrigatória")]
            [Display(Name = "Senha Atual")]
            public string SenhaAtual { get; set; } = string.Empty;

            [Required(ErrorMessage = "Nova senha é obrigatória")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Nova senha deve ter no mínimo 8 caracteres")]
            [Display(Name = "Nova Senha")]
            public string NovaSenha { get; set; } = string.Empty;

            [Required(ErrorMessage = "Confirmação de senha é obrigatória")]
            [Compare("NovaSenha", ErrorMessage = "A confirmação deve ser igual à nova senha")]
            [Display(Name = "Confirmar Nova Senha")]
            public string ConfirmarSenha { get; set; } = string.Empty;
        }

        public class CriarUsuarioInput
        {
            [Required(ErrorMessage = "Nome é obrigatório")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 50 caracteres")]
            [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Nome deve conter apenas letras")]
            public string Nome { get; set; } = string.Empty;

            [Required(ErrorMessage = "Sobrenome é obrigatório")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Sobrenome deve ter entre 2 e 50 caracteres")]
            [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Sobrenome deve conter apenas letras")]
            public string Sobrenome { get; set; } = string.Empty;

            [Required(ErrorMessage = "E-mail é obrigatório")]
            [EmailAddress(ErrorMessage = "E-mail inválido")]
            [StringLength(150, ErrorMessage = "E-mail muito longo")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Senha é obrigatória")]
            [StringLength(100, MinimumLength = 8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres")]
            public string Senha { get; set; } = string.Empty;

            [StringLength(20, ErrorMessage = "Telefone muito longo")]
            [RegularExpression(@"^\(\d{2}\)\s\d{5}-\d{4}$", ErrorMessage = "Formato inválido. Use: (99) 99999-9999")]
            public string? Telefone { get; set; }

            [StringLength(50, ErrorMessage = "Departamento muito longo")]
            public string? Departamento { get; set; }

            [StringLength(50, ErrorMessage = "Cargo muito longo")]
            public string? Cargo { get; set; }

            [Required(ErrorMessage = "Tipo de usuário é obrigatório")]
            public string TipoUsuario { get; set; } = "Usuario";
        }

        public class EditarUsuarioInput
        {
            public int Id { get; set; }

            [Required(ErrorMessage = "Nome é obrigatório")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Nome deve ter entre 2 e 50 caracteres")]
            [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Nome deve conter apenas letras")]
            public string Nome { get; set; } = string.Empty;

            [Required(ErrorMessage = "Sobrenome é obrigatório")]
            [StringLength(50, MinimumLength = 2, ErrorMessage = "Sobrenome deve ter entre 2 e 50 caracteres")]
            [RegularExpression(@"^[a-zA-ZÀ-ÿ\s]+$", ErrorMessage = "Sobrenome deve conter apenas letras")]
            public string Sobrenome { get; set; } = string.Empty;

            [Required(ErrorMessage = "E-mail é obrigatório")]
            [EmailAddress(ErrorMessage = "E-mail inválido")]
            [StringLength(150, ErrorMessage = "E-mail muito longo")]
            public string Email { get; set; } = string.Empty;

            [StringLength(20, ErrorMessage = "Telefone muito longo")]
            public string? Telefone { get; set; }

            [StringLength(50, ErrorMessage = "Departamento muito longo")]
            public string? Departamento { get; set; }

            [StringLength(50, ErrorMessage = "Cargo muito longo")]
            public string? Cargo { get; set; }

            [Required(ErrorMessage = "Tipo de usuário é obrigatório")]
            public string TipoUsuario { get; set; } = "Usuario";

            [StringLength(100, MinimumLength = 8, ErrorMessage = "Senha deve ter no mínimo 8 caracteres")]
            public string? NovaSenha { get; set; }
        }
    }
}
