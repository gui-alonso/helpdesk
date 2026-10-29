# Feature — Editar e Excluir chamados por perfil

Data: 29/10/2026

## 1. Objetivo

Adicionar as operações **Editar** e **Excluir** na coluna **Ações** da listagem de chamados (`/Chamados`),
mantendo o **Ver Detalhes** e respeitando as permissões de cada perfil, tanto na interface quanto no backend.

## 2. Permissões aplicadas

| Operação       | User                                       | Support          | Admin            |
|----------------|--------------------------------------------|------------------|------------------|
| Ver Detalhes   | Só os próprios chamados                    | Todos            | Todos            |
| Editar         | Só os próprios e **somente com status Aberto** | Todos (qualquer status) | Todos (qualquer status) |
| Excluir        | Não permitido                              | Sim, com confirmação | Sim, com confirmação |

Campos editáveis: **apenas Título e Descrição**. `Id`, `UsuarioId`, `Status`, `DataAbertura` e
`DataFechamento` nunca são alterados pelo formulário de edição. O status continua sendo alterado
somente pelas ações de atendimento já existentes (Iniciar, Resolver, Reabrir, Cancelar).

### Como cada regra é garantida no backend

| Regra | Onde |
|-------|------|
| Exigir autenticação | `[Authorize]` na classe `ChamadosController` (já existia) |
| User só edita o próprio chamado | `Editar` (GET e POST) busca o chamado com `.VisiveisPara(User)`; chamado de outra pessoa → **404** (não revela que existe) |
| User só edita chamado Aberto | Método privado `PodeEditar()`, verificado no GET **e de novo no POST** (no momento de salvar) |
| Só Support/Admin excluem | `[Authorize(Roles = Perfis.SupportOuAdmin)]` em `Excluir` → User recebe *Acesso negado* |
| Exclusão apenas via POST | `[HttpPost]` em `Excluir`; um GET em `/Chamados/Excluir/5` retorna 404 |
| Proteção CSRF | `[ValidateAntiForgeryToken]` em `Editar` (POST) e `Excluir` |
| Anti-overposting | `EditarChamadoViewModel` contém só `Titulo` e `Descricao`; o `Id` vem da rota e o chamado é recarregado do banco |
| Chamado inexistente | Editar → 404; Excluir → volta para a lista com mensagem de erro |

> Esconder botões na tela é só conforto visual. Quem garante a segurança é o controller:
> mesmo digitando a URL ou enviando a requisição manualmente, as regras acima continuam valendo.

## 3. Exclusão e integridade dos dados

Antes de implementar a exclusão definitiva, o modelo foi analisado:

- `Chamado` possui apenas uma chave estrangeira **para** `ApplicationUser` (`UsuarioId`).
- **Nenhuma** outra entidade referencia `Chamado` (não há comentários, anexos, histórico etc.).

Conclusão: remover um chamado não deixa registros órfãos, então foi usada **exclusão definitiva**
(`_context.Chamados.Remove(...)`), sem alterar o modelo nem criar tabelas. Mesmo assim, o
`SaveChangesAsync()` está protegido por `try/catch (DbUpdateException)` para exibir uma mensagem
de erro amigável caso, no futuro, surja alguma dependência.

> Se o projeto ganhar tabelas ligadas ao chamado (ex.: comentários), a sugestão é trocar a
> exclusão definitiva por *exclusão lógica* (um campo `Excluido` ou o status `Cancelado`, que já existe).

A confirmação é feita com `confirm()` no `onsubmit` do formulário, o mesmo padrão já usado no
botão **Cancelar** da tela de detalhes.

## 4. Arquivos alterados

| Arquivo | Alteração |
|---------|-----------|
| `ViewModels/EditarChamadoViewModel.cs` | **Novo.** ViewModel com `Titulo` e `Descricao` (mesmas validações do `CriarChamadoViewModel`). |
| `Controllers/ChamadosController.cs` | Novas actions `Editar` (GET), `Editar` (POST), `Excluir` (POST) e o método privado `PodeEditar()`. |
| `Views/Chamados/Editar.cshtml` | **Nova.** Formulário de edição no mesmo visual da tela de criação. |
| `Views/Chamados/Index.cshtml` | Botões Editar (`outline-warning`) e Excluir (`outline-danger`) na coluna Ações, agrupados com `d-flex gap-2`; exibição de mensagens de sucesso/erro (`TempData`). |

Nada foi alterado em Identity, `Program.cs`, migrations ou banco de dados.

## 5. Fluxo das mensagens

- Editar com sucesso → redireciona para **Detalhes** com "Chamado atualizado com sucesso."
- User tentando editar chamado que não está Aberto → **Detalhes** com mensagem de erro.
- Excluir com sucesso → **Lista** com "Chamado #N excluído com sucesso."
- Excluir chamado inexistente ou com falha → **Lista** com mensagem de erro.

## 6. Testes realizados

- `dotnet build`: **compilou com 0 avisos e 0 erros.**
- **Testes por HTTP (script com `curl`): 27 de 27 aprovados.** Executados com a aplicação rodando
  sobre uma **cópia** do `helpdesk.db` (o banco real não foi tocado), com usuários User A, User B,
  Support e Admin criados para o teste.

| Perfil | Cenário | Resultado esperado | Obtido |
|--------|---------|--------------------|--------|
| User | GET Editar próprio chamado Aberto | 200 | ✅ |
| User | POST Editar próprio enviando também `Status=Resolvido` e `UsuarioId` de outro | Salva só título/descrição; status continua Aberto | ✅ |
| User | GET / POST Editar chamado de outro usuário (URL manipulada) | 404 | ✅ |
| User | GET Editar chamado inexistente | 404 | ✅ |
| User | POST Excluir (requisição manual) | Acesso negado | ✅ |
| User | GET `/Chamados/Excluir/{id}` | 404 (só POST) | ✅ |
| User | Botão Excluir na listagem | Oculto | ✅ |
| User | GET Editar chamado próprio Em Andamento | Volta para Detalhes com erro | ✅ |
| User | POST Editar chamado próprio Em Andamento (token válido) | Recusado; chamado não alterado | ✅ |
| User | POST Editar com token antifalsificação inválido | 400 | ✅ |
| Support | GET Editar chamado de outro solicitante / Em Andamento | 200 | ✅ |
| Support | POST Editar chamado de outro solicitante | Salvo, redireciona para Detalhes | ✅ |
| Support | POST Editar com título vazio | Formulário volta com validação | ✅ |
| Support | POST Excluir sem token | 400 | ✅ |
| Support | POST Excluir | Excluído, mensagem de sucesso na lista | ✅ |
| Support | Detalhes do chamado excluído | 404 | ✅ |
| Support | POST Excluir chamado inexistente | Volta para a lista com mensagem de erro | ✅ |
| Admin | GET Editar chamado de outro usuário | 200 | ✅ |
| Admin | Botão Excluir na listagem | Visível | ✅ |
| Admin | POST Excluir | Excluído | ✅ |

> Observação: na primeira execução, a checagem da mensagem de sucesso falhou por um erro do
> próprio script. O Razor codifica acentos em HTML (`excluído` → `exclu&#xED;do`) e o script
> procurava o texto com acento. A checagem foi corrigida e o cenário passou.

A confirmação do `confirm()` no navegador (cenário 9 abaixo) depende de JavaScript e foi
verificada apenas por revisão do código; vale conferir visualmente.

### Roteiro de testes manuais

Prepare: dois usuários comuns (**A** e **B**), um **Support** e o **Admin**. A abre os chamados A1 e A2; B abre o B1.

**User (A)**
1. Em `/Chamados`, A1 mostra **Ver Detalhes** e **Editar**; não aparece **Excluir**.
2. Editar A1, alterar título/descrição → mensagem de sucesso; status continua **Aberto**.
3. Acessar `/Chamados/Editar/{id de B1}` pela URL → **404**.
4. Support inicia o atendimento de A2 → para A, o botão Editar some; `/Chamados/Editar/{A2}` volta para Detalhes com erro.
5. Abrir a edição de A1, o Support inicia o atendimento nesse meio-tempo e A salva → alteração recusada (verificação no POST).
6. Enviar um POST para `/Chamados/Excluir/{A1}` (ex.: DevTools) → **Acesso negado**.

**Support**
7. Editar B1 (de outro solicitante) e A2 (Em andamento) → permitido.
8. Salvar com título vazio → mensagem de validação, nada é gravado.
9. Excluir B1 → aparece a confirmação; cancelando, nada acontece; confirmando → "excluído com sucesso".
10. Acessar `/Chamados/Detalhes/{B1}` depois → **404**.

**Admin**
11. Mesmo comportamento do Support: Editar e Excluir disponíveis em todos os chamados.

**Overposting**
12. No DevTools, adicionar ao formulário de edição campos `Status=Resolvido` e `UsuarioId=...` e salvar →
    apenas título e descrição mudam.
