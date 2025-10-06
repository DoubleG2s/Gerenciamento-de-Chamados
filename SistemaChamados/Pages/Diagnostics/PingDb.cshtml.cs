using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;            // <- necessário para Database.CanConnectAsync
using SistemaChamados.Data;

namespace SistemaChamados.Pages.Diagnostics
{
    //autorização
    [Authorize(Roles = "Admin")]
    public class PingDbModel : PageModel
    {
        private readonly AppDbContext _db;
        public PingDbModel(AppDbContext db) => _db = db;

        public bool Connected { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGet()
        {
            try
            {
                Connected = await _db.Database.CanConnectAsync();
            }
            catch (Exception ex)
            {
                Connected = false;
                Error = ex.Message;
            }
        }
    }
}
