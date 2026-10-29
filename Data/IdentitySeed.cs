using HelpDeskMvc.Models;
using Microsoft.AspNetCore.Identity;

namespace HelpDeskMvc.Data;

// Executado na inicialização da aplicação.
// É idempotente: pode rodar várias vezes sem duplicar nada.
public static class IdentitySeed
{
    public static async Task InicializarAsync(IServiceProvider services)
    {
        using var scope = services.CreateScope();
        var roleManager = scope.ServiceProvider.GetRequiredService<RoleManager<IdentityRole>>();
        var userManager = scope.ServiceProvider.GetRequiredService<UserManager<ApplicationUser>>();
        var config = scope.ServiceProvider.GetRequiredService<IConfiguration>();
        var logger = scope.ServiceProvider.GetRequiredService<ILoggerFactory>().CreateLogger("IdentitySeed");

        // 1) Cria as roles que ainda não existem.
        foreach (var perfil in Perfis.Todos)
        {
            if (!await roleManager.RoleExistsAsync(perfil))
            {
                await roleManager.CreateAsync(new IdentityRole(perfil));
                logger.LogInformation("Role {Perfil} criada.", perfil);
            }
        }

        // 2) Primeiro Admin: só se ainda não existir nenhum Admin.
        if ((await userManager.GetUsersInRoleAsync(Perfis.Admin)).Any())
        {
            return;
        }

        // Os dados vêm de User Secrets ou variáveis de ambiente, NUNCA do código/Git.
        var email = config["AdminInicial:Email"];
        var senha = config["AdminInicial:Senha"];
        var nome = config["AdminInicial:Nome"] ?? "Administrador";

        if (string.IsNullOrWhiteSpace(email) || string.IsNullOrWhiteSpace(senha))
        {
            logger.LogWarning("Nenhum Admin cadastrado. Configure AdminInicial:Email e AdminInicial:Senha (veja docs/manual-autenticacao-perfis.md).");
            return;
        }

        var admin = await userManager.FindByEmailAsync(email);

        if (admin == null)
        {
            admin = new ApplicationUser { UserName = email, Email = email, NomeCompleto = nome };

            // CreateAsync grava a senha como hash (nunca em texto puro).
            var resultado = await userManager.CreateAsync(admin, senha);
            if (!resultado.Succeeded)
            {
                logger.LogError("Falha ao criar o Admin inicial: {Erros}",
                    string.Join("; ", resultado.Errors.Select(e => e.Description)));
                return;
            }
        }
        else
        {
            // Conta já existia (ex.: cadastrada como User): remove os perfis atuais para ficar só Admin.
            await userManager.RemoveFromRolesAsync(admin, await userManager.GetRolesAsync(admin));
        }

        await userManager.AddToRoleAsync(admin, Perfis.Admin);
        logger.LogInformation("Admin inicial configurado para {Email}.", email);
    }
}
