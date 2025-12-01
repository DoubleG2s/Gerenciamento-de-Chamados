using System.Security.Claims;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.AspNetCore.Mvc;
using SistemaChamados.Data;
using SistemaChamados.Models;
using Microsoft.AspNetCore.Authorization;
using SistemaChamados.Models.dto;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Authorize(AuthenticationSchemes = JwtBearerDefaults.AuthenticationScheme)]
    [Route("api/mobile/ticket")]
    public class MobileTicketsController : Controller
    {
        private readonly AppDbContext _context;

        public MobileTicketsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet]
        public ActionResult getTickets()
        {
            var userId = int.Parse(User.FindFirst(ClaimTypes.NameIdentifier)?.Value);
            List<TicketsResponse> tickets = _context.Tickets.Where(p => p.SolicitanteId == userId)
                .Select(t => new TicketsResponse
                {
                    Id = t.Id,
                    Titulo = t.Titulo,
                    Descricao = t.Descricao,
                    Prioridade = t.Prioridade.ToString(),
                    Status = t.Status.ToString(),
                    CriadoEm = t.CriadoEm,
                    AtualizadoEm = t.AtualizadoEm
                }).ToList();

            if (tickets == null) {
                return NotFound();
            }

            return Ok(tickets);
        } 
    }
}
