using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.ViewModels
{
    // Formulário de edição de chamado: só Título e Descrição podem ser alterados.
    // Id vem da rota; Status, datas e dono nunca vêm do formulário (evita overposting).
    public class EditarChamadoViewModel
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
