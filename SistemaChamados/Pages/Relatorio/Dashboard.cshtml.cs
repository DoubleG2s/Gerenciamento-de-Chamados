using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Models;
using static SistemaChamados.Models.Enums;

namespace SistemaChamados.Pages.Relatorio;

[Authorize(Roles = "Admin,Tecnico")]
public class DashboardModel : PageModel
{
    private readonly AppDbContext _context;

    public DashboardModel(AppDbContext context)
    {
        _context = context;
    }

    // Propriedades
    public DateTime? DataInicio { get; set; }
    public DateTime? DataFim { get; set; }
    public int? CategoriaId { get; set; }

    // Métricas resumo
    public int TotalFechados { get; set; }
    public int TotalAbertos { get; set; }
    public decimal TempoMedioResolucao { get; set; }
    public decimal TaxaResolucao { get; set; }
    public decimal PercentualFechados { get; set; }

    // Dados para gráficos
    public List<FechadosPorDiaDto> FechadosPorDia { get; set; } = new();
    public List<StatusCountDto> TicketsPorStatus { get; set; } = new();
    public List<CategoriaCountDto> TopCategorias { get; set; } = new();
    public List<PrioridadeCountDto> TicketsPorPrioridade { get; set; } = new();

    // Rankings
    public List<UsuarioCountDto> TopSolicitantes { get; set; } = new();
    public List<UsuarioCountDto> TopTecnicos { get; set; } = new();

    // Lista detalhada
    public List<TicketFechadoDto> UltimosFechados { get; set; } = new();

    // Categorias para filtro
    public List<Categoria> Categorias { get; set; } = new();

    public async Task OnGetAsync(int? periodo = 30, DateTime? dataInicio = null, DateTime? dataFim = null, int? categoriaId = null)
    {
        // Definir período
        if (dataInicio.HasValue && dataFim.HasValue)
        {
            DataInicio = dataInicio.Value;
            DataFim = dataFim.Value;
        }
        else
        {
            DataFim = DateTime.UtcNow;
            DataInicio = DataFim.Value.AddDays(-(periodo ?? 30));
        }

        CategoriaId = categoriaId;

        // Buscar categorias para filtro
        Categorias = await _context.Categorias
            .Where(c => c.Ativo)
            .OrderBy(c => c.Nome)
            .ToListAsync();

        // Query base
        var ticketsQuery = _context.Tickets
            .Include(t => t.Categoria)
            .Include(t => t.Solicitante)
            .Include(t => t.Responsavel)
            .Where(t => t.CriadoEm >= DataInicio && t.CriadoEm <= DataFim)
            .AsQueryable();

        if (CategoriaId.HasValue)
        {
            ticketsQuery = ticketsQuery.Where(t => t.CategoriaId == CategoriaId.Value);
        }

        var tickets = await ticketsQuery.ToListAsync();

        // ===== MÉTRICAS RESUMO =====
        TotalFechados = tickets.Count(t => t.Status == TicketStatus.Fechado);
        TotalAbertos = tickets.Count;

        var ticketsFechados = tickets.Where(t => t.Status == TicketStatus.Fechado && t.FechadoEm.HasValue).ToList();

        if (ticketsFechados.Any())
        {
            var temposResolucao = ticketsFechados
                .Select(t => (t.FechadoEm!.Value - t.CriadoEm).TotalHours)
                .ToList();

            TempoMedioResolucao = Math.Round((decimal)temposResolucao.Average(), 1);
        }

        TaxaResolucao = TotalAbertos > 0 ? Math.Round((decimal)TotalFechados / TotalAbertos * 100, 1) : 0;

        var totalMesAnterior = await _context.Tickets
            .Where(t => t.CriadoEm >= DataInicio.Value.AddMonths(-1) && t.CriadoEm < DataInicio.Value && t.Status == TicketStatus.Fechado)
            .CountAsync();

        PercentualFechados = totalMesAnterior > 0
            ? Math.Round((decimal)(TotalFechados - totalMesAnterior) / totalMesAnterior * 100, 1)
            : 0;

        // ===== FECHADOS POR DIA =====
        FechadosPorDia = ticketsFechados
            .GroupBy(t => t.FechadoEm!.Value.Date)
            .Select(g => new FechadosPorDiaDto
            {
                Data = g.Key,
                Total = g.Count()
            })
            .OrderBy(x => x.Data)
            .ToList();

        // Preencher dias sem fechamentos com 0
        if (DataInicio.HasValue && DataFim.HasValue)
        {
            var diasCompletos = new List<FechadosPorDiaDto>();
            for (var data = DataInicio.Value.Date; data <= DataFim.Value.Date; data = data.AddDays(1))
            {
                var fechadosNoDia = FechadosPorDia.FirstOrDefault(f => f.Data == data);
                diasCompletos.Add(new FechadosPorDiaDto
                {
                    Data = data,
                    Total = fechadosNoDia?.Total ?? 0
                });
            }
            FechadosPorDia = diasCompletos;
        }

        // ===== POR STATUS =====
        TicketsPorStatus = tickets
            .GroupBy(t => t.Status)
            .Select(g => new StatusCountDto
            {
                Status = g.Key.ToString(),
                Total = g.Count()
            })
            .ToList();

        // ===== TOP CATEGORIAS =====
        TopCategorias = tickets
            .Where(t => t.Categoria != null)
            .GroupBy(t => t.Categoria!.Nome)
            .Select(g => new CategoriaCountDto
            {
                Categoria = g.Key,
                Total = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        // ===== POR PRIORIDADE =====
        TicketsPorPrioridade = tickets
            .GroupBy(t => t.Prioridade)
            .Select(g => new PrioridadeCountDto
            {
                Prioridade = g.Key.ToString(),
                Total = g.Count()
            })
            .ToList();

        // ===== TOP SOLICITANTES (CORRIGIDO) =====
        TopSolicitantes = tickets
            .Where(t => t.Solicitante != null)
            .GroupBy(t => new {
                t.Solicitante!.Id,
                t.Solicitante.Nome,  // ← SEM Sobrenome
                t.Solicitante.Departamento,
                t.Solicitante.Cargo
            })
            .Select(g => new UsuarioCountDto
            {
                Nome = g.Key.Nome,  // ← CORRIGIDO: apenas Nome
                Departamento = g.Key.Departamento,
                Cargo = g.Key.Cargo,
                Total = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        // ===== TOP TÉCNICOS/RESPONSÁVEIS (CORRIGIDO) =====
        TopTecnicos = ticketsFechados
            .Where(t => t.Responsavel != null)
            .GroupBy(t => new {
                t.Responsavel!.Id,
                t.Responsavel.Nome,  // ← SEM Sobrenome
                t.Responsavel.Departamento,
                t.Responsavel.Cargo
            })
            .Select(g => new UsuarioCountDto
            {
                Nome = g.Key.Nome,  // ← CORRIGIDO: apenas Nome
                Departamento = g.Key.Departamento,
                Cargo = g.Key.Cargo,
                Total = g.Count()
            })
            .OrderByDescending(x => x.Total)
            .Take(10)
            .ToList();

        // ===== ÚLTIMOS FECHADOS (CORRIGIDO) =====
        UltimosFechados = ticketsFechados
            .OrderByDescending(t => t.FechadoEm)
            .Take(50)
            .Select(t => new TicketFechadoDto
            {
                Id = t.Id,
                Titulo = t.Titulo,
                CategoriaNome = t.Categoria != null ? t.Categoria.Nome : "Sem categoria",
                SolicitanteNome = t.Solicitante != null ? t.Solicitante.Nome : "Desconhecido",  // ← CORRIGIDO
                TecnicoNome = t.Responsavel != null ? t.Responsavel.Nome : "N/A",  // ← CORRIGIDO
                Prioridade = t.Prioridade.ToString(),
                PrioridadeCor = GetPrioridadeCor(t.Prioridade),
                CriadoEm = t.CriadoEm,
                FechadoEm = t.FechadoEm,
                TempoResolucao = t.FechadoEm.HasValue
                    ? Math.Round((t.FechadoEm.Value - t.CriadoEm).TotalHours, 1)
                    : 0
            })
            .ToList();
    }

    private string GetPrioridadeCor(PriorityLevel prioridade)
    {
        return prioridade switch
        {
            PriorityLevel.Baixa => "success",
            PriorityLevel.Média => "warning",
            PriorityLevel.Alta => "orange",
            PriorityLevel.Crítica => "danger",
            _ => "secondary"
        };
    }

    // DTOs
    public class FechadosPorDiaDto
    {
        public DateTime Data { get; set; }
        public int Total { get; set; }
    }

    public class StatusCountDto
    {
        public string Status { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class CategoriaCountDto
    {
        public string Categoria { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class PrioridadeCountDto
    {
        public string Prioridade { get; set; } = string.Empty;
        public int Total { get; set; }
    }

    public class UsuarioCountDto
    {
        public string Nome { get; set; } = string.Empty;
        public string? Departamento { get; set; }
        public string? Cargo { get; set; }
        public int Total { get; set; }
    }

    public class TicketFechadoDto
    {
        public int Id { get; set; }
        public string Titulo { get; set; } = string.Empty;
        public string CategoriaNome { get; set; } = string.Empty;
        public string SolicitanteNome { get; set; } = string.Empty;
        public string TecnicoNome { get; set; } = string.Empty;
        public string Prioridade { get; set; } = string.Empty;
        public string PrioridadeCor { get; set; } = string.Empty;
        public DateTime CriadoEm { get; set; }
        public DateTime? FechadoEm { get; set; }
        public double TempoResolucao { get; set; }
    }
}
