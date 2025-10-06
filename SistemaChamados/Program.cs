using Microsoft.EntityFrameworkCore;        // <- necessário para AddDbContext/UseNpgsql
using SistemaChamados.Data;                 // <- seu AppDbContext
using SistemaChamados.Services;             // <- InMemoryTicketStore
using Microsoft.AspNetCore.Authentication.Cookies; // <- Autenticação por Cookies

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

// Mock store (singleton em memória)
builder.Services.AddSingleton<InMemoryTicketStore>();

builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Autentication/Login";
        opt.LogoutPath = "/Autentication/Login";
        opt.AccessDeniedPath = "/Autentication/AccessDenied";
        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
    });

builder.Services.AddAuthorization();

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
app.UseAuthentication(); // <- importante ser antes do UseAuthorization
app.UseAuthorization(); // <- importante ser depois do UseAuthentication

app.MapRazorPages();

// Endpoint de teste do DB (opcional)
app.MapGet("/ping-db", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();  // <- requer EF Core instalado
    return Results.Ok(new { connected = ok });
});

// sua rota inicial -> página de Login
app.MapGet("/", () => Results.Redirect("/Login"));

app.Run();
