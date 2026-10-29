using System.ComponentModel.DataAnnotations;
using Microsoft.AspNetCore.Identity;

namespace HelpDeskMvc.Models
{
    // Usuário do sistema. Herda do IdentityUser, que já traz e-mail,
    // hash da senha, bloqueio por tentativas etc. Aqui só acrescentamos o nome.
    public class ApplicationUser : IdentityUser
    {
        [Required]
        [StringLength(120)]
        public string NomeCompleto { get; set; } = string.Empty;
    }
}
