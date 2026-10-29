# Manual — Autenticação e perfis (SPEC-002 / PLAN-002)

## Perfis

| Perfil | Pode fazer |
|---|---|
| `User` | Cadastrar-se, entrar, abrir chamados e ver **apenas os próprios** chamados. |
| `Support` | Tudo do User + ver **todos** os chamados e executar as ações de atendimento (iniciar, resolver, reabrir, cancelar). |
| `Admin` | Tudo do Support + gerenciar perfis em **Usuários** (`/Admin/Usuarios`). |

- O cadastro público (`/Account/Register`) cria sempre o perfil `User`.
- Chamados antigos (sem dono) aparecem só para Support/Admin.
- Ao alterar o perfil de alguém, a sessão dessa pessoa é encerrada em até 1 minuto; ela entra de novo já com o novo perfil.
- O sistema não permite rebaixar o último Admin.

## Senhas

As senhas **nunca** são gravadas em texto. O ASP.NET Core Identity grava apenas um *hash* (PBKDF2-HMAC-SHA512, 100 mil iterações, com *salt* aleatório) na coluna `AspNetUsers.PasswordHash`. No login, a senha digitada passa pelo mesmo processo e os resultados são comparados.

Política: mínimo de 8 caracteres, com maiúscula, minúscula e número. Após 5 tentativas erradas a conta fica bloqueada por 5 minutos.

## Primeiro Admin

As credenciais do primeiro Admin **não ficam no código nem no Git**. Elas são lidas na inicialização das chaves `AdminInicial:Email`, `AdminInicial:Senha` e `AdminInicial:Nome` (opcional). O Admin só é criado se ainda não existir nenhum.

Em desenvolvimento, use User Secrets (ficam fora da pasta do projeto):

```bash
dotnet user-secrets set "AdminInicial:Email" "admin@suaescola.com"
dotnet user-secrets set "AdminInicial:Senha" "UmaSenhaForte123"
dotnet user-secrets set "AdminInicial:Nome"  "Administrador"
dotnet run
```

Em outro ambiente, use variáveis de ambiente: `AdminInicial__Email`, `AdminInicial__Senha`, `AdminInicial__Nome`.

Se o e-mail informado já estiver cadastrado (por exemplo, como User), essa conta é promovida a Admin.

Depois que o Admin estiver criado, remova a senha da configuração:

```bash
dotnet user-secrets remove "AdminInicial:Senha"
```

Os demais perfis (Support, outros Admins) são definidos pelo Admin na tela **Usuários**.

## Banco de dados

A migration `AdicionaIdentityEPerfis` cria as tabelas `AspNet*` e a coluna opcional `Chamados.UsuarioId` (FK para `AspNetUsers`). Os chamados existentes são preservados.

```bash
dotnet ef database update
```

As roles (`User`, `Support`, `Admin`) são criadas automaticamente na inicialização da aplicação.

### Rollback

1. Pare a aplicação.
2. Volte para a migration anterior (remove as tabelas do Identity e a coluna `UsuarioId`):

   ```bash
   dotnet ef database update AlteraStatusParaEnum
   ```

3. Ou restaure o backup do arquivo `helpdesk.db` (copie-o por cima do atual e apague `helpdesk.db-wal` / `helpdesk.db-shm`, se existirem).

> **Atenção:** o rollback apaga todos os usuários e o vínculo dos chamados com seus donos.
