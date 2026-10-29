using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.ViewModels
{
    // Dados do formulário de cadastro.
    // Não existe campo de perfil de propósito: o cadastro público é sempre "User".
    public class RegisterViewModel
    {
        [Required(ErrorMessage = "Informe o nome completo.")]
        [StringLength(120, ErrorMessage = "O nome deve ter no máximo {1} caracteres.")]
        [Display(Name = "Nome completo")]
        public string NomeCompleto { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe o e-mail.")]
        [EmailAddress(ErrorMessage = "E-mail inválido.")]
        [StringLength(256)]
        [Display(Name = "E-mail")]
        public string Email { get; set; } = string.Empty;

        [Required(ErrorMessage = "Informe a senha.")]
        [StringLength(100, MinimumLength = 8, ErrorMessage = "A senha deve ter entre {2} e {1} caracteres.")]
        [DataType(DataType.Password)]
        [Display(Name = "Senha")]
        public string Senha { get; set; } = string.Empty;

        [Required(ErrorMessage = "Confirme a senha.")]
        [DataType(DataType.Password)]
        [Compare(nameof(Senha), ErrorMessage = "As senhas não conferem.")]
        [Display(Name = "Confirmar senha")]
        public string ConfirmarSenha { get; set; } = string.Empty;
    }
}
