using HelpDeskMvc.Data;
using HelpDeskMvc.Models;
using HelpDeskMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Identity;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskMvc.Controllers
{
    // [Authorize] na classe: todas as actions exigem usuário logado.
    // Anônimo é redirecionado para /Account/Login.
    [Authorize]
    public class ChamadosController : Controller
    {
        // O campo é readonly: recebemos o contexto uma vez e não trocamos depois.
        // Note que NÃO existe mais lista estática nem contadorId.
        private readonly AppDbContext _context;
        private readonly UserManager<ApplicationUser> _userManager;

        // Injeção de dependência.
        // Nós nunca damos "new AppDbContext()". Quem constrói é o ASP.NET Core,
        // usando o que foi registrado no Program.cs com AddDbContext.
        // O controller apenas declara o que precisa e recebe pronto.
        public ChamadosController(AppDbContext context, UserManager<ApplicationUser> userManager)
        {
            _context = context;
            _userManager = userManager;
        }

        // GET: /Chamados
        public async Task<IActionResult> Index()
        {
            // VisiveisPara(User): User vê só os próprios chamados; Support/Admin veem todos.
            // ToListAsync() é o momento em que a consulta vai ao banco.
            // O OrderByDescending acontece no SQL, não em memória.
            var chamados = await _context.Chamados
                .VisiveisPara(User)
                .Include(c => c.Usuario)
                .OrderByDescending(c => c.DataAbertura)
                .ToListAsync();

            return View(chamados);
        }

        // GET: /Chamados/Detalhes/5
        public async Task<IActionResult> Detalhes(int id)
        {
            // O filtro de visibilidade também vale aqui: se um User tentar abrir
            // o chamado de outra pessoa pela URL, recebe 404 (não revelamos que ele existe).
            var chamadoRecuperado = await _context.Chamados
                .VisiveisPara(User)
                .Include(c => c.Usuario)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamadoRecuperado == null)
            {
                return NotFound();
            }

            ViewData["Title"] = "Detalhes do Chamado";

            return View(chamadoRecuperado);
        }

        // GET: /Chamados/Create
        // Continua sincrono: só devolve o formulário vazio, não toca no banco.
        public IActionResult Create()
        {
            return View(new CriarChamadoViewModel());
        }

        // POST: /Chamados/Create
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Create(CriarChamadoViewModel model)
        {
            if (!ModelState.IsValid)
            {
                return View(model);
            }

            // Recebemos um ViewModel só com Título e Descrição, então o formulário
            // não consegue enviar Id, Status, datas ou dono (proteção contra overposting).
            // Os demais campos são definidos aqui, pelo servidor (RN06).
            var chamado = new Chamado
            {
                Titulo = model.Titulo,
                Descricao = model.Descricao,
                Status = ChamadoStatus.Aberto,
                DataAbertura = DateTime.Now,
                DataFechamento = null,
                // Dono = usuário logado (lido do cookie, nunca do formulário).
                UsuarioId = _userManager.GetUserId(User)
            };

            // Add() ainda não grava nada. Ele apenas marca a entidade como "a ser inserida"
            // no change tracker do EF Core.
            _context.Chamados.Add(chamado);

            // SaveChangesAsync() é quem realmente executa o INSERT.
            // Depois desta linha, chamado.Id já contém o valor gerado pelo banco.
            await _context.SaveChangesAsync();

            return RedirectToAction(nameof(Index));
        }

        // GET: /Chamados/Editar/5
        public async Task<IActionResult> Editar(int id)
        {
            // VisiveisPara(User): um User só encontra os próprios chamados.
            // Chamado de outra pessoa (ou inexistente) resulta em 404.
            var chamado = await _context.Chamados
                .VisiveisPara(User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            if (!PodeEditar(chamado))
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Você só pode editar chamados com status Aberto.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            var model = new EditarChamadoViewModel
            {
                Titulo = chamado.Titulo ?? string.Empty,
                Descricao = chamado.Descricao ?? string.Empty
            };

            ViewData["ChamadoId"] = chamado.Id;
            return View(model);
        }

        // POST: /Chamados/Editar/5
        [HttpPost]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Editar(int id, EditarChamadoViewModel model)
        {
            // Buscamos o chamado de novo no banco: nunca confiamos no que veio da tela.
            var chamado = await _context.Chamados
                .VisiveisPara(User)
                .FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            // O status é conferido no momento de salvar: se o suporte iniciou o
            // atendimento enquanto o User editava, a alteração é recusada.
            if (!PodeEditar(chamado))
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Você só pode editar chamados com status Aberto.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            if (!ModelState.IsValid)
            {
                ViewData["ChamadoId"] = chamado.Id;
                return View(model);
            }

            // Só estes dois campos mudam. Id, UsuarioId, Status e datas ficam como estavam.
            chamado.Titulo = model.Titulo;
            chamado.Descricao = model.Descricao;

            await _context.SaveChangesAsync();

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = "Chamado atualizado com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // POST: /Chamados/Excluir/5
        // Exclusão definitiva, exclusiva de Support e Admin.
        // Nenhuma outra tabela referencia Chamado, então remover o registro
        // não deixa dados "órfãos" no banco.
        [HttpPost]
        [Authorize(Roles = Perfis.SupportOuAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Excluir(int id)
        {
            var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Chamado não encontrado. Ele pode já ter sido excluído.";
                return RedirectToAction(nameof(Index));
            }

            _context.Chamados.Remove(chamado);

            try
            {
                await _context.SaveChangesAsync();
            }
            catch (DbUpdateException)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Não foi possível excluir o chamado.";
                return RedirectToAction(nameof(Index));
            }

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = $"Chamado #{id} excluído com sucesso.";
            return RedirectToAction(nameof(Index));
        }

        // Regra de edição:
        // - Support/Admin editam qualquer chamado.
        // - User edita apenas chamado Aberto (a posse já foi garantida pelo VisiveisPara).
        private bool PodeEditar(Chamado chamado)
        {
            if (User.IsInRole(Perfis.Support) || User.IsInRole(Perfis.Admin))
            {
                return true;
            }

            return chamado.Status == ChamadoStatus.Aberto;
        }

        // As ações de atendimento abaixo são exclusivas de Support e Admin.
        // Um User que enviar o POST manualmente recebe "acesso negado" e o status não muda.

        // POST: /Chamados/IniciarAtendimento/5
        // Transição permitida: Aberto -> EmAndamento.
        [HttpPost]
        [Authorize(Roles = Perfis.SupportOuAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> IniciarAtendimento(int id)
        {
            var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            if (chamado.Status != ChamadoStatus.Aberto)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Só é possível iniciar o atendimento de um chamado Aberto.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            chamado.Status = ChamadoStatus.EmAndamento;

            await _context.SaveChangesAsync();

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = "Atendimento iniciado com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // POST: /Chamados/Resolver/5
        // Transição permitida: EmAndamento -> Resolvido.
        [HttpPost]
        [Authorize(Roles = Perfis.SupportOuAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Resolver(int id)
        {
            var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            if (chamado.Status != ChamadoStatus.EmAndamento)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Só é possível resolver um chamado que esteja Em Andamento.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            chamado.Status = ChamadoStatus.Resolvido;
            chamado.DataFechamento = DateTime.Now;

            await _context.SaveChangesAsync();

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = "Chamado resolvido com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // POST: /Chamados/Reabrir/5
        // Transição permitida: Resolvido -> EmAndamento.
        [HttpPost]
        [Authorize(Roles = Perfis.SupportOuAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Reabrir(int id)
        {
            var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            if (chamado.Status != ChamadoStatus.Resolvido)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Só é possível reabrir um chamado que esteja Resolvido.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            chamado.Status = ChamadoStatus.EmAndamento;
            chamado.DataFechamento = null;

            await _context.SaveChangesAsync();

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = "Chamado reaberto com sucesso.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }

        // POST: /Chamados/Cancelar/5
        // Transições permitidas: Aberto -> Cancelado ou EmAndamento -> Cancelado.
        [HttpPost]
        [Authorize(Roles = Perfis.SupportOuAdmin)]
        [ValidateAntiForgeryToken]
        public async Task<IActionResult> Cancelar(int id)
        {
            var chamado = await _context.Chamados.FirstOrDefaultAsync(c => c.Id == id);

            if (chamado == null)
            {
                return NotFound();
            }

            if (chamado.Status != ChamadoStatus.Aberto && chamado.Status != ChamadoStatus.EmAndamento)
            {
                TempData["MensagemTipo"] = "danger";
                TempData["Mensagem"] = "Este chamado não pode ser cancelado no status atual.";
                return RedirectToAction(nameof(Detalhes), new { id });
            }

            // O cancelamento não remove o registro do banco, apenas altera o status.
            chamado.Status = ChamadoStatus.Cancelado;

            await _context.SaveChangesAsync();

            TempData["MensagemTipo"] = "success";
            TempData["Mensagem"] = "Chamado cancelado.";
            return RedirectToAction(nameof(Detalhes), new { id });
        }
    }
}
