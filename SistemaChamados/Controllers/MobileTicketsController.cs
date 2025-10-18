using Microsoft.AspNetCore.Mvc;
using SistemaChamados.Data;
using SistemaChamados.Models;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Route("api/mobile/ticket")]
    public class MobileTicketsController : Controller
    {
        private readonly AppDbContext _context;

        public MobileTicketsController(AppDbContext context)
        {
            _context = context;
        }
        [HttpGet("{id}")]
        public ActionResult getTickets(int id)
        {
            List<Ticket> tickets = _context.Tickets.Where(p => p.SolicitanteId == id).ToList(); ;

            if (tickets == null) {
                return NotFound();
            }

            return Ok(tickets);
        } 
    }
}
