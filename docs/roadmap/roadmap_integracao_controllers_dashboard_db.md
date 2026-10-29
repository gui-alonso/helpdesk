# Roadmap — Integracao de Controllers e Dashboard com Banco

## Visao geral da mudanca

Este roadmap cobre a evolucao funcional apos a infraestrutura do banco ficar pronta: controllers e tela inicial passaram a consumir dados reais do SQLite via EF Core.

Em termos praticos, o projeto deixou de depender de lista estatica para leitura e cadastro de chamados, e o dashboard da Home passou a refletir metricas reais.

Objetivo tecnico alcancado nesta etapa:

- Trocar fluxo de chamados em memoria por acesso ao AppDbContext.
- Implementar cadastro de chamado com persistencia em banco.
- Exibir metricas reais e ultimos chamados na Home usando ViewModel tipado.
- Ajustar a view da Home para renderizacao dinamica sem erros Razor.

---

## Arquivos impactados nesta mudanca

- Controllers/ChamadosController.cs
- Controllers/HomeController.cs
- ViewModels/DashboardViewModel.cs
- Views/Home/Index.cshtml
- Views/Chamados/Index.cshtml
- Views/Chamados/Create.cshtml
- Views/Chamados/Detalhes.cshtml

---

## Passo a passo detalhado

### Passo 1 — Refatoracao do ChamadosController para EF Core

O controller passou a receber AppDbContext por injeção de dependencia no construtor.

Antes:

- Lista estatica de Chamado no proprio controller.
- Contador manual de Id.

Depois:

- Fonte de dados vindo de _context.Chamados.
- Id gerado pelo banco.

Resultado:

- O controller para de gerenciar estado em memoria e passa a usar persistencia real.

### Passo 2 — Ajuste da action Index de Chamados

A action Index virou assincrona e usa consulta no banco com ordenacao por DataAbertura decrescente.

Resultado:

- Lista de chamados passa a refletir exatamente o que esta persistido no banco.

### Passo 3 — Ajuste da action Detalhes de Chamados

A action Detalhes passou a buscar por Id com FirstOrDefaultAsync em _context.Chamados.

Comportamento:

- Se nao encontrar o Id, retorna NotFound.
- Se encontrar, renderiza a view de detalhes.

Resultado:

- A tela de detalhes fica consistente com a mesma fonte de dados da listagem.

### Passo 4 — Implementacao efetiva do Create com persistencia

Fluxo Create mantido em duas actions:

- GET Create: devolve formulario vazio.
- POST Create: valida ModelState, seta campos de servidor e salva.

No POST, os pontos importantes:

- Id forçado para 0 antes do Add, garantindo geracao pelo banco.
- Status inicial definido no servidor como Aberto.
- DataAbertura e DataFechamento controladas no servidor.
- SaveChangesAsync executa o INSERT.

Resultado:

- O cadastro deixa de ser temporario e passa a persistir entre reinicios da aplicacao.

### Passo 5 — HomeController com dados reais para dashboard

A Home deixou de ser estatica e passou a montar DashboardViewModel com consultas ao banco:

- Total de chamados.
- Quantidade por status (Aberto, Em andamento, Resolvido).
- Ultimos 3 chamados por data de abertura.

Resultado:

- A Home vira um painel real de acompanhamento, nao apenas mock visual.

### Passo 6 — Criacao do DashboardViewModel

Foi criado um ViewModel proprio para a tela inicial, contendo:

- Campos numericos para os cards.
- Lista de UltimosChamados.

Por que foi a escolha correta:

- Evita ViewBag/ViewData sem tipagem.
- Torna a view mais segura e clara em compilacao.

Resultado:

- Melhor separacao entre dados da tela e entidade de dominio.

### Passo 7 — Atualizacao da View Home/Index para modelo tipado

A view passou a usar @model DashboardViewModel e renderizar:

- Cards com contadores vindos do banco.
- Lista de ultimos chamados com fallback quando nao houver dados.
- Badge de status por switch expression.

Resultado:

- UX mais informativa e aderente ao estado real do sistema.

### Passo 8 — Correcao de sintaxe Razor na Home

Durante a alteracao da Home ocorreu o erro RZ1010 (Unexpected { after @) dentro do foreach.

Causa:

- Uso de @{ ... } dentro de bloco que ja estava em codigo Razor.

Correcao aplicada:

- Declaracao da variavel badge diretamente no bloco do foreach, sem abrir novo @{ }.

Resultado:

- Compilacao voltou a passar com sucesso.

---

## Estado final apos esta mudanca

- Cadastro de chamados funciona com INSERT real no banco.
- Listagem e detalhes usam a mesma fonte persistida.
- Dashboard mostra metricas reais e ultimos chamados.
- Build do projeto concluindo com sucesso (mantendo apenas warning de dependencia vulneravel).

---

## Beneficios tecnicos obtidos

- Eliminacao de divergencia entre dados da listagem e detalhes.
- Persistencia confiavel para fluxo basico de help desk.
- Base pronta para evoluir regras de ciclo de vida do chamado.
- Home com valor funcional para acompanhamento operacional.

---

## Riscos e pontos de atencao

- Processo da aplicacao pode ser encerrado e causar ERR_CONNECTION_REFUSED no navegador, mesmo com codigo correto, se o servidor nao estiver ativo durante o teste.
- Warning de HTTPS em ambiente local pode confundir diagnostico quando o perfil usado e somente HTTP.
- Falta implementar edit/delete/encerramento para completar CRUD.

---

## Proximos passos recomendados

1. Implementar Edit e Delete em ChamadosController com validacoes.
2. Criar acao de encerramento de chamado (setar Resolvido e DataFechamento).
3. Aplicar filtros reais por status e busca textual na listagem.
4. Tratar warning de dependencia e revisar estrategia de HTTPS local.
