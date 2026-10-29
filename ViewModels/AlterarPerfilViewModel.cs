using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.ViewModels
{
    // Dados enviados pelo Admin para trocar o perfil de um usuário.
    // O valor de NovoPerfil é validado no servidor contra a lista Perfis.Todos.
    public class AlterarPerfilViewModel
    {
        [Required]
        public string UsuarioId { get; set; } = string.Empty;

        [Required]
        public string NovoPerfil { get; set; } = string.Empty;
    }
}
