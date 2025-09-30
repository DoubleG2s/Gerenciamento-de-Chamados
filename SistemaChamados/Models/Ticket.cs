using static SistemaChamados.Models.Enums;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaChamados.Models
{
    [Table("tickets")]
    public class Ticket
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, StringLength(120)]
        [Column("titulo")]
        public string Titulo { get; set; } = string.Empty;

        [Required]
        [Column("descricao")]
        public string Descricao { get; set; } = string.Empty;

        [Display(Name = "Prioridade")]
        [Column("prioridade")]
        [StringLength(20)]
        public PriorityLevel Prioridade { get; set; } = PriorityLevel.Média;

        [Display(Name = "Status")]
        [Column("status")]
        [StringLength(20)]
        public TicketStatus Status { get; set; } = TicketStatus.Aberto;

        // Chaves estrangeiras
        [Display(Name = "Solicitante")]
        [Column("solicitante_id")]
        public int SolicitanteId { get; set; }

        [Display(Name = "Responsável")]
        [Column("responsavel_id")]
        public int? ResponsavelId { get; set; }

        [Display(Name = "Categoria")]
        [Column("categoria_id")]
        public int CategoriaId { get; set; }

        // Timestamps
        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [Column("atualizado_em")]
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

        [Column("resolvido_em")]
        public DateTime? ResolvidoEm { get; set; }

        [Column("fechado_em")]
        public DateTime? FechadoEm { get; set; }

        // SLA
        [Column("prazo_resolucao")]
        public DateTime? PrazoResolucao { get; set; }

        [Column("tempo_resposta_horas")]
        public int TempoRespostaHoras { get; set; } = 24;

        // Propriedades de navegação (relacionamentos)
        [ForeignKey(nameof(CategoriaId))]
        public virtual Categoria? Categoria { get; set; }

        [ForeignKey(nameof(SolicitanteId))]
        public virtual Usuario? Solicitante { get; set; }

        [ForeignKey(nameof(ResponsavelId))]
        public virtual Usuario? Responsavel { get; set; }

        // Relacionamentos um-para-muitos
        public virtual ICollection<Anexo> Anexos { get; set; } = new List<Anexo>();
        public virtual ICollection<ComentarioTicket> Comentarios { get; set; } = new List<ComentarioTicket>();
    }
}
