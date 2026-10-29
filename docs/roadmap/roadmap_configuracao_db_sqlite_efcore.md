# Roadmap — Configuracao do Banco com EF Core + SQLite

## Visao geral da mudanca

Este roadmap descreve a etapa em que o projeto saiu de um cenario 100% em memoria para um cenario com infraestrutura de banco pronta, usando EF Core com SQLite.

No historico recente da branch, essa mudanca aparece principalmente no commit de configuracao da aula (mensagem: configuracao do EF Core com SQLite e migration inicial) e no commit que adiciona o arquivo do banco local.

Objetivo tecnico alcancado nesta etapa:

- Registrar o EF Core no projeto.
- Definir o contexto de dados da aplicacao.
- Criar a primeira migration.
- Aplicar a migration e gerar o arquivo local do banco.
- Deixar a aplicacao preparada para controllers consultarem e gravarem dados reais.

---

## Arquivos impactados nesta mudanca

- HelpDeskMvc.csproj
- Program.cs
- appsettings.json
- Data/AppDbContext.cs
- Migrations/20260730124757_CriacaoInicial.cs
- Migrations/20260730124757_CriacaoInicial.Designer.cs
- Migrations/AppDbContextModelSnapshot.cs
- helpdesk.db
- docs/manual-config-db.md

---

## Passo a passo detalhado

### Passo 1 — Adicao dos pacotes do EF Core

No arquivo HelpDeskMvc.csproj, foram adicionadas as referencias de pacotes:

- Microsoft.EntityFrameworkCore.Sqlite
- Microsoft.EntityFrameworkCore.Design

Por que isso foi necessario:

- O pacote Sqlite permite que o EF Core fale o dialeto SQL do SQLite.
- O pacote Design habilita comandos de scaffolding e migration (dotnet ef).

Resultado:

- O projeto passa a ter capacidade de mapear entidades em tabelas e gerar migrations.

### Passo 2 — Criacao do AppDbContext

Foi criado Data/AppDbContext.cs com heranca de DbContext e DbSet de Chamados.

Ponto central:

- O DbSet Chamados representa a tabela Chamados no banco.

Resultado:

- O dominio da aplicacao passa a ter um ponto unico de acesso aos dados persistidos.

### Passo 3 — Definicao da connection string

No appsettings.json foi adicionado o bloco ConnectionStrings com DefaultConnection apontando para Data Source=helpdesk.db.

Por que isso importa:

- Separa configuracao de infraestrutura do codigo de negocio.
- Facilita troca futura de provider (por exemplo, SQL Server ou MySQL) com menos alteracoes no codigo.

Resultado:

- A aplicacao ganha uma configuracao explicita de onde fica o banco.

### Passo 4 — Registro do DbContext na injecao de dependencia

No Program.cs foi registrado AddDbContext usando UseSqlite e a connection string DefaultConnection.

Por que isso importa:

- Controllers e servicos podem receber AppDbContext no construtor sem instanciacao manual.
- Garante ciclo de vida correto do contexto por requisicao.

Resultado:

- A infraestrutura de dados fica integrada ao pipeline do ASP.NET Core.

### Passo 5 — Geracao da migration inicial

Foi criada a migration CriacaoInicial na pasta Migrations.

O que a migration mostra:

- Criacao da tabela Chamados.
- Chave primaria Id com autoincremento no SQLite.
- Colunas de titulo, descricao, status e datas.

Resultado:

- O esquema do banco passa a ser versionado em codigo.

### Passo 6 — Aplicacao da migration no banco

Com dotnet ef database update, a migration foi aplicada e o arquivo helpdesk.db foi criado/atualizado.

Resultado:

- O banco local fica materializado e pronto para leitura/escrita pela aplicacao.

### Passo 7 — Documentacao da configuracao

Foi criado docs/manual-config-db.md com o roteiro didatico completo da configuracao.

Por que isso agrega valor:

- Facilita replicacao da configuracao no laboratorio.
- Apoia aula e onboarding sem depender de memoria oral.

---

## Estado final apos esta mudanca

A aplicacao ficou preparada para persistir dados em banco.

Importante: esta etapa sozinha prepara a infraestrutura. A substituicao efetiva da lista em memoria por consultas no banco e o que acontece nas mudancas funcionais dos controllers (documentadas no outro roadmap).

---

## Beneficios tecnicos obtidos

- Persistencia real dos chamados.
- Controle de schema por migration.
- Base para evolucao de CRUD completo.
- Melhor separacao entre configuracao e regra de negocio.

---

## Pontos de atencao apos a mudanca

- O arquivo helpdesk.db foi adicionado; em equipes, normalmente o ideal e versionar migrations e ignorar banco local no git para evitar conflitos binarios.
- Ha aviso de vulnerabilidade do pacote SQLitePCLRaw.lib.e_sqlite3 (NU1903) no build; vale planejar atualizacao de dependencias.
- O redirecionamento HTTPS mostra warning de porta quando a execucao ocorre so em HTTP; nao bloqueia a funcionalidade, mas convem padronizar perfil de execucao.

---

## Proxima evolucao recomendada

1. Consolidar politica de versionamento do banco local (.gitignore para arquivos .db quando aplicavel).
2. Criar camada de servico/repositorio se o controller crescer muito.
3. Avancar para operacoes de edicao, fechamento e exclusao de chamado com validacao de regra de negocio.
