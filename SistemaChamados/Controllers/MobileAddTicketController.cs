using System;
using System.Security.Claims;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using SistemaChamados.Models.dto;
using static SistemaChamados.Models.Enums;

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
    
    [HttpPost]
    public async Task<IActionResult> CriarNovoChamado([FromBody] NovoChamadoRequest request)
    {

        if (!ModelState.IsValid)
        {
            return BadRequest(ModelState);
        }

        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier);
        if (userIdClaim == null || !int.TryParse(userIdClaim.Value, out int solicitanteId))
        {
            return Unauthorized(new { error = "Token JWT inválido ou sem identificador de usuário." });
        }
        

        int categoriaPadraoId = 1; 
        
        var novoTicket = new Ticket
        {
            Titulo = request.Titulo,
            Descricao = request.Descricao,

            SolicitanteId = solicitanteId,
            CategoriaId = categoriaPadraoId, 
 
            Status = TicketStatus.Aberto, 
            
            CriadoEm = DateTime.UtcNow, 
            TempoRespostaHoras = 24, 

            Prioridade = ParsePrioridade(request.Prioridade)
        };

        PriorityLevel ParsePrioridade(string prioridadeString)
        {
            if (Enum.TryParse<PriorityLevel>(prioridadeString, true, out var result)) 
            {
                return result;
            }
            
            return PriorityLevel.Baixa; 
        }
        
        _context.Tickets.Add(novoTicket);
        await _context.SaveChangesAsync();
        
        return StatusCode(201, novoTicket);
    }
}