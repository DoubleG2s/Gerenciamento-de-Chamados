using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http.HttpResults;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models.dto;

namespace SistemaChamados.Controllers;

[ApiController]
[Route("api/mobile/ticketdetalhe")]
public class MobileTicketController : ControllerBase
{
    private readonly AppDbContext _context;

    public MobileTicketController(AppDbContext context)
    {
        _context = context;
    }

    [Authorize]
    [HttpGet("{id}")]
    public async Task<ActionResult<TicketDetalhesResponse>> GetDetalhes(int id)
    {
        var ticket = await _context.Tickets
            .Include(t => t.Solicitante)
            .Include(t => t.Comentarios)
            .ThenInclude(c => c.Usuario)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (ticket == null)
            return NotFound();

        return new TicketDetalhesResponse
        {
            Id = ticket.Id,
            Titulo = ticket.Titulo,
            Descricao = ticket.Descricao,
            Status = ticket.Status.ToString(),
            Prioridade = (int)ticket.Prioridade,
            CriadoEm = ticket.CriadoEm,
            AtualizadoEm = ticket.AtualizadoEm,

            Comentarios = ticket.Comentarios
                .Select(c => new ComentarioResponse
                {
                    Autor = c.Usuario.Nome ?? "Desconhecido",
                    Data = c.CriadoEm.ToString("dd/MM/yyyy HH:mm") ?? "",
                    Mensagem = c.Comentario ?? ""
                })
                .ToList()
        };
    
    }
}

