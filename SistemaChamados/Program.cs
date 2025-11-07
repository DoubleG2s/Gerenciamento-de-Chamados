using Microsoft.EntityFrameworkCore;        // <- necess�rio para AddDbContext/UseNpgsql
using SistemaChamados.Data;                 // <- seu AppDbContext
using SistemaChamados.Services;             // <- InMemoryTicketStore
using Microsoft.AspNetCore.Authentication.Cookies; // <- Autentica��o por Cookies

var builder = WebApplication.CreateBuilder(args);

// Razor Pages e Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Mock store (singleton em mem�ria)
builder.Services.AddSingleton<InMemoryTicketStore>();

// Serviços de Chat IA
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHttpClient<TextToSpeechService>();
builder.Services.AddScoped<TextToSpeechService>();




builder.Services
    .AddAuthentication(CookieAuthenticationDefaults.AuthenticationScheme)
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Login";
        opt.LogoutPath = "/Logout";
        opt.AccessDeniedPath = "/AccessDenied";
        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.Cookie.HttpOnly = true;  // ADICIONAR (seguran�a)
        opt.Cookie.IsEssential = true;  // ADICIONAR (seguran�a)
    });

builder.Services.AddAuthorization();

// EF Core + Npgsql usando DefaultConnection
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection n�o configurada.");

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

using (var scope = app.Services.CreateScope())
{
    var db = scope.ServiceProvider.GetRequiredService<AppDbContext>();
    db.Database.EnsureCreated();
}

app.UseHttpsRedirection();
app.UseStaticFiles();
app.UseRouting();
app.UseAuthentication(); // <- importante ser antes do UseAuthorization
//Middleware para prevenir cache de p�ginas autenticadas
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});
app.UseAuthorization(); // <- importante ser depois do UseAuthentication

app.MapRazorPages();
app.MapControllers(); // Adiciona mapeamento de controllers

// Endpoint de teste do DB (opcional)
app.MapGet("/ping-db", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();  // <- requer EF Core instalado
    return Results.Ok(new { connected = ok });
});

// sua rota inicial -> p�gina de Login
app.MapGet("/", () => Results.Redirect("/Login"));

app.Run();
