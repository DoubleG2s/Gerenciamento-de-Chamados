using Microsoft.AspNetCore.Authentication;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;

namespace SistemaChamados.Pages.Autentication
{
    public class LogoutModel : PageModel
    {
        private readonly ILogger<LogoutModel> _logger;

        public LogoutModel(ILogger<LogoutModel> logger)
        {
            _logger = logger;
        }

        public async Task<IActionResult> OnGetAsync()
        {
            return await OnPostAsync();
        }

        public async Task<IActionResult> OnPostAsync()
        {
            var userName = User.Identity?.Name ?? "Desconhecido";

            _logger.LogInformation("Logout iniciado para usu�rio: {UserName}", userName);

            // Limpar cookie de autentica��o
            await HttpContext.SignOutAsync(CookieAuthenticationDefaults.AuthenticationScheme);

            // Limpar todos os cookies (garantir limpeza completa)
            foreach (var cookie in Request.Cookies.Keys)
            {
                Response.Cookies.Delete(cookie);
            }

            // Limpar cache do navegador
            Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
            Response.Headers["Pragma"] = "no-cache";
            Response.Headers["Expires"] = "0";

            _logger.LogInformation("Logout conclu�do para usu�rio: {UserName}", userName);

            // Redirecionar para p�gina de login
            return RedirectToPage("/Autentication/Login");
        }
    }
}
