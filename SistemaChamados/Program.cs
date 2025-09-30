using Microsoft.EntityFrameworkCore;        // <- necessário para AddDbContext/UseNpgsql
using SistemaChamados.Data;                 // <- seu AppDbContext
using SistemaChamados.Services;             // <- InMemoryTicketStore

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Mock store (singleton em memória)
builder.Services.AddSingleton<InMemoryTicketStore>();

// EF Core + Npgsql usando DefaultConnection
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(connString);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true); // Para compatibilidade com timestamps
});

var app = builder.Build();

if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Error");
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();

app.MapRazorPages();

// Endpoint de teste do DB (opcional)
app.MapGet("/ping-db", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();  // <- requer EF Core instalado
    return Results.Ok(new { connected = ok });
});

// sua rota inicial
app.MapGet("/", () => Results.Redirect("/Dashboard"));

app.Run();
