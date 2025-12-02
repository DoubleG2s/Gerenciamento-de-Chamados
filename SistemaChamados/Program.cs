using Microsoft.EntityFrameworkCore;
using SistemaChamados.Data;
using SistemaChamados.Services;
using Microsoft.AspNetCore.Authentication.Cookies;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.IdentityModel.Tokens;
using System.Text;

var builder = WebApplication.CreateBuilder(args);

// Razor Pages e Controllers
builder.Services.AddRazorPages();
builder.Services.AddControllers();

// Mock store (singleton em memória)
builder.Services.AddSingleton<InMemoryTicketStore>();

// Serviços de Chat IA
builder.Services.AddHttpClient<ChatService>();
builder.Services.AddScoped<ChatService>();
builder.Services.AddHttpClient<TextToSpeechService>();
builder.Services.AddScoped<TextToSpeechService>();

// Configuração JWT
var jwtKey = builder.Configuration["Jwt:Key"];
var key = Encoding.ASCII.GetBytes(jwtKey);

builder.Services.AddAuthentication(options =>
    {
        options.DefaultAuthenticateScheme = CookieAuthenticationDefaults.AuthenticationScheme;
        options.DefaultChallengeScheme = CookieAuthenticationDefaults.AuthenticationScheme;
    })
    .AddCookie(opt =>
    {
        opt.LoginPath = "/Login";
        opt.LogoutPath = "/Logout";
        opt.AccessDeniedPath = "/AccessDenied";
        opt.SlidingExpiration = true;
        opt.ExpireTimeSpan = TimeSpan.FromHours(8);
        opt.Cookie.HttpOnly = true;
        opt.Cookie.IsEssential = true;
    })
    .AddJwtBearer("Bearer", options =>
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

builder.Services.AddAuthorization();

// Configuração do banco de dados
var connString = builder.Configuration.GetConnectionString("DefaultConnection")
    ?? throw new InvalidOperationException("ConnectionStrings:DefaultConnection não configurada.");

builder.Services.AddDbContext<AppDbContext>(opt =>
{
    opt.UseNpgsql(connString);
    AppContext.SetSwitch("Npgsql.EnableLegacyTimestampBehavior", true);
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

app.UseAuthentication();

// Middleware para prevenir cache de páginas autenticadas
app.Use(async (context, next) =>
{
    context.Response.Headers["Cache-Control"] = "no-cache, no-store, must-revalidate";
    context.Response.Headers["Pragma"] = "no-cache";
    context.Response.Headers["Expires"] = "0";
    await next();
});

app.UseAuthorization();

app.MapRazorPages();
app.MapControllers();

app.MapGet("/ping-db", async (AppDbContext db) =>
{
    var ok = await db.Database.CanConnectAsync();
    return Results.Ok(new { connected = ok });
});

// Rota inicial -> página de Login
app.MapGet("/", () => Results.Redirect("/Login"));

app.Run();
