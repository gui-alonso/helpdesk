# HelpDeskMVC — Resumo do estado atual

**Data da análise:** 25/08/2026  
**Branch analisada:** `feature/aula-integracao-db`

## 1. Visão geral

O HelpDeskMVC é uma aplicação web educacional para registro e consulta de chamados de suporte. O projeto segue o padrão **ASP.NET Core MVC** e já passou da etapa de armazenamento em memória: os chamados são persistidos em um banco **SQLite** usando **Entity Framework Core**.

O fluxo principal atualmente permite:

- acessar um dashboard inicial;
- visualizar indicadores de chamados;
- abrir um novo chamado;
- validar título e descrição do formulário;
- salvar o chamado no banco de dados;
- listar os chamados por data de abertura, do mais recente para o mais antigo;
- consultar os detalhes de um chamado existente.

## 2. Estrutura e tecnologias

- **Framework:** ASP.NET Core MVC;
- **Target framework:** .NET 10 (`net10.0`);
- **Persistência:** Entity Framework Core 10;
- **Banco de dados:** SQLite, no arquivo `helpdesk.db`;
- **Interface:** Razor Views com Bootstrap;
- **Organização:** Controllers, Models, ViewModels, Data, Views e Migrations;
- **Configuração:** conexão com o banco definida em `appsettings.json`;
- **Controle de versão:** trabalho atual na branch `feature/aula-integracao-db`.

O `AppDbContext` expõe a coleção `Chamados`, e a migration inicial cria a tabela `Chamados` com chave primária, título, descrição, status, data de abertura e data de fechamento.

## 3. Funcionalidades implementadas

### Dashboard

A página inicial usa `DashboardViewModel` para exibir:

- quantidade total de chamados;
- quantidade de chamados abertos;
- quantidade em andamento;
- quantidade resolvida;
- os três chamados mais recentes.

As contagens e a consulta dos últimos chamados são executadas de forma assíncrona diretamente no banco.

### Cadastro de chamados

A tela `Create` possui formulário para título e descrição. O modelo `Chamado` aplica validação obrigatória por Data Annotations. No envio válido, o servidor define o identificador, o status inicial como `Aberto` e a data de abertura antes de salvar a entidade.

### Listagem e detalhes

A tela `Index` apresenta os chamados em tabela, com identificador, título, descrição, status, data de abertura e link para detalhes. A action `Detalhes` consulta o registro pelo identificador e retorna `NotFound` quando ele não existe.

## 4. Pontos positivos observados

- Separação de responsabilidades compatível com MVC.
- Uso de injeção de dependência para `AppDbContext` e `ILogger`.
- Consultas com `async`/`await` e métodos do EF Core adequados ao acesso ao banco.
- Validação básica de entrada no modelo e proteção antifalsificação no POST.
- Uso de ViewModel no dashboard, evitando concentrar dados em `ViewBag` ou `ViewData`.
- Migration inicial versionada, tornando a estrutura do banco reproduzível.
- Documentação de regras e workflow de desenvolvimento já presente em `docs/`.

## 5. Limitações e próximos cuidados

- Não há actions ou telas para editar um chamado, alterar seu status ou registrar a resolução.
- Os campos de busca e filtro da listagem estão presentes visualmente, mas ainda não enviam parâmetros nem filtram os dados.
- Não há autenticação, autorização ou associação de chamados a usuários.
- O status é armazenado como texto livre; isso pode gerar inconsistências de grafia conforme o sistema crescer.
- O projeto não apresenta testes automatizados de controller, persistência ou fluxo web.
- A listagem carrega todos os chamados de uma vez e ainda não possui paginação.
- O tratamento de falhas de banco e mensagens de sucesso/erro para o usuário pode ser aprimorado.

## 6. Sugestões de novas funcionalidades

### 6.1 Gestão completa do ciclo de vida do chamado

Adicionar edição e alteração controlada de status, por exemplo: `Aberto`, `Em andamento`, `Resolvido` e `Cancelado`. Ao marcar um chamado como resolvido, o sistema deve preencher `DataFechamento`; ao reabri-lo, deve limpar essa data.

**Benefícios:** completa o fluxo operacional, dá utilidade ao campo `DataFechamento` e torna os indicadores do dashboard mais confiáveis.

### 6.2 Busca, filtros e paginação reais

Transformar os controles atuais em filtros funcionais por texto, status e período de abertura. A consulta deve ser feita no banco e a listagem deve usar paginação para evitar carregar todos os registros quando a base aumentar.

**Benefícios:** melhora a navegação e prepara o projeto para um volume maior de chamados.

### 6.3 Usuários, perfis e atribuição

Criar autenticação com perfis de solicitante e atendente. O solicitante poderia acompanhar seus próprios chamados, enquanto o atendente poderia assumir chamados, alterar status e registrar observações.

**Benefícios:** aproxima o sistema de um cenário real de help desk e estabelece controle de acesso adequado.

### 6.4 Histórico e anexos do chamado

Adicionar uma tabela de histórico para registrar alterações de status, comentários e responsável pela ação. Como extensão, permitir anexos de evidências, como imagens de erro ou documentos.

**Benefícios:** melhora a rastreabilidade do atendimento e reduz a perda de contexto durante a resolução.

## 7. Ordem recomendada de evolução

1. Implementar alteração de status e edição, aproveitando os campos já existentes.
2. Tornar busca e filtro funcionais e adicionar paginação.
3. Criar testes automatizados para cadastro, consulta e transições de status.
4. Adicionar usuários e permissões.
5. Evoluir o histórico e os anexos conforme a necessidade do projeto.

## 8. Verificação realizada

A compilação foi executada em 25/08/2026 com o comando `dotnet build` e foi concluída com êxito para o target `net10.0`.

Não foram identificados testes automatizados no escopo analisado; por isso, ainda é recomendável validar manualmente a inicialização da aplicação, a conexão com o SQLite, o cadastro, a listagem e a consulta de detalhes.
