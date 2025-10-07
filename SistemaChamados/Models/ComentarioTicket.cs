using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Models
{
    [Table("comentarios_ticket")]
    public class ComentarioTicket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Required]
        [Column("usuario_id")]
        public int UsuarioId { get; set; }

        [Required]
        [Column("comentario")]
        public string Comentario { get; set; } = string.Empty;

        [Column("tipo")]
        [StringLength(20)]
        public TipoComentario Tipo { get; set; } = TipoComentario.Comentario;

        [Column("visivel_solicitante")]
        public bool VisivelSolicitante { get; set; } = true;

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        // Propriedades de navegação (relacionamentos)
        [ForeignKey(nameof(TicketId))]
        public virtual Ticket? Ticket { get; set; }

        [ForeignKey(nameof(UsuarioId))]
        public virtual Usuario? Usuario { get; set; }
    }

    // Enum para o tipo de comentário
    //public enum TipoComentario
    //{
    //    Comentario = 1,        // Comentário normal do usuário
    //    MudancaStatus = 2,     // Mudança de status do ticket  
    //    Atribuicao = 3         // Atribuição de responsável
    //}
}
