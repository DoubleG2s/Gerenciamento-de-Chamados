using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace SistemaChamados.Models
{
    [Table("usuarios")] // tabela do PostgreSQL
    public class Usuario
    {
        [Key]
        [Column("id")]
        public int Id { get; set; }

        [Required, StringLength(100)]
        [Column("nome")]
        public string Nome { get; set; } = string.Empty;

        [Required, StringLength(150), EmailAddress]
        [Column("email")]
        public string Email { get; set; } = string.Empty;

        [Required, StringLength(255)]
        [Column("senha_hash")]
        public string SenhaHash { get; set; } = string.Empty;

        [StringLength(20)]
        [Column("telefone")]
        public string? Telefone { get; set; }

        [StringLength(50)]
        [Column("departamento")]
        public string? Departamento { get; set; }

        [StringLength(50)]
        [Column("cargo")]
        public string? Cargo { get; set; }

        // CHECK (tipo_usuario IN ('Admin','Tecnico','Usuario'))
        [StringLength(20)]
        [Column("tipo_usuario")]
        public string TipoUsuario { get; set; } = "Usuario";

        [Column("ativo")]
        public bool Ativo { get; set; } = true;

        [Column("criado_em")]
        public DateTime CriadoEm { get; set; } = DateTime.UtcNow;

        [Column("atualizado_em")]
        public DateTime AtualizadoEm { get; set; } = DateTime.UtcNow;

        // Mantém "Iniciais" (não existe no banco)
        [NotMapped, StringLength(2)]
        public string Iniciais
        {
            get
            {
                if (string.IsNullOrWhiteSpace(Nome)) return string.Empty;
                var partes = Nome.Trim().Split(' ', StringSplitOptions.RemoveEmptyEntries);
                if (partes.Length == 1) return char.ToUpperInvariant(partes[0][0]).ToString();

                var ignores = new[] { "da", "de", "do", "dos", "das", "e" };
                var first = partes[0][0];
                var lastWord = partes.Reverse().FirstOrDefault(p => !ignores.Contains(p.ToLowerInvariant())) ?? partes[^1];

                return $"{char.ToUpperInvariant(first)}{char.ToUpperInvariant(lastWord[0])}";
            }
        }
    }
}
