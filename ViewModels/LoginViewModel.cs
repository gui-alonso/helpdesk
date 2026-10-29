using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.ViewModels
{
    // Dados do formulário de login.
    public class LoginViewModel
    {
        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Display(Name = "Lembrar-me")]
        public bool LembrarMe { get; set; }
    }
}
