using System.ComponentModel.DataAnnotations;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Models
{
    public class Prioridade
    {
        public int Id { get; set; }
        public PriorityLevel Nivel { get; set; }
        [Required, StringLength(30)]
        public string Nome { get; set; }
    }
}
