using System.Diagnostics;
using HelpDeskMvc.Data;
using HelpDeskMvc.Models;
using HelpDeskMvc.ViewModels;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskMvc.Controllers
{
    public class HomeController : Controller
    {
        private readonly ILogger<HomeController> _logger;
        private readonly AppDbContext _context;

        // Mesmo padrão do ChamadosController: o contexto entra pelo construtor.
        // O logger que já existia continua aqui — só ganhamos um segundo parâmetro.
        public HomeController(ILogger<HomeController> logger, AppDbContext context)
        {
            _logger = logger;
            _context = context;
        }

        // Dashboard exige login. Os números respeitam o perfil:
        // User vê só os próprios chamados; Support/Admin veem todos (RN10).
        [Authorize]
        public async Task<IActionResult> Index()
        {
            // Consulta base já filtrada. Nada vai ao banco ainda (é só um IQueryable).
            var chamados = _context.Chamados.VisiveisPara(User);

            var viewModel = new DashboardViewModel
            {
                // CountAsync() traduz para SELECT COUNT(*). Note que NÃO trazemos
                // os chamados para a memória para depois contar — a contagem
                // acontece dentro do banco e volta só um número.
                Total = await chamados.CountAsync(),

                // O predicado dentro do CountAsync vira o WHERE do SQL.
                Abertos = await chamados
                    .CountAsync(c => c.Status == ChamadoStatus.Aberto),

                EmAndamento = await chamados
                    .CountAsync(c => c.Status == ChamadoStatus.EmAndamento),

                Resolvidos = await chamados
                    .CountAsync(c => c.Status == ChamadoStatus.Resolvido),

                // Take(3) vira LIMIT 3 no SQLite. Só três linhas saem do banco,
                // não a tabela inteira.
                UltimosChamados = await chamados
                    .OrderByDescending(c => c.DataAbertura)
                    .Take(3)
                    .ToListAsync()
            };

            return View(viewModel);
        }

        public IActionResult Privacy()
        {
            return View();
        }

        [ResponseCache(Duration = 0, Location = ResponseCacheLocation.None, NoStore = true)]
        public IActionResult Error()
        {
            return View(new ErrorViewModel
            {
                RequestId = Activity.Current?.Id ?? HttpContext.TraceIdentifier
            });
        }
    }
}
