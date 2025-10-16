using Microsoft.AspNetCore.Mvc;
using SistemaChamados.Data;

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
            var tickets = _context.Tickets.FirstOrDefault(p => p.Id == id);

            if (tickets == null) {
                return NotFound();
            }

            return Ok(tickets);
        } 
    }
}
