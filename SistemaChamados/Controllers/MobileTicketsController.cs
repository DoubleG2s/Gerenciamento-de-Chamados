using Microsoft.AspNetCore.Mvc;

namespace SistemaChamados.Controllers
{
    [ApiController]
    [Route("api/mobile/auth")]
    public class MobileTicketsController : Controller
    {
        public IActionResult Index()
        {
            return View();
        }
    }
}
