using HelpDeskMvc.Data;
using HelpDeskMvc.Models;
using Microsoft.AspNetCore.Identity;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

// Add services to the container.
builder.Services.AddControllersWithViews();
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

// Identity: cadastro, login, hash de senha e roles, gravando no mesmo AppDbContext.
builder.Services.AddIdentity<ApplicationUser, IdentityRole>(options =>
    {
        // Política de senha.
        options.Password.RequiredLength = 8;
        options.Password.RequireDigit = true;
        options.Password.RequireLowercase = true;
        options.Password.RequireUppercase = true;
        options.Password.RequireNonAlphanumeric = false;

        // Bloqueio temporário após várias senhas erradas (dificulta "chutar" senhas).
        options.Lockout.MaxFailedAccessAttempts = 5;
        options.Lockout.DefaultLockoutTimeSpan = TimeSpan.FromMinutes(5);
        options.Lockout.AllowedForNewUsers = true;

        options.User.RequireUniqueEmail = true;

        // MVP didático: login logo após o cadastro, sem confirmação de e-mail.
        options.SignIn.RequireConfirmedAccount = false;
    })
    .AddEntityFrameworkStores<AppDbContext>()
    .AddDefaultTokenProviders()
    .AddErrorDescriber<IdentityErrorDescriberPtBr>();

// Cookie de autenticação.
builder.Services.ConfigureApplicationCookie(options =>
{
    options.LoginPath = "/Account/Login";
    options.LogoutPath = "/Account/Logout";
    options.AccessDeniedPath = "/Account/AccessDenied";

    options.Cookie.Name = "HelpDeskMvc.Auth";
    options.Cookie.HttpOnly = true;                                   // JavaScript não consegue ler o cookie
    options.Cookie.SecurePolicy = CookieSecurePolicy.SameAsRequest;   // Secure quando acessado via HTTPS
    options.Cookie.SameSite = SameSiteMode.Lax;

    options.ExpireTimeSpan = TimeSpan.FromHours(8);
    options.SlidingExpiration = true;
});

// Revalida o cookie a cada 1 minuto: se o Admin mudar o perfil de alguém,
// a sessão antiga dessa pessoa é encerrada rapidamente (ela entra de novo já com o novo perfil).
builder.Services.Configure<SecurityStampValidatorOptions>(options =>
    options.ValidationInterval = TimeSpan.FromMinutes(1));

var app = builder.Build();

// Cria as roles e o primeiro Admin (se configurado). Requer o banco já migrado.
await IdentitySeed.InicializarAsync(app.Services);

// Configure the HTTP request pipeline.
if (!app.Environment.IsDevelopment())
{
    app.UseExceptionHandler("/Home/Error");
    // The default HSTS value is 30 days. You may want to change this for production scenarios, see https://aka.ms/aspnetcore-hsts.
    app.UseHsts();
}

app.UseHttpsRedirection();
app.UseRouting();

// A ordem importa: primeiro descobre QUEM é o usuário, depois verifica O QUE ele pode fazer.
app.UseAuthentication();
app.UseAuthorization();

app.MapStaticAssets();

// Rota das Areas (ex.: /Admin/Usuarios). Deve vir antes da rota padrão.
app.MapControllerRoute(
    name: "areas",
    pattern: "{area:exists}/{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();

app.MapControllerRoute(
    name: "default",
    pattern: "{controller=Home}/{action=Index}/{id?}")
    .WithStaticAssets();


app.Run();
