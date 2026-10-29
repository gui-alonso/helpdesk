# Aula 1 — Configurando EF Core + MySQL no HelpDeskMvc

**Objetivo da aula:** deixar a aplicacao conectada ao MySQL usando EF Core (ORM), com estrutura do banco criada por migrations, sem precisar escrever SQL manual.

**Regra importante:** ao final desta aula os controllers podem continuar usando a lista estatica em memoria. A troca completa para o banco pode ficar para a aula seguinte, evitando quebrar o que ja funciona.

---

## O que voce ganha com esse fluxo ORM + Migrations

- A estrutura do banco nasce do codigo C# (Code First).
- Mudancas de schema viram historico versionado em `Migrations/`.
- O time inteiro recria o banco com os mesmos comandos.
- Quase todo o trabalho de DDL fica automatizado pelo EF Core.

Em resumo: voce modela em C#, roda comandos simples, e o EF Core gera/aplica as alteracoes no MySQL.

---

## Checklist antes da aula

Rode em uma maquina de teste do laboratorio:

```bash
dotnet --version
dotnet ef --version
```

- [ ] SDK do .NET 10 instalado
- [ ] `dotnet-ef` instalado
- [ ] MySQL Server instalado e em execucao
- [ ] MySQL Workbench (opcional, recomendado para visualizar tabelas)
- [ ] Acesso ao NuGet liberado
- [ ] Usuario MySQL com permissao para criar/alterar tabelas no schema da aula

---

## Passo 0 — Instalar/atualizar o CLI do EF Core

Ferramenta instalada uma vez por maquina:

```bash
dotnet tool install --global dotnet-ef
dotnet tool update --global dotnet-ef
dotnet ef --version
```

Se o terminal nao reconhecer `dotnet ef`, feche e abra o terminal novamente.

---

## Passo 1 — Trocar o provider: de SQLite para MySQL

Na raiz do projeto (onde esta o `.csproj`):

```bash
dotnet remove package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Por que Pomelo?

- E o provider mais usado com EF Core para MySQL/MariaDB em projetos academicos e corporativos.
- Funciona muito bem com migrations e `ServerVersion.AutoDetect`.

No `.csproj`, o resultado esperado e algo nessa linha:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.x">
    <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
    <PrivateAssets>all</PrivateAssets>
  </PackageReference>
  <PackageReference Include="Pomelo.EntityFrameworkCore.MySql" Version="10.0.x" />
</ItemGroup>
```

---

## Passo 2 — Confirmar que o Model esta pronto

No model `Chamado`, mantenha uma chave primaria por convencao:

```csharp
public int Id { get; set; }
```

Com isso, o EF Core reconhece a PK automaticamente. As anotacoes como `[Required]` e `[StringLength]` continuam validando formulario e tambem ajudam a definir colunas no banco.

---

## Passo 3 — Manter/validar o DbContext

O `AppDbContext` deve conter o `DbSet<Chamado>`:

```csharp
public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chamado> Chamados { get; set; }
}
```

---

## Passo 4 — Configurar a connection string do MySQL

No `appsettings.json`, adicione/ajuste o bloco `ConnectionStrings`:

```json
{
  "ConnectionStrings": {
    "DefaultConnection": "Server=localhost;Port=3306;Database=helpdeskmvc;User=root;Password=SUA_SENHA;"
  }
}
```

Se preferir, use outro usuario que nao seja `root`.

Dica para aula:

- Mostre que trocar de SQLite para MySQL muda principalmente a connection string e o provider.
- O restante da arquitetura com EF Core permanece praticamente igual.

---

## Passo 5 — Registrar o MySQL no Program.cs

Troque `UseSqlite(...)` por `UseMySql(...)`:

```csharp
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<HelpDeskMvc.Data.AppDbContext>(options =>
    options.UseMySql(
        builder.Configuration.GetConnectionString("DefaultConnection"),
        ServerVersion.AutoDetect(builder.Configuration.GetConnectionString("DefaultConnection"))));
```

O `ServerVersion.AutoDetect` detecta automaticamente a versao do servidor MySQL.

---

## Passo 6 — Se havia migration de SQLite, limpar base antiga da aula

Se o projeto ja tinha migrations de SQLite (como neste caso), faca limpeza antes de gerar migration MySQL:

1. Apague a pasta `Migrations/`.
2. Apague o arquivo local `helpdesk.db` (se existir).

Isso evita conflito de tipos/annotations especificos de SQLite em um banco MySQL.

Observacao: em projeto real com dados de producao, a estrategia de migracao entre providers exige planejamento. Para ambiente didatico, resetar e o caminho mais simples.

---

## Passo 7 — Gerar a migration inicial (sem SQL manual)

```bash
dotnet ef migrations add CriacaoInicialMySql
```

O EF Core vai gerar os arquivos na pasta `Migrations/`. Esse codigo representa o "roteiro" de criacao da estrutura.

---

## Passo 8 — Aplicar migration no MySQL

```bash
dotnet ef database update
```

Pronto: as tabelas sao criadas no schema `helpdeskmvc` conforme os modelos C# e migrations.

Sem SQL manual.

---

## Passo 9 — Validar rapidamente

```bash
dotnet build
dotnet run
```

Opcional no MySQL Workbench:

- Abra o schema `helpdeskmvc`.
- Verifique as tabelas da aplicacao.
- Verifique a tabela `__EFMigrationsHistory` com a migration aplicada.

---

## Fluxo de evolucao (proximas aulas)

Quando adicionar uma nova propriedade no model (ex.: `Prioridade`), o fluxo continua simples:

```bash
dotnet ef migrations add AdicionaPrioridadeChamado
dotnet ef database update
```

Isso mostra a forca do ORM com migrations: evolucao incremental do banco a partir do codigo.

---

## Erros comuns e solucao

| Sintoma | Causa provavel | Solucao |
|---|---|---|
| `dotnet ef: command not found` | Ferramenta global nao instalada | `dotnet tool install --global dotnet-ef` e reabrir terminal |
| `Unable to create a 'DbContext'...` | Falta provider ou registro no `Program.cs` | Revisar passos 1 e 5 |
| `Access denied for user` | Usuario/senha incorretos na connection string | Corrigir credenciais do MySQL |
| `Unknown database 'helpdeskmvc'` | Schema nao existe e usuario sem permissao de criar | Criar schema no Workbench ou ajustar permissoes |
| `Authentication method unknown` | Incompatibilidade de plugin de autenticacao | Ajustar usuario para metodo compativel com o conector |
| Migration criada, mas tabela nao aparece | Nao rodou `database update` no projeto certo | Rodar comando na pasta do `.csproj` |

---

## Comandos minimos para o aluno (resumo rapido)

```bash
dotnet add package Pomelo.EntityFrameworkCore.MySql
dotnet add package Microsoft.EntityFrameworkCore.Design
dotnet ef migrations add CriacaoInicialMySql
dotnet ef database update
dotnet run
```

Com esses comandos e configuracao correta da connection string, o EF Core faz o resto.