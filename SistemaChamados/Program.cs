using Microsoft.EntityFrameworkCore;        // <- necess�rio para AddDbContext/UseNpgsql
using SistemaChamados.Data;                 // <- seu AppDbContext
using SistemaChamados.Services;             // <- InMemoryTicketStore
using Microsoft.AspNetCore.Authentication.Cookies; 
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages
builder.Services.AddRazorPages();

//Suporte a rotas de API
builder.Services.AddControllers();

// Mock store (singleton em mem�ria)
builder.Services.AddSingleton<InMemoryTicketStore>();
var jwtKey = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(jwtKey);
builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = "JwtBearer";
        options.DefaultChallengeScheme = "JwtBearer";
    })
    .AddJwtBearer("JwtBearer", options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = builder.Configuration["Jwt:Issuer"],
            ValidAudience = builder.Configuration["Jwt:Audience"],
            IssuerSigningKey = new SymmetricSecurityKey(key),
            ClockSkew = TimeSpan.Zero
        };
    });


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

//Ativa as rota de API
app.MapControllers();

// Endpoint de teste do DB (opcional)
app.MapGet("/ping-db", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();  // <- requer EF Core instalado
    return Results.Ok(new { connected = ok });
});

// sua rota inicial -> p�gina de Login
app.MapGet("/", () => Results.Redirect("/Login"));

app.Run();
