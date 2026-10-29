using HelpDeskMvc.Models;
using Microsoft.AspNetCore.Identity.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskMvc.Data;

// Agora herda de IdentityDbContext: além dos chamados, o mesmo banco guarda
// as tabelas do Identity (AspNetUsers, AspNetRoles, AspNetUserRoles...).
public class AppDbContext : IdentityDbContext<ApplicationUser>
{
    // O DbContext é o "coração" do Entity Framework Core. Ele representa a sessão com o banco de dados e permite consultar e salvar dados.
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chamado> Chamados { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        // Obrigatório: configura as tabelas do Identity.
        base.OnModelCreating(modelBuilder);

        // Persistimos o enum como texto (ex.: "Aberto") em vez de número,
        // mantendo compatibilidade com os registros já gravados no SQLite
        // e a legibilidade direta do valor na tabela.
        modelBuilder.Entity<Chamado>()
            .Property(c => c.Status)
            .HasConversion<string>();

        // Relacionamento Chamado -> Usuário (opcional por causa dos chamados antigos).
        // Restrict: não deixa excluir um usuário que possui chamados, preservando o histórico.
        modelBuilder.Entity<Chamado>()
            .HasOne(c => c.Usuario)
            .WithMany()
            .HasForeignKey(c => c.UsuarioId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
