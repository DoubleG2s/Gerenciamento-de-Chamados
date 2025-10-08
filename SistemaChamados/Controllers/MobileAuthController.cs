/*using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Route("api/mobile/auth")]
    public class MobileAuthController : ControllerBase
    {
        private readonly AppDbContext _context;
        private readonly PasswordHasher<Usuario> _hasher = new();

        public MobileAuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Ativo);

            if (usuario == null)
                return Ok(new { sucesso = false, mensagem = "Usuário não encontrado." });

            var result = _hasher.VerifyHashedPassword(usuario, usuario.SenhaHash, dto.Senha);
            if (result != PasswordVerificationResult.Success)
                return Ok(new { sucesso = false, mensagem = "Senha incorreta." });

            return Ok(new
            {
                sucesso = true,
                usuario = new
                {
                    usuario.Id,
                    usuario.Nome,
                    usuario.Email,
                    usuario.TipoUsuario
                }
            });
        }
    }
}*/

using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Route("api/mobile/auth")]
    public class MobileAuthController : ControllerBase
    {
        private readonly AppDbContext _context;

        public MobileAuthController(AppDbContext context)
        {
            _context = context;
        }

        [HttpPost("login")]
        public async Task<IActionResult> Login([FromBody] LoginDto dto)
        {
            var usuario = await _context.Usuarios
                .FirstOrDefaultAsync(u => u.Email == dto.Email && u.Ativo);

            if (usuario == null)
                return Ok(new { sucesso = false, mensagem = "Usuário não encontrado." });

            // 🔹 Comparação direta (sem hash)
            if (usuario.SenhaHash != dto.Senha)
                return Ok(new { sucesso = false, mensagem = "Senha incorreta." });

            return Ok(new
            {
                sucesso = true,
                usuario = new
                {
                    usuario.Id,
                    usuario.Nome,
                    usuario.Email,
                    usuario.TipoUsuario
                }
            });
        }
    }
}