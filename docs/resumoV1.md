# HelpDeskMvc — Resumo V1

## O que é o projeto

Sistema web de **help desk** (chamados/tickets) em **ASP.NET Core MVC** (.NET 10), voltado ao aprendizado de estrutura MVC: Models, Controllers e Views.

## Progresso atual

| Área | Situação |
|------|----------|
| Estrutura MVC | Configurada (Home + Chamados) |
| Modelo `Chamado` | Pronto (título, descrição, status, datas) |
| Listagem | Tela `/Chamados` com tabela Bootstrap |
| Detalhes | Tela `/Chamados/Detalhes/{id}` com card de informações |
| Navegação | Menu com Home e Chamados |
| Persistência | **Não há** — dados só em memória no controller |
| Cadastro / edição / exclusão | **Não implementados** |
| Banco de dados (EF Core) | **Não configurado** |
| Autenticação | **Não há** |

### O que já funciona

- Abrir a aplicação e navegar até a lista de chamados.
- Ver um chamado em detalhe pelo botão **Ver Detalhes** (quando o ID existe na lista usada na action `Detalhes`).

### Pontos de atenção (estado atual)

- A **lista da Index** e a **lista da Detalhes** são **diferentes** no código: a listagem mostra um chamado; os detalhes usam outra lista fixa com três itens. Por isso, clicar em “Ver Detalhes” na Index pode não bater com o que aparece na tela de detalhes.
- O contador automático de ID está comentado — ainda não há fluxo para **criar** novos chamados.

## Conclusão

O projeto está na **primeira versão funcional de leitura**: modelo definido, listagem e visualização de detalhes com interface Bootstrap. Ainda é um protótipo didático, sem persistência nem operações de escrita (criar, editar, fechar chamado).

---

## Próximo passo recomendado

**Feature sugerida: Abrir novo chamado (Create)**

Implementar as actions `Create` (GET — formulário) e `Create` (POST — salvar), reutilizando a **mesma lista em memória** da Index, com validação básica e redirecionamento para a listagem após salvar. Antes ou junto disso, **unificar a fonte de dados** entre `Index` e `Detalhes` para que listagem e detalhes mostrem os mesmos registros.

Isso completa o fluxo mínimo de um help desk (ver lista → abrir ticket → ver detalhe) e prepara o terreno para, em seguida, adicionar **Entity Framework** e banco de dados.
