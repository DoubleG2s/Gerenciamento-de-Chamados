using Microsoft.EntityFrameworkCore;
using SistemaChamados.Models;

namespace SistemaChamados.Data
{
    public class AppDbContext : DbContext
    {
        public AppDbContext(DbContextOptions<AppDbContext> options) : base(options)
        {
        }

        public DbSet<Ticket> Tickets { get; set; }
        public DbSet<Categoria> Categorias { get; set; }
        public DbSet<Usuario> Usuarios { get; set; }
        public DbSet<Anexo> Anexos { get; set; }
        public DbSet<ComentarioTicket> ComentariosTicket { get; set; }

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            base.OnModelCreating(modelBuilder);

            // Configurar conversão de Enums para string
            modelBuilder.Entity<Ticket>()
                .Property(t => t.Prioridade)
                .HasConversion<string>();

            modelBuilder.Entity<Ticket>()
                .Property(t => t.Status)
                .HasConversion<string>();

            modelBuilder.Entity<ComentarioTicket>()
                .Property(c => c.Tipo)
                .HasConversion<string>();

            // Configurações de precisão para timestamps (se necessário)
            modelBuilder.Entity<Ticket>()
                .Property(t => t.CriadoEm)
                .HasColumnType("timestamp");

            modelBuilder.Entity<Ticket>()
                .Property(t => t.AtualizadoEm)
                .HasColumnType("timestamp");
        }
    }
}
