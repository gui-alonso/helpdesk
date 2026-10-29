# Diagnóstico do HelpDeskMVC — 28/10/2026

## Overview e resumo

O HelpDeskMVC é uma aplicação web educacional para registrar e acompanhar chamados de suporte. O fluxo principal já permite abrir um chamado, consultar a listagem e os detalhes, acompanhar indicadores no dashboard e avançar o atendimento entre estados definidos. Os dados são persistidos em um arquivo SQLite local.

A base é pequena e segue o padrão ASP.NET Core MVC. Os controllers fazem as consultas, aplicam as regras de transição e entregam dados às Razor Views. O Entity Framework Core cuida da persistência. A estrutura favorece o entendimento do fluxo completo, embora as regras de atendimento estejam concentradas no controller de chamados.

### Tecnologias e configuração

| Área | Implementação atual |
|---|---|
| Aplicação | ASP.NET Core MVC em .NET 10 (`net10.0`) |
| Persistência | Entity Framework Core 10 com provedor SQLite |
| Banco | `helpdesk.db`, configurado pela connection string `DefaultConnection` em `appsettings.json` |
| Interface | Razor Views, Bootstrap, jQuery, CSS e JavaScript em `wwwroot/` |
| Esquema | Migrations do EF Core em `Migrations/` |

## Arquitetura atual

O `Program.cs` registra MVC e `AppDbContext` na injeção de dependência, configura o provedor SQLite e define a rota convencional `{controller=Home}/{action=Index}/{id?}`. A aplicação usa redirecionamento para HTTPS, tratamento de exceção e HSTS fora do ambiente de desenvolvimento, além de servir arquivos estáticos.

```text
Navegador
   ↓ requisição HTTP
Roteamento MVC (Program.cs)
   ↓
HomeController / ChamadosController
   ↓ consultas e alterações assíncronas
AppDbContext → Entity Framework Core → SQLite (helpdesk.db)
   ↓ dados para apresentação
Razor Views (Views/) + Bootstrap/CSS/JS (wwwroot/)
```

| Camada | Responsabilidade observada |
|---|---|
| `Controllers/` | `HomeController` monta o dashboard; `ChamadosController` lista, cria, consulta detalhes e executa transições de status. |
| `Models/` | `Chamado` representa o registro; `ChamadoStatus` define os estados possíveis. |
| `Data/` | `AppDbContext` expõe `DbSet<Chamado>` e converte o enum de status para texto no banco. |
| `ViewModels/` | `DashboardViewModel` reúne contadores e os três chamados mais recentes. |
| `Views/` | Telas Razor do dashboard, cadastro, listagem e detalhes, além do layout compartilhado. |
| `Migrations/` | Histórico versionado da criação e evolução da tabela `Chamados`. |
| `docs/` | Especificações, planos, manuais, regras e diagnósticos anteriores. |

### Modelo e fluxo de dados

`Chamado` tem identificador inteiro, título, descrição, status, data de abertura e data de fechamento opcional. Título e descrição são obrigatórios por Data Annotations. O status é um enum com os valores `Aberto`, `EmAndamento`, `Resolvido` e `Cancelado`, armazenado como texto no SQLite.

No cadastro, o POST valida o modelo e redefine no servidor o identificador, o status inicial (`Aberto`), a data de abertura e a data de fechamento antes de salvar. A listagem consulta todos os chamados por data de abertura decrescente. Os detalhes consultam pelo identificador e retornam `NotFound` quando ele não existe. O dashboard calcula total, abertos, em andamento e resolvidos, além de buscar os três registros mais recentes; cancelados entram no total, mas não têm contador próprio.

As transições implementadas são:

| Estado atual | Ação | Novo estado | Data de fechamento |
|---|---|---|---|
| Aberto | Iniciar atendimento | EmAndamento | Permanece vazia |
| EmAndamento | Resolver | Resolvido | Recebe a data e hora atuais |
| Resolvido | Reabrir | EmAndamento | Volta a ficar vazia |
| Aberto ou EmAndamento | Cancelar | Cancelado | Permanece como estava, normalmente vazia |

Cada transição é um POST com validação antifalsificação. O controller consulta o registro, verifica o estado permitido, salva a alteração e usa `TempData` para mostrar o resultado na tela de detalhes. Os botões exibidos nessa tela acompanham o estado atual.

## Estado geral e pontos de atenção

- O fluxo essencial de chamados está implementado e persistido; há separação clara entre telas, modelos e acesso a dados.
- A busca e o filtro por status exibidos na listagem não enviam parâmetros nem filtram a consulta. A listagem também não tem paginação.
- As regras de transição e o acesso ao banco ficam diretamente em `ChamadosController`; não há camada de serviço para esse fluxo.
- O POST de criação recebe a entidade `Chamado` diretamente. Os campos internos são redefinidos no servidor, mas um modelo de entrada específico deixaria explícito quais dados o formulário aceita.
- Não há configuração de autenticação nem usuários ou perfis no código atual. As ações de atendimento estão disponíveis sem distinção entre solicitante e suporte.
- Não foi encontrado projeto de testes automatizados na estrutura analisada. Este diagnóstico foi feito por leitura do repositório; não inclui execução da aplicação nem verificação do conteúdo do banco.
- A documentação existente inclui planos e especificações de evolução. As funcionalidades descritas nesses arquivos foram consideradas implementadas apenas quando confirmadas no código atual.

## Síntese

O projeto está em um estágio funcional de aplicação MVC didática: o ciclo básico do chamado, o dashboard e a persistência SQLite estão presentes. Para evoluir a experiência e a manutenção, os pontos mais imediatos são tornar a busca e o filtro funcionais, cobrir o fluxo com testes e separar gradualmente as regras de atendimento e os dados de entrada das actions MVC. Autenticação e autorização passam a ser necessárias quando houver papéis distintos ou uso por múltiplos usuários.
