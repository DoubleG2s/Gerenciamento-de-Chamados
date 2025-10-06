using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc.RazorPages;
using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using System.Data;


namespace SistemaChamados.Pages.Diagnostics
{
    //autorização
    [Authorize(Roles = "Admin")]
    public class TabelasVisuaisBdModel : PageModel
    {
        private readonly AppDbContext _db;

        public TabelasVisuaisBdModel(AppDbContext db) => _db = db;

        // Entrada (querystring)
        public string? SchemaSel { get; private set; }
        public string? TabelaSel { get; private set; }
        public int Page { get; private set; } = 1;
        public int PageSize { get; private set; } = 50;

        // Saída para a View
        public List<(string Schema, string Name)> Tabelas { get; private set; } = new();
        public List<(string Name, string DataType)> Colunas { get; private set; } = new();
        public List<Dictionary<string, object?>> Linhas { get; private set; } = new();
        public long? TotalLinhas { get; private set; }
        public bool TemProximaPagina { get; private set; }
        public string? Error { get; private set; }

        public async Task OnGet(string? schema, string? tabela, int? page, int? pageSize)
        {
            try
            {
                SchemaSel = schema;
                TabelaSel = tabela;
                Page = page.GetValueOrDefault(1);
                PageSize = pageSize.GetValueOrDefault(50);
                if (Page < 1) Page = 1;
                if (PageSize <= 0 || PageSize > 200) PageSize = 50;

                await using var conn = _db.Database.GetDbConnection();
                if (conn.State != ConnectionState.Open)
                    await conn.OpenAsync();

                // 1) Todas as tabelas (exceto system schemas)
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        select table_schema, table_name
                        from information_schema.tables
                        where table_type = 'BASE TABLE'
                          and table_schema not in ('pg_catalog','information_schema')
                        order by table_schema, table_name;";
                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        Tabelas.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }

                if (Tabelas.Count == 0) return;

                // se não foi selecionado, pega a primeira
                if (string.IsNullOrWhiteSpace(SchemaSel) || string.IsNullOrWhiteSpace(TabelaSel) ||
                    !Tabelas.Any(t => t.Schema == SchemaSel && t.Name == TabelaSel))
                {
                    var first = Tabelas.First();
                    SchemaSel = first.Schema;
                    TabelaSel = first.Name;
                }

                // 2) Colunas da tabela selecionada
                await using (var cmd = conn.CreateCommand())
                {
                    cmd.CommandText = @"
                        select column_name, data_type
                        from information_schema.columns
                        where table_schema = @schema and table_name = @tabela
                        order by ordinal_position;";
                    var p1 = cmd.CreateParameter(); p1.ParameterName = "schema"; p1.Value = SchemaSel!;
                    var p2 = cmd.CreateParameter(); p2.ParameterName = "tabela"; p2.Value = TabelaSel!;
                    cmd.Parameters.Add(p1); cmd.Parameters.Add(p2);

                    await using var reader = await cmd.ExecuteReaderAsync();
                    while (await reader.ReadAsync())
                    {
                        Colunas.Add((reader.GetString(0), reader.GetString(1)));
                    }
                }

                // 3) Dados (LIMIT/OFFSET) — protegendo identificadores
                string Q(string ident) => "\"" + ident.Replace("\"", "\"\"") + "\"";
                var identTable = $"{Q(SchemaSel!)}.{Q(TabelaSel!)}";
                var offset = (Page - 1) * PageSize;

                // Conta total (opcional — útil pra paginação)
                await using (var countCmd = conn.CreateCommand())
                {
                    countCmd.CommandText = $"select count(*) from {identTable};";
                    TotalLinhas = (long)(await countCmd.ExecuteScalarAsync() ?? 0L);
                }

                // Seleciona dados
                await using (var dataCmd = conn.CreateCommand())
                {
                    dataCmd.CommandText = $"select * from {identTable} order by 1 limit @lim offset @off;";
                    var pLim = dataCmd.CreateParameter(); pLim.ParameterName = "lim"; pLim.Value = PageSize;
                    var pOff = dataCmd.CreateParameter(); pOff.ParameterName = "off"; pOff.Value = offset;
                    dataCmd.Parameters.Add(pLim); dataCmd.Parameters.Add(pOff);

                    await using var rdr = await dataCmd.ExecuteReaderAsync();
                    while (await rdr.ReadAsync())
                    {
                        var row = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
                        for (int i = 0; i < rdr.FieldCount; i++)
                        {
                            var name = rdr.GetName(i);
                            var val = await rdr.IsDBNullAsync(i) ? null : rdr.GetValue(i);
                            row[name] = val;
                        }
                        Linhas.Add(row);
                    }
                }

                // Existe próxima página?
                TemProximaPagina = (TotalLinhas ?? 0) > (Page * (long)PageSize);
            }
            catch (Exception ex)
            {
                Error = ex.Message;
            }
        }
    }
}
