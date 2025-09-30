using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaChamados.Models
{
    [Table("anexos")]
    public class Anexo
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required]
        [Column("ticket_id")]
        public int TicketId { get; set; }

        [Required]
        [Column("nome_arquivo")]
        [StringLength(255)]
        public string NomeArquivo { get; set; } = string.Empty;

        [Required]
        [Column("nome_original")]
        [StringLength(255)]
        public string NomeOriginal { get; set; } = string.Empty;

        [Required]
        [Column("tamanho_bytes")]
        public long TamanhoBytes { get; set; }

        [Required]
        [Column("tipo_conteudo")]
        [StringLength(100)]
        public string TipoConteudo { get; set; } = string.Empty;

        [Required]
        [Column("caminho_arquivo")]
        [StringLength(500)]
        public string CaminhoArquivo { get; set; } = string.Empty;

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [Required]
        [Column("criado_por")]
        public int CriadoPor { get; set; }

        // Propriedades de navegação (relacionamentos)
        [ForeignKey(nameof(TicketId))]
        public virtual Ticket? Ticket { get; set; }

        [ForeignKey(nameof(CriadoPor))]
        public virtual Usuario? UsuarioCriador { get; set; }
    }
}
