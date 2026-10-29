using HelpDeskMvc.Data;
using HelpDeskMvc.Models;
using HelpDeskMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskMvc.Areas.Admin.Controllers
{
    // Gestão de usuários: somente Admin (validado no servidor, não só escondendo o menu).
    [Area("Admin")]
    [Authorize(Roles = Perfis.Admin)]
    public class UsuariosController : Controller
    {
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;
        private readonly SignInManager<ApplicationUser> _signInManager;
        private readonly ILogger<UsuariosController> _logger;

        public UsuariosController(
            AppDbContext context,
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<UsuariosController> logger)
        {
            _context = context;
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Admin/Usuarios
        // Lista todos os usuários com o perfil atual de cada um.
        public async Task<IActionResult> Index()
        {
            var usuarios = await _userManager.Users
                .OrderBy(u => u.NomeCompleto)
                .ToListAsync();

            var idAtual = _userManager.GetUserId(User);
            var lista = new List<UsuarioListaViewModel>();

            foreach (var usuario in usuarios)
            {
                var perfis = await _userManager.GetRolesAsync(usuario);

                lista.Add(new UsuarioListaViewModel
                {
                    Id = usuario.Id,
                    NomeCompleto = usuario.NomeCompleto,
                    Email = usuario.Email ?? string.Empty,
                    // Cada usuário tem um único perfil; sem perfil é tratado como User.
                    Perfil = perfis.FirstOrDefault() ?? Perfis.User,
                    EhUsuarioAtual = usuario.Id == idAtual
                });
            }

            return View(lista);
        }

        // POST: /Admin/Usuarios/AlterarPerfil
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> AlterarPerfil(AlterarPerfilViewModel model)
        {
            // Só aceita perfis da lista oficial (o valor vem do cliente e pode ser forjado).
            if (!ModelState.IsValid || !Perfis.Todos.Contains(model.NovoPerfil))
            {
                return Mensagem("danger", "Perfil inválido.");
            }

            var usuario = await _userManager.FindByIdAsync(model.UsuarioId);
            if (usuario == null)
            {
                return NotFound();
            }

            var perfisAtuais = await _userManager.GetRolesAsync(usuario);

            if (perfisAtuais.Count == 1 && perfisAtuais[0] == model.NovoPerfil)
            {
                return Mensagem("info", $"{usuario.NomeCompleto} já possui o perfil {model.NovoPerfil}.");
            }

            // RN07: nunca deixar o sistema sem nenhum Admin.
            if (perfisAtuais.Contains(Perfis.Admin) && model.NovoPerfil != Perfis.Admin)
            {
                var admins = await _userManager.GetUsersInRoleAsync(Perfis.Admin);
                if (admins.Count <= 1)
                {
                    return Mensagem("danger", "Não é possível rebaixar o último Admin do sistema.");
                }
            }

            // Transação: remover o perfil antigo e adicionar o novo acontecem juntos (ou nenhum).
            await using var transacao = await _context.Database.BeginTransactionAsync();

            var remocao = await _userManager.RemoveFromRolesAsync(usuario, perfisAtuais);
            var adicao = remocao.Succeeded
                ? await _userManager.AddToRoleAsync(usuario, model.NovoPerfil)
                : remocao;

            if (!adicao.Succeeded)
            {
                await transacao.RollbackAsync();
                return Mensagem("danger", "Não foi possível alterar o perfil. Tente novamente.");
            }

            await transacao.CommitAsync();

            // Invalida o cookie atual do usuário: em até 1 minuto a sessão dele é encerrada
            // e, ao entrar de novo, ele já recebe o novo perfil.
            await _userManager.UpdateSecurityStampAsync(usuario);

            // Registro em log para auditoria: quem alterou, de quem, de/para qual perfil.
            _logger.LogInformation(
                "Admin {Admin} alterou o perfil de {Usuario} de [{De}] para {Para}.",
                User.Identity?.Name, usuario.Email, string.Join(",", perfisAtuais), model.NovoPerfil);

            // Se o Admin alterou o próprio perfil, atualiza o cookie dele na hora.
            // Como deixou de ser Admin, não pode mais ver esta tela: volta para a Home.
            if (usuario.Id == _userManager.GetUserId(User))
            {
                await _signInManager.RefreshSignInAsync(usuario);
                return RedirectToAction("Index", "Home", new { area = "" });
            }

            return Mensagem("success",
                $"Perfil de {usuario.NomeCompleto} alterado para {model.NovoPerfil}. O usuário precisará entrar novamente.");
        }

        // Guarda a mensagem para exibir após o redirecionamento e volta para a lista.
        private IActionResult Mensagem(string tipo, string texto)
        {
            TempData["MensagemTipo"] = tipo;
            TempData["Mensagem"] = texto;
            return RedirectToAction(nameof(Index));
        }
    }
}
