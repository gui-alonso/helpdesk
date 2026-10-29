# Aula 1 — Configurando EF Core + SQLite no HelpDeskMvc

**Objetivo da aula:** ter o banco de dados criado e a aplicação conectada a ele, **sem quebrar nada do que já funciona**.

**Regra importante:** ao final desta aula os controllers continuam usando a lista estática em memória. Isso é intencional. Trocar a fonte de dados é o assunto da Aula 2. Assim, se algo der errado hoje, o projeto ainda roda e ninguém sai da aula com a aplicação quebrada.

---

## Checklist antes da aula (fazer no laboratório, sem alunos)

Rode em uma máquina do laboratório e confirme cada item:

```bash
dotnet --version          # deve mostrar 10.x
dotnet ef --version       # se der "command not found", ver Passo 0
```

- [ ] SDK do .NET 10 instalado em todas as máquinas
- [ ] `dotnet-ef` instalado (ou os alunos têm permissão para instalar ferramenta global)
- [ ] DB Browser for SQLite instalado (opcional, mas recomendo muito — é o que torna o banco visível)
- [ ] Máquinas com acesso ao NuGet (se a rede da escola bloqueia, ver "Plano B" no final)

---

## Passo 0 — Instalar a ferramenta de linha de comando do EF Core

Essa ferramenta é o que permite gerar migrations. É instalada **uma vez por máquina**, não por projeto.

```bash
dotnet tool install --global dotnet-ef
```

Se já estiver instalada mas desatualizada:

```bash
dotnet tool update --global dotnet-ef
```

Confirme:

```bash
dotnet ef --version
```

> **Para explicar aos alunos:** `dotnet ef` não faz parte da aplicação. É uma ferramenta de desenvolvimento, como um martelo — não vai junto na entrega final, só serve para construir.

---

## Passo 1 — Adicionar os pacotes NuGet ao projeto

Na pasta raiz do projeto (onde está o arquivo `.csproj`):

```bash
dotnet add package Microsoft.EntityFrameworkCore.Sqlite
dotnet add package Microsoft.EntityFrameworkCore.Design
```

Para que serve cada um:

| Pacote | Papel |
|--------|-------|
| `...EntityFrameworkCore.Sqlite` | O "tradutor" do EF Core para o dialeto do SQLite. É o provider. |
| `...EntityFrameworkCore.Design` | Necessário para o `dotnet ef` conseguir ler o projeto e gerar as migrations. |

Confirme que o `.csproj` ficou com algo assim:

```xml
<ItemGroup>
  <PackageReference Include="Microsoft.EntityFrameworkCore.Sqlite" Version="10.0.0" />
  <PackageReference Include="Microsoft.EntityFrameworkCore.Design" Version="10.0.0" />
</ItemGroup>
```

> O número exato da versão pode variar. O `dotnet add package` sem `--version` já pega a mais recente compatível — deixe assim.

---

## Passo 2 — Garantir que o Model está pronto para virar tabela

Abra `Models/Chamado.cs`. Ele precisa de uma propriedade chamada `Id` (ou `ChamadoId`) para o EF Core reconhecer como chave primária por convenção.

```csharp
using System.ComponentModel.DataAnnotations;

namespace HelpDeskMvc.Models;

public class Chamado
{
    public int Id { get; set; }

    [Required(ErrorMessage = "O título é obrigatório.")]
    [StringLength(100, ErrorMessage = "O título deve ter no máximo 100 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "A descrição é obrigatória.")]
    [StringLength(1000)]
    public string Descricao { get; set; } = string.Empty;
}
```

Dois pontos que valem discussão em aula:

1. **`Id` sem `[Key]`.** O EF Core segue *convenção sobre configuração*: uma propriedade chamada `Id` ou `NomeDaClasseId` já é adotada como chave primária automaticamente. Sendo `int`, ele também a configura como auto-incremento. É exatamente isso que vai matar o contador manual de IDs em memória.
2. **`[StringLength]` agora vale duas coisas.** Antes só validava o formulário. Agora ele também define o tamanho da coluna no banco. Boa oportunidade para mostrar que a mesma anotação atua em duas camadas.

> Se o `Chamado` atual tiver outras propriedades, mantenha. Não adicione `Status` nem datas ainda — isso é a Aula 4, e o valor de fazer depois é justamente demonstrar uma **segunda migration**.

---

## Passo 3 — Criar o DbContext

Crie a pasta `Data` e dentro dela o arquivo `AppDbContext.cs`:

```csharp
using HelpDeskMvc.Models;
using Microsoft.EntityFrameworkCore;

namespace HelpDeskMvc.Data;

public class AppDbContext : DbContext
{
    public AppDbContext(DbContextOptions<AppDbContext> options)
        : base(options)
    {
    }

    public DbSet<Chamado> Chamados { get; set; }
}
```

**Como explicar o DbContext:** é a representação do banco de dados dentro do código C#. Cada `DbSet<T>` corresponde a uma tabela. A classe `Chamado` descreve *uma linha*; o `DbSet<Chamado>` representa *a tabela inteira*.

Uma analogia que costuma funcionar: se a classe `Chamado` é a planta de um apartamento, o `DbSet<Chamado>` é o prédio.

O construtor recebendo `DbContextOptions` é o que permite a injeção de dependência configurar a conexão de fora — nada de connection string escrita dentro da classe.

---

## Passo 4 — Declarar a connection string

Abra `appsettings.json` e adicione o bloco `ConnectionStrings` (no mesmo nível de `Logging`, não dentro dele):

```json
{
  "Logging": {
    "LogLevel": {
      "Default": "Information",
      "Microsoft.AspNetCore": "Warning"
    }
  },
  "AllowedHosts": "*",
  "ConnectionStrings": {
    "DefaultConnection": "Data Source=helpdesk.db"
  }
}
```

Note a simplicidade: `Data Source=helpdesk.db` e pronto. Sem servidor, sem usuário, sem senha, sem porta. Vale mostrar aos alunos uma connection string de SQL Server ao lado para eles verem o contraste — e entenderem que o resto do código não muda quando você troca.

---

## Passo 5 — Registrar o DbContext no Program.cs

Em `Program.cs`, **antes** de `var app = builder.Build();`:

```csharp
using HelpDeskMvc.Data;
using Microsoft.EntityFrameworkCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.AddControllersWithViews();

// --- NOVO: registro do banco de dados ---
builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));

var app = builder.Build();

// ... resto do arquivo permanece igual
```

**Conceito central da aula:** essa linha diz ao ASP.NET Core "quando alguém pedir um `AppDbContext`, construa um usando SQLite com essa connection string". Na Aula 2, o controller vai simplesmente pedir um `AppDbContext` no construtor e recebê-lo pronto — sem nunca dar `new`.

Se a turma ainda não viu injeção de dependência, esse é o momento de plantar a ideia. Não precisa esgotar o assunto hoje; na Aula 2 ela aparece na prática e o conceito se fecha.

---

## Passo 6 — Gerar a primeira migration

```bash
dotnet ef migrations add CriacaoInicial
```

Se der certo, apareceu uma pasta `Migrations/` com três arquivos. **Abra o arquivo `..._CriacaoInicial.cs` com a turma** — esse é o momento mais importante da aula.

Dentro dele há um método `Up()` com algo assim:

```csharp
migrationBuilder.CreateTable(
    name: "Chamados",
    columns: table => new
    {
        Id = table.Column<int>(type: "INTEGER", nullable: false)
            .Annotation("Sqlite:Autoincrement", true),
        Titulo = table.Column<string>(type: "TEXT", maxLength: 100, nullable: false),
        Descricao = table.Column<string>(type: "TEXT", maxLength: 1000, nullable: false)
    },
    constraints: table =>
    {
        table.PrimaryKey("PK_Chamados", x => x.Id);
    });
```

O que apontar:

- O EF Core **leu a classe C# e escreveu a estrutura da tabela sozinho**. É a definição prática de Code First.
- O `maxLength: 100` veio do `[StringLength(100)]`. A anotação virou coluna.
- Existe um método `Down()` que desfaz tudo. Migrations são versionadas e reversíveis — é controle de versão do banco de dados.
- **Nada foi criado no banco ainda.** A migration é só o roteiro. Ótima pergunta para lançar antes do próximo passo: "e agora, onde está o banco?"

---

## Passo 7 — Aplicar a migration (criar o banco de verdade)

```bash
dotnet ef database update
```

Agora sim: apareceu o arquivo `helpdesk.db` na raiz do projeto.

Abra no **DB Browser for SQLite** e mostre:

- a tabela `Chamados`, vazia, com as três colunas
- a tabela `__EFMigrationsHistory`, com uma linha registrando a migration aplicada

Essa segunda tabela merece atenção: é assim que o EF Core sabe quais migrations já rodaram. Por isso rodar `database update` duas vezes não duplica nada.

---

## Passo 8 — Proteger o repositório

Adicione ao `.gitignore`:

```gitignore
# Banco de dados local do SQLite
*.db
*.db-shm
*.db-wal
```

**Mas versione a pasta `Migrations/`.** Essa distinção é importante e vale explicar:

- As migrations são **código-fonte** — descrevem a estrutura. Vão para o Git.
- O arquivo `.db` são **dados locais** de cada aluno. Se for para o Git, você vai passar a próxima aula resolvendo conflito em arquivo binário.

Quem clonar o projeto roda `dotnet ef database update` e tem o banco recriado. Bom momento para mencionar que é exatamente assim que funciona em equipe de verdade.

---

## Passo 9 — Fechar a aula

```bash
dotnet build      # deve compilar sem erros
dotnet run        # a aplicação sobe e funciona igual antes
```

A aplicação está idêntica ao que era, ainda usando a lista em memória — e isso é o esperado. O que mudou é que **existe um banco pronto esperando para ser usado**.

Commit do dia:

```bash
git add .
git commit -m "Aula 1: configuracao do EF Core com SQLite e migration inicial"
git tag aula01
```

A tag é seu seguro. Quando alguém quebrar as migrations na Aula 4, `git checkout aula01` resolve.

**Gancho para a Aula 2:** "O banco existe e está vazio. A lista em memória continua funcionando e perdendo tudo a cada restart. Na próxima aula a gente liga os dois — e apaga a lista."

---

## Erros comuns e como resolver

| Sintoma | Causa | Solução |
|---------|-------|---------|
| `dotnet ef: command not found` | Ferramenta global não instalada | `dotnet tool install --global dotnet-ef` e reabrir o terminal |
| `Unable to create a 'DbContext' of type...` | Falta o pacote `Design`, ou o `AddDbContext` não foi adicionado no `Program.cs` | Revisar Passos 1 e 5 |
| `Your startup project doesn't reference Microsoft.EntityFrameworkCore.Design` | Comando rodado na pasta errada | `cd` até a pasta que contém o `.csproj` |
| `no such table: Chamados` | Migration criada mas não aplicada | Rodar `dotnet ef database update` |
| Arquivo `helpdesk.db` não aparece | Ainda não rodou o `database update`, ou está em outra pasta | Verificar Passo 7; o arquivo nasce na pasta de execução |
| `The entity type 'Chamado' requires a primary key` | Model sem propriedade `Id` | Revisar Passo 2 |
| Erro de build depois de adicionar os pacotes | Falta o `using Microsoft.EntityFrameworkCore;` | Adicionar no topo do `Program.cs` e do `AppDbContext.cs` |

Reserve os últimos 15 minutos da aula para essa tabela. Numa turma inteira, pelo menos três desses erros vão aparecer.

---

## Plano B se a rede da escola bloquear o NuGet

Duas alternativas, em ordem de preferência:

1. Você baixa os pacotes numa máquina com internet e configura uma **pasta local como fonte NuGet** no laboratório:
   ```bash
   dotnet nuget add source C:\PacotesOffline --name Local
   ```
2. Você prepara um projeto-base já com os pacotes restaurados e distribui como ponto de partida (pendrive ou pasta compartilhada). Nesse caso a aula começa no Passo 2.

Vale testar a rede antes — se o NuGet estiver bloqueado, descobrir isso no meio da aula custa o encontro inteiro.