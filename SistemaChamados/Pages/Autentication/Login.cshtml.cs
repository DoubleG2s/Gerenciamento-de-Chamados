using System.ComponentModel.DataAnnotations;
using System.Security.Claims;
using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;

namespace SistemaChamados.Pages.Autentication
{
    public class LoginModel : PageModel
    {
        private readonly AppDbContext _db;
        private readonly ILogger<LoginModel> _logger;

        public LoginModel(AppDbContext db, ILogger<LoginModel> logger)
        {
            _db = db;
            _logger = logger;
        }

        [BindProperty]
        public LoginInput Input { get; set; } = new();

        public string? ErrorMessage { get; set; }

        public void OnGet(string? returnUrl = null)
        {
            // Log para debug
            _logger.LogInformation("Página de login acessada");
        }

        public async Task<IActionResult> OnPostAsync(string? returnUrl = null)
        {
            _logger.LogInformation("Tentativa de login para: {Email}", Input.Email);

            if (!ModelState.IsValid)
            {
                ErrorMessage = "Informe e-mail e senha válidos.";
                return Page();
            }

            try
            {
                // Busca usuário ativo pelo e-mail (case-sensitive)
                var user = await _db.Usuarios
                    .AsNoTracking()
                    .FirstOrDefaultAsync(u =>
                        u.Email == Input.Email &&
                        u.Ativo);

                if (user == null)
                {
                    _logger.LogWarning("Usuário não encontrado ou inativo: {Email}", Input.Email);
                    ErrorMessage = "E-mail ou senha inválidos.";
                    return Page();
                }

                _logger.LogInformation("Usuário encontrado: {UserId} - {UserName} - Tipo: {UserType}",
                    user.Id, user.Nome, user.TipoUsuario);

                // Validação de senha melhorada
                bool senhaOk = await ValidarSenhaAsync(Input.Senha, user.SenhaHash);

                if (!senhaOk)
                {
                    _logger.LogWarning("Senha incorreta para usuário: {Email}", Input.Email);
                    ErrorMessage = "E-mail ou senha inválidos.";
                    return Page();
                }

                // Login bem-sucedido
                _logger.LogInformation("Login bem-sucedido para: {Email} - Tipo: {UserType}", Input.Email, user.TipoUsuario);

                // Monta as claims
                var claims = new List<Claim>
                {
                    new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
                    new Claim(ClaimTypes.Name, !string.IsNullOrWhiteSpace(user.Nome) ? user.Nome : user.Email),
                    new Claim(ClaimTypes.Email, user.Email),
                    new Claim(ClaimTypes.Role, !string.IsNullOrWhiteSpace(user.TipoUsuario) ? user.TipoUsuario : "Usuario"),
                    new Claim("TipoUsuario", user.TipoUsuario ?? "Usuario")
                };

                var identity = new ClaimsIdentity(claims, CookieAuthenticationDefaults.AuthenticationScheme);
                var principal = new ClaimsPrincipal(identity);

                var props = new AuthenticationProperties
                {
                    IsPersistent = Input.Lembrar,
                    ExpiresUtc = DateTimeOffset.UtcNow.AddHours(8)
                };

                await HttpContext.SignInAsync(CookieAuthenticationDefaults.AuthenticationScheme, principal, props);

                // REDIRECIONA PÓS-LOGIN BASEADO NO TIPO DE USUÁRIO
                if (!string.IsNullOrWhiteSpace(returnUrl) && Url.IsLocalUrl(returnUrl))
                {
                    _logger.LogInformation("Redirecionando para returnUrl: {ReturnUrl}", returnUrl);
                    return Redirect(returnUrl);
                }

                // Redirecionar baseado no tipo de usuário
                var tipoUsuario = user.TipoUsuario ?? "Usuario";
                string redirectPage;

                switch (tipoUsuario.ToUpper())
                {
                    case "ADMIN":
                    case "TECNICO":
                        redirectPage = "/Index"; // Dashboard para Admin/Técnico
                        break;
                    case "USUARIO":
                    default:
                        redirectPage = "/Tickets/UsuarioTickets"; // Página específica para usuários comuns
                        break;
                }

                _logger.LogInformation("Redirecionando usuário {UserType} para: {RedirectPage}", tipoUsuario, redirectPage);
                return RedirectToPage(redirectPage);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Erro durante tentativa de login para: {Email}", Input.Email);
                ErrorMessage = "Erro interno. Tente novamente.";
                return Page();
            }
        }

        private async Task<bool> ValidarSenhaAsync(string senhaInput, string senhaHash)
        {
            return await Task.Run(() =>
            {
                try
                {
                    // Verifica se é um hash BCrypt válido
                    if (!string.IsNullOrWhiteSpace(senhaHash))
                    {
                        // Tenta validar como BCrypt primeiro
                        if (senhaHash.StartsWith("$2a$") || senhaHash.StartsWith("$2b$") || senhaHash.StartsWith("$2y$"))
                        {
                            _logger.LogDebug("Validando senha com BCrypt");
                            return BCrypt.Net.BCrypt.Verify(senhaInput, senhaHash);
                        }

                        // Fallback para senha em texto puro (apenas para desenvolvimento/migração)
                        _logger.LogWarning("Usando validação de senha em texto puro - considere migrar para BCrypt");
                        return senhaInput == senhaHash;
                    }

                    return false;
                }
                catch (Exception ex)
                {
                    _logger.LogError(ex, "Erro na validação de senha");
                    return false;
                }
            });
        }

        public class LoginInput
        {
            [Required(ErrorMessage = "E-mail é obrigatório")]
            [EmailAddress(ErrorMessage = "E-mail inválido")]
            [Display(Name = "E-mail")]
            public string Email { get; set; } = string.Empty;

            [Required(ErrorMessage = "Senha é obrigatória")]
            [Display(Name = "Senha")]
            public string Senha { get; set; } = string.Empty;

            [Display(Name = "Lembrar de mim")]
            public bool Lembrar { get; set; }
        }
    }
}
