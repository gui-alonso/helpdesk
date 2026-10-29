namespace HelpDeskMvc.ViewModels
{
    // Uma linha da tela de gestão de usuários (Admin).
    public class UsuarioListaViewModel
    {
        public string Id { get; set; } = string.Empty;
        public string NomeCompleto { get; set; } = string.Empty;
        public string Email { get; set; } = string.Empty;
        public string Perfil { get; set; } = string.Empty;
        public bool EhUsuarioAtual { get; set; }
    }
}
