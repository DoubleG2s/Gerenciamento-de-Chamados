namespace SistemaChamados.Models.dto;

public class TicketDetalhesResponse
{
    public int Id { get; set; }
    public string Titulo { get; set; }
    public string Descricao { get; set; }
    public string Status { get; set; }
    public int Prioridade { get; set; }
    public DateTime CriadoEm { get; set; }
    public DateTime AtualizadoEm { get; set; }
    public List<ComentarioResponse> Comentarios { get; set; }
}