# SPEC-002 — Autenticação, cadastro e autorização por perfis

**Projeto:** HelpDeskMVC | **Data:** 28/10/2026 | **Status:** Proposta técnica

> O número 002 é provisório: validar numeração no repositório antes de merge.

## 1. Estado observado e premissas
Diagnóstico de 28/10/2026: ASP.NET Core MVC .NET 10, EF Core 10 com SQLite, `AppDbContext`, `Chamado`, `ChamadoStatus`, `HomeController`, `ChamadosController` e Razor Views. Não há Identity nem roles. Os chamados existentes não possuem proprietário segundo o diagnóstico. Os arquivos-fonte não foram anexados; nomes e interfaces novos abaixo são especificação proposta, não implementação confirmada.

## 2. Papéis
- `User`: cadastro público, login, abertura e consulta somente de chamados próprios; sem ações de atendimento.
- `Support`: tudo que User faz, consulta de todos os chamados, início, resolução, reabertura e cancelamento conforme regras atuais.
- `Admin`: tudo que Support faz, acesso à gestão de usuários e atribuição de roles.

### Matriz de autorização
| Recurso/operação | Anônimo | User | Support | Admin |
|---|---|---|---|---|
| GET/POST cadastro e login | Sim | Conforme fluxo | Conforme fluxo | Conforme fluxo |
| Logout POST | Não | Sim | Sim | Sim |
| Criar chamado | Não | Sim | Sim | Sim |
| Listar/visualizar próprios chamados | Não | Sim | Sim | Sim |
| Listar/visualizar todos os chamados | Não | Não | Sim | Sim |
| Alterar status | Não | Não | Sim | Sim |
| Administrar usuários/roles | Não | Não | Não | Sim |

## 3. Modelo de dados
### ApplicationUser
Classe `ApplicationUser : IdentityUser` (opcional: `NomeCompleto` obrigatório, limite 120). Identity mantém usuário, senha com hash, claims, logins, tokens e roles em suas tabelas padrão.

### Chamado
Adicionar:
- `string? UsuarioId` — FK para `AspNetUsers.Id`, opcional na primeira migração por causa de registros antigos.
- `ApplicationUser? Usuario` — navegação opcional.

Manter `Id`, `Titulo`, `Descricao`, `Status`, `DataAbertura` e `DataFechamento` com semântica atual. Para novos chamados, `UsuarioId` deve sempre ser preenchido no servidor. Chamados antigos sem usuário ficam visíveis somente para Support/Admin até regularização; não exibir a User.

### AppDbContext
Proposta: `AppDbContext : IdentityDbContext<ApplicationUser>`; preservar `DbSet<Chamado>`, conversão de enum para texto e todas as configurações preexistentes. Invocar `base.OnModelCreating(modelBuilder)`.

## 4. Rotas e telas
- `GET /Account/Login`: e-mail, senha, opção lembrar-me; link de cadastro.
- `POST /Account/Login`: validar credenciais; mensagem genérica em falha; validar returnUrl local.
- `GET /Account/Register`: nome, e-mail, senha, confirmação.
- `POST /Account/Register`: criar usuário com role fixa `User`; impedir roles enviadas pelo formulário.
- `POST /Account/Logout`: antiforgery, encerrar sessão.
- `GET /Account/AccessDenied`: acesso negado, sem vazamento de dados.
- `GET /Admin/Usuarios`: listar usuários e roles (Admin).
- `POST /Admin/Usuarios/AlterarPerfil`: mudar role após validação (Admin).

Nomes de rota são sugestões; alinhar ao roteamento existente. Preferir ViewModels `LoginViewModel`, `RegisterViewModel`, `CriarChamadoViewModel` e `AlterarPerfilViewModel`; não aceitar entidades de banco diretamente em formulários.

## 5. Regras de negócio
**RN01** Cadastro público cria apenas `User`.
**RN02** E-mail único e normalizado pelo Identity; exigir senha conforme política configurada.
**RN03** Usuário não autenticado é redirecionado ao login para páginas privadas.
**RN04** `User` recebe apenas chamados com `UsuarioId == UserManager.GetUserId(User)`; aplicar tanto em listagem quanto em detalhes.
**RN05** `Support` e `Admin` podem alterar estados apenas nas transições válidas já documentadas.
**RN06** O servidor define `UsuarioId`, `Status`, `DataAbertura` e `DataFechamento` no cadastro.
**RN07** Apenas `Admin` gerencia roles; impedir remoção/rebaixamento do último Admin ativo.
**RN08** Nenhum usuário pode promover a própria conta por campos de formulário, URL ou API.
**RN09** Chamados legados com `UsuarioId == null` não ficam disponíveis a usuários comuns.
**RN10** Indicadores e dados do dashboard respeitam o escopo de acesso.

## 6. Segurança
- Configurar `AddIdentity`/`AddDefaultIdentity` + roles conforme escolha de UI; evitar registrar esquemas de autenticação conflitantes.
- Usar `UseAuthentication()` antes de `UseAuthorization()`.
- Proteger controllers/actions com `[Authorize]` e `[Authorize(Roles = "Support,Admin")]` ou políticas equivalentes.
- Usar `[ValidateAntiForgeryToken]` nos POSTs e logout exclusivamente via POST.
- Cookies HttpOnly, Secure em HTTPS e SameSite apropriado; lockout por tentativas e mensagens que não revelem se e-mail existe.
- Nunca armazenar senha em texto puro nem inserir senha Admin no Git; usar User Secrets/variáveis de ambiente no bootstrap.
- Validar acesso a objeto no banco; ocultar botão não substitui autorização.
- Definir comportamento de não encontrado/acesso negado sem revelar chamados alheios.

## 7. Componentes/arquivos esperados
```text
Models/ApplicationUser.cs
Models/Chamado.cs                         (alterar)
Data/AppDbContext.cs                     (alterar)
Data/IdentitySeed.cs                     (novo, ou equivalente)
ViewModels/LoginViewModel.cs
ViewModels/RegisterViewModel.cs
ViewModels/CriarChamadoViewModel.cs      (novo, se ainda não existir)
ViewModels/AlterarPerfilViewModel.cs
Controllers/AccountController.cs
Controllers/ChamadosController.cs        (alterar)
Controllers/HomeController.cs            (alterar)
Areas/Admin/Controllers/UsuariosController.cs
Views/Account/Login.cshtml
Views/Account/Register.cshtml
Views/Account/AccessDenied.cshtml
Areas/Admin/Views/Usuarios/Index.cshtml
Views/Shared/_Layout.cshtml             (alterar)
Program.cs                                (alterar)
Migrations/                               (nova migration)
```
Estrutura ilustrativa; adaptar ao projeto real e evitar duplicação de arquivos existentes.

## 8. Cenários de aceite (Given/When/Then)
1. **Cadastro:** dado visitante válido, quando cadastra, então cria conta com role `User`, sem opção de elevação.
2. **Login:** dado usuário válido, quando autentica, então acessa recursos de sua role; logout invalida a sessão.
3. **Anônimo:** dado visitante sem sessão, quando acessa `/Chamados`, então é enviado ao login.
4. **Isolamento:** dado User A, quando solicita detalhes de chamado de User B, então não recebe os dados.
5. **Privilégio:** dado User, quando envia POST para iniciar/resolver chamado, então recebe bloqueio e o status permanece inalterado.
6. **Suporte:** dado Support, quando executa transição válida, então o status muda conforme fluxo existente.
7. **Admin:** dado Admin, quando altera role de outro usuário, então a alteração é persistida e auditável ao menos por logs de aplicação.
8. **Último Admin:** dado único Admin, quando tenta remover sua role, então a operação é recusada.
9. **Legado:** dado chamado anterior sem `UsuarioId`, quando User consulta a listagem, então ele não aparece; Support/Admin podem acessá-lo.
10. **Migração:** dado banco com chamados, quando migration é aplicada, então quantidade e campos existentes são preservados.
11. **Dashboard:** dado User, quando acessa indicadores, então não recebe dados de terceiros.

## 9. Implantação e validação
1. Fazer backup testado de `helpdesk.db` e manter cópia para rollback.
2. Criar migration do Identity + FK opcional; revisar SQL e testar em cópia.
3. Aplicar migration em ambiente de desenvolvimento; validar tabelas `AspNet*`, `Chamados` e FK.
4. Criar roles e Admin inicial por processo seguro e idempotente.
5. Testar com pelo menos quatro contas: User A, User B, Support e Admin.
6. Repetir testes das transições existentes e revisar logs.

## 10. Decisões pendentes
- Confirmar numeração da feature e padrão dos arquivos anteriores.
- Definir se o cadastro autentica imediatamente ou exige confirmação de e-mail (para MVP didático, login imediato é mais simples; não usar sem confirmação em produção pública sem avaliar abuso).
- Definir se Admin terá dashboard próprio nesta fase ou apenas tela de gestão.
- Definir política de atribuição de chamados antigos e se haverá edição de dados pessoais nesta fase.

**Branch sugerida:** `feature/002-auth-roles-identity`.
