using HelpDeskMvc.Models;

namespace HelpDeskMvc.ViewModels
{
    // Um ViewModel existe porque a tela precisa de coisas que NENHUMA entidade
    // sozinha representa: quatro contadores + uma lista parcial de chamados.
    //
    // Alternativa ruim seria mandar tudo por ViewData/ViewBag: sem tipagem,
    // sem IntelliSense, erro só aparece em tempo de execução.
    // Com ViewModel, se você errar o nome de uma propriedade, o build quebra.
    public class DashboardViewModel
    {
        public int Total { get; set; }
        public int Abertos { get; set; }
        public int EmAndamento { get; set; }
        public int Resolvidos { get; set; }

        // Inicializada para a view nunca receber null e estourar no @foreach.
        public List<Chamado> UltimosChamados { get; set; } = new();
    }
}