# Relatorio geral de erros do projeto

Data da analise: 20/08/2026  
Projeto: `HelpDeskMvc`  
Comando usado: `dotnet build`

## Resumo

O projeto nao compila atualmente. A compilacao terminou com **34 erros**. A maior parte dos erros e consequencia de duas causas-raiz:

1. Os arquivos com sufixo `-gui` definem as mesmas classes e modelos dos arquivos oficiais, gerando duplicidade de tipos e metodos.
2. A implementacao com Entity Framework Core foi adicionada parcialmente: existem `AppDbContext`, migrations e controllers que usam EF Core, mas o `.csproj`, o `appsettings.json` e o `Program.cs` nao estao configurados para essa infraestrutura.

O editor nao apresentou diagnosticos, mas o build do projeto reproduziu os erros descritos abaixo. Os arquivos `bin/` e `obj/` nao devem ser usados como fonte de configuracao, pois sao artefatos gerados.

## Erros confirmados na compilacao

### 1. Classes duplicadas por arquivos `-gui`

**Arquivos envolvidos:**

- `Controllers/ChamadosController.cs`
- `Controllers/ChamadosController-gui.cs`
- `Controllers/HomeController.cs`
- `Controllers/HomeController-gui.cs`
- `Models/chamado.cs`
- `Models/chamado-gui.cs`

**Erros observados:** `CS0101` e `CS0111`.

**Motivo:** os dois arquivos de cada par pertencem ao mesmo namespace e declaram a mesma classe. O compilador encontra duas definicoes de `ChamadosController`, duas de `HomeController` e duas de `Chamado`. Os metodos `Index`, `Detalhes`, `Privacy` e `Error` tambem ficam duplicados.

**Solucao recomendada:** escolher uma unica implementacao. Para manter a versao com banco, integrar o conteudo dos arquivos `-gui` nos nomes oficiais e remover ou mover os arquivos duplicados para fora do projeto. O mesmo deve ser feito com `Models/chamado-gui.cs`.

### 2. Pacotes do Entity Framework Core ausentes

**Arquivos que dependem dos pacotes:**

- `Controllers/ChamadosController-gui.cs`
- `Controllers/HomeController-gui.cs`
- `Data/AppDbContext.cs`
- `Migrations/20260730124757_CriacaoInicial.cs`
- `Migrations/20260730124757_CriacaoInicial.Designer.cs`
- `Migrations/AppDbContextModelSnapshot.cs`

**Erros observados:** `CS0234` para `Microsoft.EntityFrameworkCore` e `CS0246` para `DbContext`, `DbSet`, `Migration`, `MigrationBuilder`, `ModelSnapshot`, `ModelBuilder` e atributos relacionados.

**Motivo:** `HelpDeskMvc.csproj` contem apenas o SDK web e o target `net10.0`. Nao ha `PackageReference` para o provider SQLite nem para o pacote de design do EF Core. As referencias encontradas em `obj/` e `bin/` sao restos de uma restauracao anterior e nao substituem referencias no `.csproj`.

**Solucao recomendada:** na pasta raiz do projeto, executar:

```powershell
dotnet add package Microsoft.EntityFrameworkCore.Sqlite --version 10.0.10
dotnet add package Microsoft.EntityFrameworkCore.Design --version 10.0.10
```

Depois, executar `dotnet restore` e `dotnet build`. Usar a mesma versao compativel com o SDK instalado para os dois pacotes.

### 3. DbContext nao registrado na injecao de dependencia

**Arquivos envolvidos:** `Program.cs`, `Data/AppDbContext.cs`, `Controllers/ChamadosController-gui.cs` e `Controllers/HomeController-gui.cs`.

**Motivo:** os controllers da versao com banco recebem `AppDbContext` pelo construtor, mas `Program.cs` nao chama `AddDbContext`. Depois dos erros de compilacao, a aplicacao falhara ao criar esses controllers por falta de registro do servico.

**Solucao recomendada:** adicionar antes de `builder.Build()`:

```csharp
using HelpDeskMvc.Data;
using Microsoft.EntityFrameworkCore;

builder.Services.AddDbContext<AppDbContext>(options =>
    options.UseSqlite(builder.Configuration.GetConnectionString("DefaultConnection")));
```

### 4. Connection string ausente

**Arquivo envolvido:** `appsettings.json`.

**Motivo:** o arquivo atual possui apenas `Logging` e `AllowedHosts`. O provider SQLite precisa de `ConnectionStrings:DefaultConnection`.

**Solucao recomendada:** adicionar no mesmo nivel de `Logging`:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=helpdesk.db"
}
```

O arquivo `helpdesk.db` existe no workspace, mas sua presenca nao substitui a configuracao da aplicacao.

## Problemas funcionais encontrados

Estes itens nao aparecem como erros C# no build atual, mas impedem o funcionamento esperado da versao com banco.

### Views `-gui` nao sao selecionadas automaticamente

**Arquivos envolvidos:** `Views/Home/Index-gui.cshtml`, `Views/Chamados/Index-gui.cshtml`, `Views/Chamados/Detalhes-gui.cshtml` e `Views/Shared/_Layout-gui.cshtml`.

O MVC procura, por convencao, `Views/Home/Index.cshtml`, `Views/Chamados/Index.cshtml`, `Views/Chamados/Detalhes.cshtml` e `Views/Shared/_Layout.cshtml`. Arquivos com `-gui` no nome nao sao usados automaticamente.

**Efeito:** mesmo que os controllers `-gui` fossem compilados, o dashboard e as telas novas nao seriam exibidos pelas rotas padrao.

**Solucao:** escolher a interface desejada e substituir/consolidar as views nos caminhos convencionais. Manter duas versoes exige configuracao explicita de localizacao de views ou actions com nomes diferentes.

### Rotas oficiais ainda apontam para a implementacao antiga

**Arquivos envolvidos:** `Controllers/HomeController.cs`, `Controllers/ChamadosController.cs`, `Views/Home/Index.cshtml`, `Views/Chamados/Index.cshtml` e `Views/Chamados/Create.cshtml`.

Os controllers oficiais usam dados em memoria. O `HomeController.cs` nao fornece `DashboardViewModel`, e o `ChamadosController.cs` nao possui a action `Create`, embora exista a view `Views/Chamados/Create.cshtml` e a interface nova contenha links para essa rota.

**Efeito:** a pagina inicial continua sendo o template padrao, a lista nao consulta SQLite e `/Chamados/Create` tende a retornar 404.

**Solucao:** depois de resolver as duplicidades, manter como oficiais os controllers com EF Core e consolidar as views correspondentes nos caminhos convencionais.

### Migrations sem aplicacao garantida na inicializacao

**Arquivos envolvidos:** `Migrations/20260730124757_CriacaoInicial.cs`, `helpdesk.db` e `Program.cs`.

As migrations existem e o banco local foi gerado, mas `Program.cs` nao executa `Database.Migrate()`. Em um ambiente novo, a aplicacao pode abrir sem a tabela `Chamados` se a migration nao for aplicada manualmente.

**Solucao recomendada:** aplicar a migration durante o deploy ou executar:

```powershell
dotnet ef database update
```

Para desenvolvimento local, automatizar a migration no startup somente com uma decisao explicita da equipe, pois isso pode ser inadequado em producao.

## Sequencia de correcao sugerida

1. Escolher a versao oficial: em memoria ou SQLite/EF Core. Para o estado atual e as migrations existentes, a versao SQLite/EF Core e a mais consistente.
2. Consolidar os arquivos `-gui` nos nomes oficiais e eliminar as definicoes duplicadas.
3. Adicionar os pacotes EF Core ao `HelpDeskMvc.csproj`.
4. Adicionar `ConnectionStrings:DefaultConnection` ao `appsettings.json`.
5. Registrar `AppDbContext` com `UseSqlite` no `Program.cs`.
6. Consolidar as views `-gui` nos nomes esperados pelo MVC.
7. Restaurar, compilar e aplicar as migrations.

Comandos de verificacao:

```powershell
dotnet restore
dotnet build
dotnet ef database update
dotnet run
```

## Observacoes de manutencao

- `helpdesk.db` e um banco local; a equipe deve decidir se ele sera ignorado pelo Git e se somente as migrations serao versionadas.
- O roadmap registra um aviso anterior de vulnerabilidade relacionado a `SQLitePCLRaw.lib.e_sqlite3`. Depois de restaurar os pacotes, executar `dotnet list package --vulnerable` e atualizar dependencias se o aviso continuar.
- O projeto usa `net10.0`; SDK e pacotes precisam estar instalados em versoes compativeis com esse target.