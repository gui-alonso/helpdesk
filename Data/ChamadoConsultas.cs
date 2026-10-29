using System.Security.Claims;
using HelpDeskMvc.Models;

namespace HelpDeskMvc.Data;

// Regra de visibilidade dos chamados, usada pela listagem, detalhes e dashboard.
public static class ChamadoConsultas
{
    // Support/Admin veem todos os chamados.
    // Qualquer outro usuário vê somente os chamados que ele mesmo abriu (RN04).
    // Chamados antigos (UsuarioId nulo) nunca aparecem para User (RN09).
    public static IQueryable<Chamado> VisiveisPara(this IQueryable<Chamado> chamados, ClaimsPrincipal usuario)
    {
        if (usuario.IsInRole(Perfis.Support) || usuario.IsInRole(Perfis.Admin))
        {
            return chamados;
        }

        // Id do usuário logado, guardado no cookie de autenticação.
        var usuarioId = usuario.FindFirstValue(ClaimTypes.NameIdentifier);

        if (string.IsNullOrEmpty(usuarioId))
        {
            // Sem identificação: não devolve nada (por segurança).
            return chamados.Where(c => false);
        }

        return chamados.Where(c => c.UsuarioId == usuarioId);
    }
}
