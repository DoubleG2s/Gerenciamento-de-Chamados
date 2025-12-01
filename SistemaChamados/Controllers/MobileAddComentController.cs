using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using SistemaChamados.Models.dto;

namespace SistemaChamados.Controllers
{


    [Route("api/tickets")]
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    public class TicketsController : ControllerBase
    {
        private readonly AppDbContext _db;

        public TicketsController(AppDbContext db)
        {
            _db = db;
        }

        // POST: tickets/{id}/comentarios
        [HttpPost("{id}/comentarios")]
        public async Task<IActionResult> AdicionarComentario(
            int id,
            [FromBody] AdicionarComentarioRequest dto)
        {
            if (string.IsNullOrWhiteSpace(dto.Comentario))
                return BadRequest("O comentário não pode estar vazio.");

            // 🔐 Busca o ID do usuário logado
            var claim = User.FindFirst(ClaimTypes.NameIdentifier);
            if (claim == null)
                return Unauthorized("Claim 'NameIdentifier' não encontrada.");

            int usuarioId = int.Parse(claim.Value);
            

            var ticket = await _db.Tickets
                .Include(t => t.Comentarios)
                .FirstOrDefaultAsync(t => t.Id == id);

            if (ticket == null)
                return NotFound("Ticket não encontrado.");

            var novoComentario = new ComentarioTicket
            {
                TicketId = id,
                UsuarioId = usuarioId,
                Comentario = dto.Comentario,
                Tipo = Enums.TipoComentario.Comentario,
                VisivelSolicitante = true,
                CriadoEm = DateTime.UtcNow
            };

            _db.ComentariosTicket.Add(novoComentario);
            await _db.SaveChangesAsync();

            // busca o nome/username do autor
            var usuario = await _db.Usuarios
                .FirstOrDefaultAsync(u => u.Id == usuarioId);

            var response = new ComentarioResponse
            {
                Id = novoComentario.Id,
                Mensagem = novoComentario.Comentario,
                Autor = usuario?.Nome ?? "Usuário",
                Data = novoComentario.CriadoEm.ToString("yyyy-MM-dd HH:mm:ss"),
            };

            return Ok(response);
        }
    }
}