using HelpDeskMvc.Models;
using HelpDeskMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;

namespace HelpDeskMvc.Controllers
{
    // Cadastro, login, logout e acesso negado.
    public class AccountController : Controller
    {
        // Mensagem única para qualquer falha de login: não revela se o e-mail existe.
        private const string MensagemLoginInvalido =
            "E-mail ou senha inválidos. Após várias tentativas o acesso é bloqueado por alguns minutos.";

        private readonly UserManager<ApplicationUser> _userManager;     // cria/consulta usuários
        private readonly SignInManager<ApplicationUser> _signInManager; // faz login/logout (cookie)
        private readonly ILogger<AccountController> _logger;

        public AccountController(
            UserManager<ApplicationUser> userManager,
            SignInManager<ApplicationUser> signInManager,
            ILogger<AccountController> logger)
        {
            _userManager = userManager;
            _signInManager = signInManager;
            _logger = logger;
        }

        // GET: /Account/Login
        [AllowAnonymous]
        public IActionResult Login(string? returnUrl = null)
        {
            // Quem já está logado não precisa ver a tela de login.
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            ViewData["ReturnUrl"] = returnUrl;
            return View(new LoginViewModel());
        }

        // POST: /Account/Login
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Login(LoginViewModel model, string? returnUrl = null)
        {
            ViewData["ReturnUrl"] = returnUrl;

            if (!ModelState.IsValid)
            {
                return View(model);
            }

            var usuario = await _userManager.FindByEmailAsync(model.Email);
            if (usuario == null)
            {
                ModelState.AddModelError(string.Empty, MensagemLoginInvalido);
                return View(model);
            }

            // Compara a senha digitada com o hash salvo. lockoutOnFailure conta as tentativas erradas.
            var resultado = await _signInManager.PasswordSignInAsync(
                usuario, model.Senha, model.LembrarMe, lockoutOnFailure: true);

            if (!resultado.Succeeded)
            {
                if (resultado.IsLockedOut)
                {
                    _logger.LogWarning("Conta {Email} bloqueada por excesso de tentativas.", model.Email);
                }

                ModelState.AddModelError(string.Empty, MensagemLoginInvalido);
                return View(model);
            }

            _logger.LogInformation("Usuário {Email} entrou no sistema.", model.Email);

            // Só redireciona para URLs do próprio site (evita "open redirect").
            if (!string.IsNullOrEmpty(returnUrl) && Url.IsLocalUrl(returnUrl))
            {
                return LocalRedirect(returnUrl);
            }

            // Redireciona conforme o perfil: equipe de atendimento vai direto para a fila.
            if (await _userManager.IsInRoleAsync(usuario, Perfis.Support) ||
                await _userManager.IsInRoleAsync(usuario, Perfis.Admin))
            {
                return RedirectToAction("Index", "Chamados");
            }

            return RedirectToAction("Index", "Home");
        }

        // GET: /Account/Register
        [AllowAnonymous]
        public IActionResult Register()
        {
            if (User.Identity?.IsAuthenticated == true)
            {
                return RedirectToAction("Index", "Home");
            }

            return View(new RegisterViewModel());
        }

        // POST: /Account/Register
        [HttpPost]
        [AllowAnonymous]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Register(RegisterViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // O e-mail também é usado como nome de usuário (UserName).
            var usuario = new ApplicationUser
            {
                UserName = model.Email,
                Email = model.Email,
                NomeCompleto = model.NomeCompleto.Trim()
            };

            // CreateAsync valida a senha e salva apenas o HASH dela (PBKDF2 com salt).
            // A senha em texto puro nunca é gravada no banco.
            var resultado = await _userManager.CreateAsync(usuario, model.Senha);

            if (!resultado.Succeeded)
            {
                foreach (var erro in resultado.Errors)
                {
                    ModelState.AddModelError(string.Empty, erro.Description);
                }
                return View(model);
            }

            // Perfil fixo "User": o formulário não tem como escolher outro (RN01).
            var resultadoPerfil = await _userManager.AddToRoleAsync(usuario, Perfis.User);
            if (!resultadoPerfil.Succeeded)
            {
                // Desfaz o cadastro para não deixar usuário sem perfil.
                await _userManager.DeleteAsync(usuario);
                _logger.LogError("Falha ao atribuir o perfil User a {Email}.", model.Email);
                ModelState.AddModelError(string.Empty, "Não foi possível concluir o cadastro. Tente novamente.");
                return View(model);
            }

            _logger.LogInformation("Novo usuário cadastrado: {Email}.", model.Email);

            // Já entra logado após o cadastro.
            await _signInManager.SignInAsync(usuario, isPersistent: false);
            return RedirectToAction("Index", "Home");
        }

        // POST: /Account/Logout
        // Logout só por POST com antiforgery: um link malicioso não consegue deslogar o usuário.
        [HttpPost]
        [Authorize]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Logout()
        {
            await _signInManager.SignOutAsync();
            return RedirectToAction(nameof(Login));
        }

        // GET: /Account/AccessDenied
        // Usuário logado tentou acessar algo que seu perfil não permite.
        [AllowAnonymous]
        public IActionResult AccessDenied()
        {
            return View();
        }
    }
}
