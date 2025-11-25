using System.Security.Claims;
using Microsoft.AspNetCore.Mvc;
using SistemaChamados.Data;
using SistemaChamados.Models;
using Microsoft.AspNetCore.Authorization;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Authorize]
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
            List<Ticket> tickets = _context.Tickets.Where(p => p.SolicitanteId == userId).ToList(); ;

            if (tickets == null) {
                return NotFound();
            }

            return Ok(tickets);
        } 
    }
}
