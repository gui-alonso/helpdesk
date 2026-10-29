# Relatório de correções do projeto

Data da correção: 20/08/2026  
Projeto: `HelpDeskMvc`  
Documento de origem: `docs/relatorio-erros-projeto.md`

## Resultado

Os 34 erros de compilação registrados no relatório de origem foram eliminados. A implementação consolidada utiliza ASP.NET Core MVC, Entity Framework Core 10 e SQLite.

Validação final:

```text
Compilação com êxito.
0 Aviso(s)
0 Erro(s)
```

## Correções realizadas

### 1. Remoção das definições duplicadas

O conteúdo da implementação `-gui`, que já utilizava banco de dados, foi consolidado nos arquivos oficiais esperados pelo projeto. Os arquivos duplicados com sufixo `-gui` foram removidos.

Arquivos consolidados:

- `Controllers/ChamadosController.cs`
- `Controllers/HomeController.cs`
- `Models/chamado.cs`

Com isso, deixaram de existir as declarações duplicadas de `ChamadosController`, `HomeController`, `Chamado` e de suas actions, eliminando os erros `CS0101` e `CS0111`.

### 2. Configuração do Entity Framework Core

Foram adicionadas ao `HelpDeskMvc.csproj` as referências:

- `Microsoft.EntityFrameworkCore.Sqlite` 10.0.10
- `Microsoft.EntityFrameworkCore.Design` 10.0.10, marcado como dependência privada de desenvolvimento

Isso disponibilizou os tipos usados pelo contexto e pelas migrations, eliminando os erros `CS0234` e `CS0246`.

### 3. Correção da dependência SQLite vulnerável

Foi adicionada uma referência direta a `SQLitePCLRaw.bundle_e_sqlite3` 2.1.13. Essa versão substituiu a versão transitiva 2.1.11, que gerava o aviso `NU1903`.

A verificação final com `dotnet list HelpDeskMvc.csproj package --vulnerable --include-transitive` não encontrou pacotes vulneráveis nas fontes atuais.

### 4. Registro do contexto na injeção de dependência

O `Program.cs` passou a registrar `AppDbContext` por meio de `AddDbContext`, configurado com `UseSqlite` e a connection string `DefaultConnection`.

Assim, os controllers podem receber `AppDbContext` pelo construtor quando forem instanciados pelo ASP.NET Core.

### 5. Configuração da conexão SQLite

Foi adicionada ao `appsettings.json` a configuração:

```json
"ConnectionStrings": {
  "DefaultConnection": "Data Source=helpdesk.db"
}
```

### 6. Integração dos controllers com o banco

Os controllers oficiais agora utilizam a implementação baseada em Entity Framework Core:

- `ChamadosController` consulta a lista e os detalhes no SQLite e disponibiliza as actions `Create` para exibir e processar o formulário.
- `HomeController` consulta os indicadores e os três chamados mais recentes para montar o `DashboardViewModel`.
- O modelo `Chamado` mantém as propriedades persistidas e incorpora as validações de título e descrição necessárias ao formulário.

### 7. Consolidação das views convencionais

As interfaces `-gui` foram movidas para os caminhos que o MVC localiza por convenção:

- `Views/Home/Index.cshtml`
- `Views/Chamados/Index.cshtml`
- `Views/Chamados/Detalhes.cshtml`
- `Views/Shared/_Layout.cshtml`

Com isso, o dashboard, a listagem integrada ao banco, os detalhes e os links para criação passam a ser utilizados pelas rotas oficiais.

### 8. Limpeza de artefatos antigos

Foram removidos de `bin/` e `obj/` somente os artefatos gerados que ainda continham o sufixo `-gui`. Eles causavam importações duplicadas no MSBuild mesmo após a correção dos arquivos-fonte.

## Comandos de validação executados

```powershell
dotnet build
dotnet list HelpDeskMvc.csproj package --vulnerable --include-transitive
```

O comando `dotnet run` não foi executado. A migration também não foi aplicada automaticamente e o arquivo `helpdesk.db` não foi alterado; a execução e o teste funcional permanecem para validação manual, conforme solicitado.
