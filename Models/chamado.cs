using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.Models
{
    public class Chamado
    {
        public int Id { get; set; }

        // Validações usando Data Annotations
        [Required(ErrorMessage = "O título é obrigatório.")]
        [Display(Name = "Título")]
        public string? Titulo { get; set; }

        // Validações usando Data Annotations
        [Required(ErrorMessage = "A descrição é obrigatória.")]
        [Display(Name = "Descrição")]
        public string? Descricao { get; set; }
        public ChamadoStatus Status { get; set; } = ChamadoStatus.Aberto;
        public DateTime DataAbertura { get; set; } = DateTime.Now;
        public DateTime? DataFechamento { get; set; }

        // Dono do chamado (quem abriu). Opcional porque chamados antigos não têm dono;
        // esses ficam visíveis apenas para Support/Admin.
        public string? UsuarioId { get; set; }
        public ApplicationUser? Usuario { get; set; }
    }
}
