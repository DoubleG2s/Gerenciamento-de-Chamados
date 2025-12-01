using System.ComponentModel.DataAnnotations;
// Remova qualquer construtor que você possa ter adicionado.

public class NovoChamadoRequest
{
    // Crie um construtor vazio (padrão) apenas para garantir
    public NovoChamadoRequest() { } 

    // Garanta que todas as propriedades sejam { get; set; }
    [Required]
    public string Titulo { get; set; } 

    [Required]
    public string Descricao { get; set; } 

    [Required]
    public string Prioridade { get; set; } 
}