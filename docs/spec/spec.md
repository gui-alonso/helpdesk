# Especificação técnica — HelpDeskMVC / Fase 1

**Funcionalidade:** Busca e filtros da listagem + preparação dos modelos de entrada  
**Aplicação:** ASP.NET Core MVC (.NET 10), Razor Views, EF Core 10, SQLite  
**Referência:** diagnóstico geral de 08/10/2026

## 1. Comportamento esperado

### RF-01 — Listagem sem filtros
Ao acessar a listagem sem parâmetros, o sistema deve exibir todos os chamados, **ordenados por `DataAbertura` decrescente**, conforme o comportamento descrito no diagnóstico.

### RF-02 — Pesquisa textual
O campo **Buscar** deve filtrar chamados quando o texto informado estiver presente no **título OU na descrição**. Espaços extras no início e fim da entrada devem ser desconsiderados. Busca vazia deve equivaler à ausência de filtro textual.

### RF-03 — Filtro de status
O campo **Status** deve permitir **Todos** (sem filtro) e os valores existentes do enum: `Aberto`, `EmAndamento`, `Resolvido`, `Cancelado`. Os nomes visuais podem ser amigáveis (por exemplo, "Em andamento"), sem alterar o valor armazenado.

### RF-04 — Composição dos filtros
Quando `busca` e `status` estiverem preenchidos, a pesquisa deve aplicar ambos (`AND`). Dentro da pesquisa textual, a correspondência deve ser por título **ou** descrição (`OR`).

### RF-05 — Permanência e limpeza
A interface deve manter os valores da busca e do status no retorno da página e oferecer um link/botão **Limpar filtros**, voltando para a listagem sem query string.

### RF-06 — Resultado vazio
Quando não houver chamados correspondentes, mostrar mensagem explícita (por exemplo, "Nenhum chamado encontrado para os filtros informados") sem tratar isso como erro HTTP.

### RF-07 — Cadastro com ViewModel
O POST de criação deve receber apenas os campos que o solicitante pode informar atualmente: `Titulo` e `Descricao`. O servidor continua responsável por status, data de abertura, data de fechamento e outros campos internos que existam na entidade.

### RF-08 — Preservação das transições de status
Nenhuma alteração funcional nas transições existentes nesta fase:

| Estado atual | Ação | Próximo estado |
|---|---|---|
| `Aberto` | Iniciar atendimento | `EmAndamento` |
| `EmAndamento` | Resolver | `Resolvido` |
| `Resolvido` | Reabrir | `EmAndamento` |
| `Aberto` ou `EmAndamento` | Cancelar | `Cancelado` |

O diagnóstico registra `DataFechamento` no momento da resolução e ausência de preenchimento ao cancelar. Manter a implementação atual e registrar essa decisão pendente para uma fase posterior.

## 2. Contratos propostos

### 2.1. GET da listagem

**Rota:** preservar a rota já existente; exemplos abaixo assumem `/Chamados`.

```http
GET /Chamados
GET /Chamados?busca=impressora
GET /Chamados?status=Aberto
GET /Chamados?busca=rede&status=EmAndamento
```

**Parâmetros:**

| Nome | Tipo | Obrigatório | Regra |
|---|---|---|---|
| `busca` | string | Não | `Trim()`; branco equivale a nenhum filtro |
| `status` | enum opcional ou string validada | Não | Aplicar apenas quando for valor definido do enum |

**Status inválido:** não lançar exceção nem retornar erro 500. Comportamento escolhido para esta fase: ignorar o filtro inválido e mostrar a listagem aplicando apenas os demais filtros válidos. A interface deverá voltar a **Todos** para o seletor de status. Um comportamento alternativo (erro de validação) exigiria decisão explícita antes de codificar.

### 2.2. ViewModel de listagem

Nome sugerido: `ListarChamadosViewModel` em `ViewModels/`.

```csharp
public class ListarChamadosViewModel
{
    public string? Busca { get; set; }
    public StatusChamado? Status { get; set; }
    public List<Chamado> Chamados { get; set; } = new();
}
```

> Exemplo conceitual: ajustar namespace, nome exato do enum `StatusChamado` e tipo de coleção à implementação real. O diagnóstico confirma um enum de status, mas não seu identificador C#.

Neste momento, os itens podem continuar utilizando `Chamado` como dados de leitura. Não é obrigatório criar ViewModel por linha da tabela.

### 2.3. ViewModel de criação

Nome sugerido: `CriarChamadoViewModel`, a **reutilizar caso já exista**. Verificar se a solução o mantém em `Models/` ou `ViewModels/` antes de mover/criar.

```csharp
using System.ComponentModel.DataAnnotations;

public class CriarChamadoViewModel
{
    [Required(ErrorMessage = "Informe um título.")]
    [StringLength(150, ErrorMessage = "O título deve ter no máximo 150 caracteres.")]
    public string Titulo { get; set; } = string.Empty;

    [Required(ErrorMessage = "Informe uma descrição.")]
    [StringLength(2000, ErrorMessage = "A descrição deve ter no máximo 2000 caracteres.")]
    public string Descricao { get; set; } = string.Empty;
}
```

Os limites exemplificados (150/2000) já foram utilizados no histórico de desenvolvimento; **conferir se correspondem à versão atual do projeto** e manter as regras efetivamente presentes, caso sejam diferentes.

## 3. Esboço de implementação no controller

O código a seguir é **referência de lógica**, não um patch pronto; conferir classes, propriedades e assinaturas do projeto.

```csharp
public async Task<IActionResult> Index(string? busca, string? status)
{
    var texto = busca?.Trim();
    StatusChamado? statusSelecionado = null;

    if (!string.IsNullOrWhiteSpace(status)
        && Enum.TryParse<StatusChamado>(status, true, out var parsed)
        && Enum.IsDefined(parsed))
    {
        statusSelecionado = parsed;
    }

    IQueryable<Chamado> query = _context.Chamados.AsNoTracking();

    if (!string.IsNullOrWhiteSpace(texto))
    {
        query = query.Where(c => c.Titulo.Contains(texto)
                              || c.Descricao.Contains(texto));
    }

    if (statusSelecionado.HasValue)
    {
        query = query.Where(c => c.Status == statusSelecionado.Value);
    }

    var vm = new ListarChamadosViewModel
    {
        Busca = texto,
        Status = statusSelecionado,
        Chamados = await query
            .OrderByDescending(c => c.DataAbertura)
            .ToListAsync()
    };

    return View(vm);
}
```

**Observações técnicas:**
- O enum é persistido como texto segundo o diagnóstico; verificar a tradução do filtro pelo provider SQLite.
- `Contains` é intencionalmente simples e deve ser **testado no SQLite real** para conferir tratamento de caixa e acentuação. Caso haja requisito específico de busca case-insensitive, definir a estratégia e testá-la; não assumir comportamento idêntico em todos os bancos.
- A consulta deve ser construída antes de chamar `ToListAsync()`; não carregar a tabela inteira para filtrar em memória.
- No POST de criação, após `ModelState.IsValid`, mapear somente campos permitidos para `new Chamado { ... }` e manter as regras de servidor atuais. Caso o formulário seja inválido, retornar a mesma view com os erros.

## 4. Interface Razor

### 4.1. Formulário GET

No topo da view de listagem, usar os campos já existentes com nomes que correspondam à action:

```cshtml
@model ListarChamadosViewModel

<form asp-controller="Chamados" asp-action="Index" method="get">
    <input asp-for="Busca" name="busca" placeholder="Buscar por título ou descrição" />
    <select asp-for="Status" name="status">
        <option value="">Todos os status</option>
        <option value="Aberto">Aberto</option>
        <option value="EmAndamento">Em andamento</option>
        <option value="Resolvido">Resolvido</option>
        <option value="Cancelado">Cancelado</option>
    </select>
    <button type="submit">Filtrar</button>
    <a asp-controller="Chamados" asp-action="Index">Limpar filtros</a>
</form>
```

**Nota:** snippet ilustrativo; aplicar as classes Bootstrap e a estrutura de layout já utilizadas na tela. Confirmar route/action reais. Preferir não duplicar atributos `name` quando o Tag Helper já emitir o nome desejado; se necessário, alinhar propriedade e parâmetro ou usar um `<input name="busca">` simples.

### 4.2. Tabela e estado vazio

- Iterar sobre `Model.Chamados`, não mais sobre um `IEnumerable<Chamado>` como model raiz (se essa era a tipagem anterior).
- Preservar colunas, formatação de datas/status, botões e links atuais.
- Mostrar mensagem amigável quando `Model.Chamados.Count == 0`.
- Não inserir recursos exclusivos de usuários/roles nesta fase.

### 4.3. Formulário de criação

- Alterar `@model` para `CriarChamadoViewModel` caso ainda aponte para `Chamado`.
- Manter `asp-for="Titulo"` e `asp-for="Descricao"` e validações visuais existentes.
- Não disponibilizar campos de status, datas e outros dados internos no formulário.

## 5. Testes e cenários de aceite

Sugestão: usar **xUnit** em um projeto de testes separado, caso não exista infraestrutura de testes. Preferir SQLite em memória em testes de consulta com EF Core, para reproduzir o provider usado pelo projeto; evitar testes que dependam do arquivo `helpdesk.db` da turma.

| ID | Cenário | Resultado esperado |
|---|---|---|
| CT-01 | GET sem parâmetros | Todos os chamados, ordenados por abertura decrescente |
| CT-02 | Busca por palavra no título | Retorna os chamados correspondentes |
| CT-03 | Busca por palavra na descrição | Retorna os chamados correspondentes |
| CT-04 | Busca sem correspondência | Lista vazia e mensagem amigável |
| CT-05 | Filtro `Aberto` | Apenas chamados abertos |
| CT-06 | Filtro `EmAndamento` | Apenas chamados em andamento |
| CT-07 | Filtro `Resolvido` | Apenas chamados resolvidos |
| CT-08 | Filtro `Cancelado` | Apenas chamados cancelados |
| CT-09 | Busca + status | Interseção das duas condições |
| CT-10 | Busca com espaços extras | Equivale ao termo sem espaços laterais |
| CT-11 | Status inválido | Não ocorre erro 500; filtro inválido ignorado |
| CT-12 | Formulário de criação sem título | Validação impede persistência |
| CT-13 | Formulário de criação sem descrição | Validação impede persistência |
| CT-14 | Título/descrição acima do limite | Validação impede persistência |
| CT-15 | Criação válida | Salva novo chamado com campos internos definidos pelo servidor |
| CT-16 | Limpar filtros | Retorna à lista sem critérios selecionados |
| CT-17 | Fluxo de status pré-existente | Iniciar, resolver, reabrir e cancelar continuam respeitando regras atuais |

**Teste exploratório adicional:** pesquisar com diferenças de maiúsculas/minúsculas e acentuação; registrar o comportamento real antes de prometer uniformidade.

## 6. Condições de segurança e regressão

- Preservar validação antifalsificação nos formulários POST existentes.
- Não usar a entidade persistida inteira como contrato de entrada para criação.
- Não transformar filtros GET em comandos que modificam dados.
- Não alterar permissões ou visibilidade de chamados: não há autenticação implementada ainda.
- Não modificar `Migrations/` sem alteração real no esquema.
- Não alterar a lógica de dashboard nem ações de transição nesta fase, salvo correção essencial documentada separadamente.
- Não remover dados reais para criar cenários de teste.

## 7. Definição de pronto (DoD)

- [ ] Implementação conferida contra arquivos reais do repositório.
- [ ] Busca textual funciona em título/descrição.
- [ ] Filtro de status funciona nos quatro estados.
- [ ] Filtros simultâneos, permanência, limpeza e estado vazio funcionam.
- [ ] ViewModels corretamente integrados no GET e no POST.
- [ ] Campos internos da entidade continuam controlados pelo servidor.
- [ ] `dotnet build` executa sem erros.
- [ ] Testes automatizados adicionados executam sem falhas e cenários manuais são verificados.
- [ ] Fluxos existentes de chamados não apresentam regressão.
- [ ] Documentação atualizada com decisões e limitações conhecidas.

## 8. Notas para a próxima fase

Depois desta entrega, será possível introduzir autenticação com Identity e relacionar cada chamado a um solicitante, sem misturar essa mudança com a implementação de busca e filtros.
