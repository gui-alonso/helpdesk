# PLAN-002 — Autenticação, cadastro e autorização por perfis

**Projeto:** HelpDeskMVC | **Data:** 28/10/2026 | **Status:** Planejado

> Numeração provisória: o diagnóstico menciona planos anteriores, mas não informa sua sequência. Confirmar o último número antes de integrar aos documentos do repositório.

## 1. Contexto
O diagnóstico de 28/10/2026 confirma ASP.NET Core MVC .NET 10, EF Core 10, SQLite, Razor Views e o fluxo básico de chamados. Não há autenticação, usuários ou perfis. A busca e os filtros ainda não funcionam. A análise foi estática, sem testes de execução.

## 2. Objetivo
Implementar login, cadastro, logout, autenticação por cookies e autorização para três perfis (`User`, `Support`, `Admin`), preservando dados e funcionalidades atuais.

## 3. Escopo
- ASP.NET Core Identity integrado ao `AppDbContext` existente e ao SQLite.
- `ApplicationUser : IdentityUser`, roles e inicialização idempotente de roles.
- Telas Razor de login, cadastro e acesso negado, alinhadas ao layout atual.
- Cadastro público sempre com perfil `User`.
- Criação inicial segura do primeiro administrador por configuração/secret de ambiente ou comando de bootstrap, nunca por credenciais fixas no código.
- Administração de usuários: listagem, alteração controlada de role e ativação/desativação de acesso (se adotada), somente Admin.
- Associar chamado ao usuário criador; aplicar escopo por proprietário ao User.
- Support/Admin consultam chamados e realizam transições de atendimento; User não altera estados de atendimento.
- Menus e dashboard adaptados ao perfil; validação de permissões obrigatória no servidor.
- Migrations, testes de autorização e documentação didática.

## 4. Fora do escopo
Recuperação de senha por e-mail, login social, anexos, notificações, auditoria avançada, paginação e correção completa de busca/filtros (tratar em feature separada).

## 5. Plano de execução
### Etapa 0 — Preparação e segurança dos dados
1. Inspecionar `Program.cs`, `AppDbContext`, `Chamado`, controllers, views, migrations e arquivos de configuração.
2. Confirmar sequência de nomenclatura e versão dos pacotes compatíveis com .NET 10.
3. Fazer backup do arquivo SQLite e testar restauração; registrar contagem de chamados.
4. Definir política para chamados antigos sem proprietário: manter `UsuarioId` opcional durante migração, restringir visualização a Support/Admin até atribuição explícita; não atribuir automaticamente a um usuário arbitrário.

### Etapa 1 — Identity e persistência
1. Adicionar pacotes Identity/EF Core SQLite compatíveis.
2. Criar `ApplicationUser` e adaptar `AppDbContext` para `IdentityDbContext<ApplicationUser>` preservando `DbSet<Chamado>` e configurações existentes.
3. Configurar Identity, cookie seguro, lockout, senha e autorização em `Program.cs`; ordenar `UseAuthentication()` antes de `UseAuthorization()`.
4. Criar migration e aplicar primeiro em cópia do banco; verificar preservação dos chamados.
5. Criar seed idempotente das roles; bootstrap de Admin com segredo fornecido fora do repositório.

### Etapa 2 — Interface de autenticação
1. Criar ViewModels específicos e validações para cadastro e login.
2. Implementar `AccountController` com GET/POST de login e cadastro, POST logout e tela de acesso negado.
3. Aplicar antiforgery, validação de retorno local (`Url.IsLocalUrl`), mensagens genéricas de falha e redirecionamentos por perfil.
4. Atualizar layout para exibir autenticação, usuário e logout.

### Etapa 3 — Autorização dos chamados
1. Adicionar `UsuarioId` e navegação para `ApplicationUser` em `Chamado`.
2. Criar ViewModel de abertura e atribuir `UsuarioId` a partir do usuário autenticado no servidor.
3. Proteger todas as actions de chamados, inclusive detalhes e POSTs de transição.
4. Filtrar listagem e detalhes para User; Support/Admin acessam todos.
5. Garantir que o dashboard não exponha contagens ou títulos de chamados de terceiros ao User.

### Etapa 4 — Administração
1. Criar área ou controller de usuários protegido por role Admin.
2. Listar usuários, consultar role e alterar perfil via POST protegido.
3. Impedir auto-rebaixamento do último administrador e escalonamento de privilégio por parâmetros do cliente.
4. Adaptar menus conforme roles, sem depender da ocultação visual para segurança.

### Etapa 5 — Qualidade e entrega
1. Testar cadastro User, login/logout, cookies, senhas inválidas, bloqueio de acesso anônimo e CSRF.
2. Testar acesso horizontal (User A não vê chamado de User B) e vertical (User não executa ações Support/Admin).
3. Testar transições de status e migração de chamados legados.
4. Executar `dotnet build`, `dotnet test` (quando houver projeto de testes), `dotnet ef migrations add <Nome>` e `dotnet ef database update` em ambiente controlado.
5. Atualizar README/docs com fluxo de criação do primeiro Admin, variáveis de ambiente e instruções de rollback.

## 6. Ordem recomendada para aulas
Aula 1: autenticação x autorização, Identity e banco. Aula 2: telas de cadastro/login. Aula 3: roles e proteção das actions. Aula 4: vínculo dos chamados e gestão Admin. Aula 5: testes de segurança e revisão.

## 7. Critérios de conclusão
- Três roles criadas e reconhecidas.
- Cadastro público não permite selecionar Support/Admin.
- Anônimo não acessa chamados; User vê apenas seus chamados.
- Support/Admin podem atender; somente Admin administra usuários.
- Chamados existentes preservados e protegidos.
- Rotas protegidas no backend e cenários críticos testados.

## 8. Riscos e mitigação
- **Perda de dados SQLite:** backup + ensaio de migration + rollback documentado.
- **Chamados sem dono:** vínculo opcional temporário e visibilidade restrita.
- **Admin inicial inseguro:** segredo em ambiente/secret manager e remoção de bootstrap após uso.
- **Quebra de controllers atuais:** refatoração incremental e testes de regressão.

**Branch sugerida:** `feature/002-auth-roles-identity` (ajustar número após verificar histórico).
