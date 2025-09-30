using SistemaChamados.Models;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Services
{
    public class InMemoryTicketStore
    {
        public List<Categoria> Categorias { get; } = new()
        {
            new() { Id = 1, Nome = "Sistema" },
            new() { Id = 2, Nome = "Acesso" },
            new() { Id = 3, Nome = "Relatórios" },
            new() { Id = 4, Nome = "Hardware" },
            new() { Id = 5, Nome = "Email" },
            new() { Id = 6, Nome = "Rede" }
        };

        public List<Usuario> Usuarios { get; } = new()
        {
            // NÃO atribua "Iniciais" (é calculado a partir de Nome)
            new() { Id = 1, Nome = "João Silva" },
            new() { Id = 2, Nome = "Maria Santos" },
            new() { Id = 3, Nome = "Pedro Costa" },
            new() { Id = 4, Nome = "Ana Oliveira" },
            new() { Id = 5, Nome = "Carlos Lima" },
            new() { Id = 6, Nome = "Fernanda Rocha" }
        };

        public List<Ticket> Tickets { get; }
        private int _nextId = 7;

        public InMemoryTicketStore()
        {
            Tickets = new()
            {
                new Ticket
                {
                    Id = 1,
                    Titulo = "Sistema de login não está funcionando",
                    Descricao = "Falha ao autenticar.",
                    CategoriaId = 1,
                    Prioridade = PriorityLevel.Alta,
                    Status = TicketStatus.Aberto,
                    SolicitanteId = 1,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 08), DateTimeKind.Utc)
                },
                new Ticket
                {
                    Id = 2,
                    Titulo = "Solicitação de acesso ao módulo financeiro",
                    Descricao = "Permissões ao módulo X.",
                    CategoriaId = 2,
                    Prioridade = PriorityLevel.Média, // antes: Média
                    Status = TicketStatus.Andamento,
                    SolicitanteId = 2,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 07), DateTimeKind.Utc)
                },
                new Ticket
                {
                    Id = 3,
                    Titulo = "Erro ao gerar relatório mensal",
                    Descricao = "Stacktrace no anexo.",
                    CategoriaId = 3,
                    Prioridade = PriorityLevel.Baixa,
                    Status = TicketStatus.Resolvido,
                    SolicitanteId = 3,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 06), DateTimeKind.Utc)
                },
                new Ticket
                {
                    Id = 4,
                    Titulo = "Computador não liga após atualização",
                    Descricao = "PC reinicia em loop.",
                    CategoriaId = 4,
                    Prioridade = PriorityLevel.Crítica, // antes: Crítica
                    Status = TicketStatus.Aberto,
                    SolicitanteId = 4,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 05), DateTimeKind.Utc)
                },
                new Ticket
                {
                    Id = 5,
                    Titulo = "Configuração de email corporativo",
                    Descricao = "Novo colaborador.",
                    CategoriaId = 5,
                    Prioridade = PriorityLevel.Baixa,
                    Status = TicketStatus.Fechado,
                    SolicitanteId = 5,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 04), DateTimeKind.Utc)
                },
                new Ticket
                {
                    Id = 6,
                    Titulo = "Lentidão na rede interna",
                    Descricao = "Quedas intermitentes.",
                    CategoriaId = 6,
                    Prioridade = PriorityLevel.Alta,
                    Status = TicketStatus.Andamento,
                    SolicitanteId = 6,
                    CriadoEm = DateTime.SpecifyKind(new DateTime(2025, 01, 03), DateTimeKind.Utc)
                }
            };

            // relaciona navegação
            foreach (var ticket in Tickets)
            {
                ticket.Categoria = Categorias.First(ctg => ctg.Id == ticket.CategoriaId);
                ticket.Solicitante = Usuarios.First(user => user.Id == ticket.SolicitanteId);
            }
        }

        public IEnumerable<Ticket> GetAll() => Tickets.OrderByDescending(t => t.CriadoEm);
        public Ticket? GetById(int id) => Tickets.FirstOrDefault(t => t.Id == id);

        public int Add(Ticket ticket)
        {
            ticket.Id = _nextId++;
            ticket.CriadoEm = DateTime.UtcNow;

            Tickets.Add(ticket);

            ticket.Categoria = Categorias.First(ctg => ctg.Id == ticket.CategoriaId);
            ticket.Solicitante = Usuarios.First(user => user.Id == ticket.SolicitanteId);

            return ticket.Id;
        }
    }
}
