namespace HelpDeskMvc.Models
{
    // Nomes dos perfis (roles) em um só lugar, evitando erros de digitação.
    public static class Perfis
    {
        public const string User = "User";
        public const string Support = "Support";
        public const string Admin = "Admin";

        // Usado em [Authorize(Roles = ...)]: aceita Support OU Admin.
        public const string SupportOuAdmin = Support + "," + Admin;

        // Lista oficial de perfis válidos (usada para validar o que vem do formulário).
        public static readonly string[] Todos = { User, Support, Admin };
    }
}
