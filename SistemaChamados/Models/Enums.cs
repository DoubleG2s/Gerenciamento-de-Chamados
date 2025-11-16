namespace SistemaChamados.Models
{
    public class Enums
    {
        public enum TicketStatus { Aberto = 1, Andamento = 2, Resolvido = 3, Fechado = 4 }
        public enum PriorityLevel { Baixa = 1, Media = 2, Alta = 3, Crítica = 4 }
        public enum TipoComentario { Comentario = 1, MudancaStatus = 2, Atribuicao = 3 }
    }
}
