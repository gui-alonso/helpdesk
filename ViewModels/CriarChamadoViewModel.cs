using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.ViewModels
{
    // Formulário de abertura de chamado: só os campos que o usuário pode preencher.
    // Status, datas e dono são definidos pelo servidor (evita overposting).
    public class CriarChamadoViewModel
    {
        [Required(ErrorMessage = "O título é obrigatório.")]
        [StringLength(100, ErrorMessage = "O título deve ter no máximo {1} caracteres.")]
        [Display(Name = "Título")]
        public string Titulo { get; set; } = string.Empty;

        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo {1} caracteres.")]
        [Display(Name = "Descrição")]
        public string Descricao { get; set; } = string.Empty;
    }
}
