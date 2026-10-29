# HelpDeskMVC — Fase 1: Busca, filtros e preparação dos modelos

**Status:** Planejado  
**Projeto:** HelpDeskMVC — ASP.NET Core MVC (.NET 10), Razor Views, Entity Framework Core 10 e SQLite  
**Objetivo:** Evoluir a listagem de chamados e organizar os dados recebidos nos formulários, preservando as funcionalidades já existentes.

## 1. Contexto

De acordo com o diagnóstico de 08/10/2026, o projeto já dispõe de cadastro, listagem e detalhes de chamados, dashboard com indicadores e operações de transição de status. A listagem mostra elementos de busca e filtro, porém ainda não os aplica à consulta. O diagnóstico também registra que o cadastro recebe diretamente a entidade `Chamado` e que as regras de transição estão concentradas no `ChamadosController`.

Esta fase prepara a aplicação para as próximas etapas — autenticação e perfis de acesso — **sem implementá-las agora**.

## 2. Objetivos e entregáveis

1. Habilitar pesquisa de chamados por título e descrição.
2. Habilitar filtro por status, respeitando os valores do enum existente (`Aberto`, `EmAndamento`, `Resolvido`, `Cancelado`).
3. Permitir busca e filtro simultaneamente; preservar os valores selecionados após enviar o formulário.
4. Criar/adotar um ViewModel de entrada para abrir chamados, sem receber a entidade `Chamado` diretamente no POST.
5. Criar/adotar um ViewModel para exibir os filtros e os resultados da listagem.
6. Revisar e documentar as regras atuais de abertura, resolução, reabertura e cancelamento, sem alterar seu comportamento nesta fase.
7. Adicionar testes automatizados essenciais para busca, filtros e validação de cadastro, conforme a estrutura atual permitir.

## 3. Escopo

### Incluído
- Ajustes na action GET de listagem do controller de chamados.
- Formulário GET de busca e filtros na view de listagem.
- Consultas com LINQ/EF Core aplicadas ao banco antes de `ToListAsync()`.
- ViewModels dedicados a entrada e apresentação, reutilizando tipos existentes quando apropriado.
- Validações de título e descrição existentes, com mensagens de erro preservadas.
- Testes automatizados e verificação manual de regressão.
- Atualização breve da documentação do fluxo.

### Fora do escopo
- Login, cadastro de usuários, roles, autorização e painéis administrativos.
- Upload de anexos, comentários e histórico de atendimento.
- Categorias, prioridade, paginação e atribuição de atendente.
- Migração de SQLite para outro banco.
- Mudança de layout geral, redesign ou reestruturação completa dos controllers.
- Mudança das transições de status ou da semântica de `DataFechamento` (apenas documentar a situação atual).

## 4. Sequência de implementação

### Etapa 0 — Inventário e proteção do comportamento atual
- [ ] Confirmar nomes e caminhos reais do controller, views, entidade, enum e eventuais ViewModels já existentes.
- [ ] Inspecionar a action GET de listagem e o formulário de criação; verificar quais filtros já existem na interface.
- [ ] Confirmar as validações atuais do cadastro e as transições de status.
- [ ] Criar branch de trabalho e registrar estado de referência; fazer backup do arquivo `helpdesk.db` antes de mudanças que possam afetar dados.

**Saída:** lista dos arquivos que serão alterados e verificação do fluxo atual.

### Etapa 1 — Preparar os ViewModels
- [ ] Verificar se já existe `CriarChamadoViewModel` (ou equivalente); **reutilizá-lo** se adequado, evitando classe duplicada.
- [ ] Fazer o POST de criação receber o ViewModel, validar `ModelState` e mapear explicitamente somente título e descrição para uma nova entidade `Chamado`.
- [ ] Manter status e datas sob controle do servidor.
- [ ] Criar `ListarChamadosViewModel` (nome sugerido), com `Busca`, `Status` e `Chamados`.
- [ ] Ajustar as views e seus `asp-for`/`asp-validation-for` conforme necessário.

**Saída:** contrato de entrada seguro para criação e modelo de apresentação da listagem.

### Etapa 2 — Implementar busca e filtro de status
- [ ] Na action GET, receber parâmetros opcionais `busca` e `status`.
- [ ] Compor a consulta de `Chamado` a partir de `IQueryable` usando `AsNoTracking()`.
- [ ] Aplicar busca por título **ou** descrição quando houver texto não vazio.
- [ ] Validar o valor de status recebido e aplicar filtro apenas para valores definidos do enum.
- [ ] Manter a ordenação atual de mais recente para mais antigo por data de abertura.
- [ ] Materializar a consulta após os filtros com `ToListAsync()`.
- [ ] Exibir estado vazio adequado quando nenhum resultado corresponder.

**Saída:** listagem responde corretamente à combinação de busca e filtro.

### Etapa 3 — Integrar a interface
- [ ] Tornar funcional o campo de busca existente e o seletor de status.
- [ ] Usar `method="get"` para produzir URL compartilhável e não alterar dados.
- [ ] Manter os valores enviados preenchidos após o retorno da pesquisa.
- [ ] Adicionar ação **Limpar filtros** que retorne à listagem sem query string.
- [ ] Conferir funcionamento em telas menores sem redesign geral.

**Saída:** interface consistente com a consulta implementada.

### Etapa 4 — Testes e documentação
- [ ] Testar listagem sem filtros.
- [ ] Testar pesquisa por título e por descrição, inclusive resultados inexistentes.
- [ ] Testar cada valor do enum de status e a combinação com a pesquisa textual.
- [ ] Testar status ausente/inválido e busca vazia.
- [ ] Testar abertura válida e inválida de chamado e conferir os valores definidos no servidor.
- [ ] Executar testes de regressão do fluxo: abrir, visualizar, iniciar, resolver, reabrir e cancelar.
- [ ] Documentar regras atuais das transições e observação sobre `DataFechamento`.
- [ ] Rodar `dotnet build` e testes automatizados, caso adicionados.

**Saída:** comportamento validado e documentado.

## 5. Arquivos envolvidos (a confirmar no repositório)

| Arquivo/pasta provável | Intervenção proposta |
|---|---|
| `Controllers/ChamadosController.cs` | Ajustar GET da listagem e POST de criação |
| `Models/Chamado.cs` | Consultar contrato atual; evitar alterações desnecessárias |
| Enum de status em `Models/` | Reutilizar enum existente |
| `ViewModels/CriarChamadoViewModel.cs` **ou** equivalente | Reutilizar ou criar conforme estrutura real |
| `ViewModels/ListarChamadosViewModel.cs` | Criar, caso ainda não exista |
| `Views/Chamados/Index.cshtml` **ou** view equivalente | Ativar busca, filtro e estado vazio |
| `Views/Chamados/Create.cshtml` **ou** equivalente | Ajustar model binding de criação |
| Projeto de testes (novo, se ausente) | Cobrir cenários essenciais |
| `docs/` | Registrar comportamento e decisões |

**Observação:** os caminhos acima são sugestões de implementação, não arquivos confirmados pelo diagnóstico. A estrutura real deve ser conferida antes de editar.

## 6. Dependências e cuidados

- **Banco:** a fase não requer novas entidades persistidas nem novas colunas; **não criar migration** salvo se uma necessidade concreta for identificada durante a revisão.
- **Pesquisa SQLite:** escolher uma tradução LINQ compatível com EF Core/SQLite. Verificar comportamento de maiúsculas/minúsculas e caracteres acentuados; não prometer equivalência entre buscas acentuadas e não acentuadas sem implementar normalização.
- **Desempenho:** filtrar e ordenar no banco antes da materialização; paginação fica para outra fase.
- **Segurança:** não aceitar `Id`, `Status`, datas ou demais campos internos no contrato de abertura de chamado.
- **Compatibilidade:** não mudar rotas públicas nem operações existentes sem necessidade.
- **Persistência:** não apagar o banco SQLite durante limpeza ou execução de testes.

## 7. Critérios de conclusão

A fase estará concluída quando:

1. A listagem sem filtros continuar exibindo todos os chamados na ordem atual.
2. Busca textual e filtro por status funcionarem separadamente e em conjunto.
3. A interface preservar os filtros aplicados, oferecer limpeza e tratar resultados vazios.
4. O POST de criação utilizar um ViewModel próprio, validar entradas e definir os campos controlados pelo servidor.
5. Os fluxos já implementados continuarem funcionando.
6. O projeto compilar e os testes adicionados passarem.
7. Nenhuma funcionalidade de autenticação, upload ou administração tiver sido incluída nesta fase.

## 8. Organização sugerida do trabalho com a turma

- **Aula/bloco 1:** leitura da estrutura MVC e criação/revisão de ViewModels.
- **Aula/bloco 2:** busca e filtros com LINQ e EF Core.
- **Aula/bloco 3:** integração Razor, validação e estados vazios.
- **Aula/bloco 4:** testes, revisão de regressões e documentação.

Os blocos indicam uma ordem didática, não um prazo obrigatório.

## 9. Próxima fase

Após validar esta etapa, planejar **Fase 2 — Autenticação e cargos**, com ASP.NET Core Identity, roles `User`, `Support` e `Admin` e vínculo dos chamados ao solicitante.
