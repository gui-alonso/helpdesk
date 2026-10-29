# SPEC — Gestão de status e ciclo de atendimento

## 1. Nome da funcionalidade

**Gestão do ciclo de atendimento do chamado — Perfil Suporte**

---

## 2. Contexto

O HelpDeskMVC já permite criar, persistir, listar e consultar chamados utilizando ASP.NET Core MVC, Entity Framework Core e SQLite.

Todo chamado é criado com status inicial `Aberto`, porém ainda não existe uma operação para representar o trabalho realizado pela equipe de suporte.

Esta especificação define o comportamento necessário para permitir que chamados evoluam durante o atendimento sem permitir exclusão física e sem transferir ao Solicitante o controle operacional do status.

---

## 3. Objetivo funcional

Disponibilizar ações controladas para que o fluxo de um chamado possa ser representado no sistema:

```text
Aberto -> EmAndamento -> Resolvido
```

Também deverão ser previstos os estados:

```text
Cancelado
```

e, se implementado:

```text
Resolvido -> EmAndamento
```

para reabertura.

---

## 4. Atores

### Solicitante

Responsável por abrir e acompanhar chamados.

Nesta funcionalidade, não deverá:

- alterar o status operacional;
- excluir definitivamente um chamado;
- marcar o chamado como resolvido.

A autorização real por perfil será implementada posteriormente.

### Suporte

Responsável pelo ciclo operacional do chamado.

Deverá futuramente possuir permissão para:

- iniciar atendimento;
- resolver;
- reabrir;
- cancelar conforme regra do sistema.

### Administrador

Não faz parte do escopo de implementação de autorização desta funcionalidade.

Em uma fase posterior poderá possuir acesso administrativo aos chamados.

---

## 5. Status suportados

Criar um enum denominado, preferencialmente:

`ChamadoStatus`

Valores:

```csharp
public enum ChamadoStatus
{
    Aberto,
    EmAndamento,
    Resolvido,
    Cancelado
}
```

O campo `Status` da entidade `Chamado` não deverá permanecer como texto livre.

---

## 6. Entidade Chamado

A entidade deverá continuar contendo, no mínimo, os campos já existentes no projeto:

- identificador;
- título;
- descrição;
- status;
- data de abertura;
- data de fechamento.

Requisitos específicos:

```csharp
public ChamadoStatus Status { get; set; }

public DateTime DataAbertura { get; set; }

public DateTime? DataFechamento { get; set; }
```

A forma exata do identificador deve seguir o modelo já existente no projeto.

---

## 7. Regras de transição

### 7.1 Criação

Ao criar:

```text
Status = Aberto
DataAbertura = data/hora atual
DataFechamento = null
```

### 7.2 Aberto -> EmAndamento

Operação:

`IniciarAtendimento`

Pré-condições:

- chamado existe;
- status atual é `Aberto`.

Resultado:

```text
Status = EmAndamento
DataFechamento permanece null
```

### 7.3 EmAndamento -> Resolvido

Operação:

`Resolver`

Pré-condições:

- chamado existe;
- status atual é `EmAndamento`.

Resultado:

```text
Status = Resolvido
DataFechamento = data/hora atual
```

### 7.4 Resolvido -> EmAndamento

Operação:

`Reabrir`

Pré-condições:

- chamado existe;
- status atual é `Resolvido`.

Resultado:

```text
Status = EmAndamento
DataFechamento = null
```

### 7.5 Aberto ou EmAndamento -> Cancelado

Operação:

`Cancelar`

Pré-condições:

- chamado existe;
- status está em um estado que aceite cancelamento.

Resultado:

```text
Status = Cancelado
```

O registro não deverá ser apagado do banco.

---

## 8. Matriz de transição

| Status atual | Ação | Próximo status |
|---|---|---|
| Aberto | Iniciar atendimento | EmAndamento |
| Aberto | Cancelar | Cancelado |
| EmAndamento | Resolver | Resolvido |
| EmAndamento | Cancelar | Cancelado |
| Resolvido | Reabrir | EmAndamento |
| Cancelado | — | — |

Outras transições devem ser consideradas inválidas nesta especificação.

---

## 9. Controller

Utilizar o controller já responsável pelos chamados.

As operações de alteração de estado deverão ser implementadas em actions separadas.

### Exemplo estrutural

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
public async Task<IActionResult> IniciarAtendimento(Guid id)
{
    // localizar chamado
    // validar status
    // atualizar
    // salvar
    // redirecionar
}
```

A assinatura deve respeitar o tipo real do identificador já utilizado no projeto.

### Actions previstas

```text
POST /Chamados/IniciarAtendimento/{id}
POST /Chamados/Resolver/{id}
POST /Chamados/Reabrir/{id}
POST /Chamados/Cancelar/{id}
```

Os nomes finais das rotas podem seguir o padrão já adotado pelo projeto.

---

## 10. Comportamento das actions

Toda action deverá:

1. receber o identificador;
2. consultar o chamado no banco;
3. retornar `NotFound` se não existir;
4. validar se a transição solicitada é permitida;
5. alterar somente os campos necessários;
6. salvar com `SaveChangesAsync`;
7. gerar feedback para o usuário;
8. redirecionar para a tela apropriada.

Não aceitar um novo status arbitrário enviado pelo cliente.

### Evitar

```csharp
chamado.Status = statusRecebidoDoFormulario;
```

### Preferir

```csharp
chamado.Status = ChamadoStatus.EmAndamento;
```

dentro de uma operação específica e validada.

---

## 11. View de detalhes

A tela de detalhes será o principal ponto de interação nesta fase.

Deverá apresentar:

- número/ID do chamado;
- título;
- descrição;
- status;
- data de abertura;
- data de fechamento, se existente;
- ações disponíveis.

### Status Aberto

Exibir:

- `Iniciar atendimento`
- `Cancelar`

### Status EmAndamento

Exibir:

- `Resolver chamado`
- `Cancelar`

### Status Resolvido

Exibir:

- `Reabrir`

### Status Cancelado

Não exibir ação operacional nesta etapa.

---

## 12. Segurança das operações

Mesmo sem autenticação implementada, aplicar:

```csharp
[HttpPost]
[ValidateAntiForgeryToken]
```

em todas as operações que alteram estado.

Não utilizar links GET para ações de alteração.

Exemplo que deve ser evitado:

```text
GET /Chamados/Resolver/10
```

A resolução deverá ocorrer por POST.

---

## 13. Feedback para o usuário

Após uma operação válida, exibir mensagem de confirmação.

Exemplos:

```text
Atendimento iniciado com sucesso.
```

```text
Chamado resolvido com sucesso.
```

```text
Chamado reaberto com sucesso.
```

```text
Chamado cancelado.
```

Pode ser utilizado `TempData` caso já esteja alinhado ao padrão do projeto.

Para transições inválidas, não alterar o chamado e apresentar mensagem adequada ou retornar uma resposta coerente com a arquitetura atual.

---

## 14. Banco de dados e migration

Como o projeto já utiliza migrations, qualquer alteração no tipo ou mapeamento do campo `Status` deverá ser versionada.

Fluxo esperado:

```bash
dotnet ef migrations add AddChamadoStatusFlow
dotnet ef database update
```

O nome da migration pode ser adaptado.

Antes de atualizar o banco, verificar como o EF Core irá converter os valores existentes do status atual.

Os chamados já cadastrados como `Aberto` devem permanecer semanticamente equivalentes após a alteração.

---

## 15. Dashboard

O dashboard atualmente contabiliza:

- total;
- abertos;
- em andamento;
- resolvidos;
- chamados recentes.

As consultas devem ser atualizadas para utilizar `ChamadoStatus`.

Exemplo conceitual:

```csharp
await _context.Chamados
    .CountAsync(c => c.Status == ChamadoStatus.Aberto);
```

Após resolver um chamado, os indicadores devem refletir imediatamente a alteração quando o dashboard for carregado novamente.

---

## 16. Listagem

A tela `Index` deve continuar exibindo o status.

Nesta fase não é necessário implementar os filtros funcionais mencionados no planejamento geral do projeto.

Caso o enum seja exibido diretamente como:

```text
EmAndamento
```

pode ser criada posteriormente uma apresentação amigável:

```text
Em andamento
```

sem alterar o valor interno.

---

## 17. Cenários de teste manual

### CT01 — Criar chamado

**Dado** um novo chamado válido  
**Quando** for cadastrado  
**Então** deve possuir status `Aberto`  
**E** `DataFechamento` deve ser nula.

### CT02 — Iniciar atendimento

**Dado** um chamado `Aberto`  
**Quando** o suporte iniciar o atendimento  
**Então** o status deve ser `EmAndamento`.

### CT03 — Resolver

**Dado** um chamado `EmAndamento`  
**Quando** for resolvido  
**Então** o status deve ser `Resolvido`  
**E** `DataFechamento` deve ser preenchida.

### CT04 — Reabrir

**Dado** um chamado `Resolvido`  
**Quando** for reaberto  
**Então** o status deve ser `EmAndamento`  
**E** `DataFechamento` deve voltar a ser nula.

### CT05 — Cancelar

**Dado** um chamado cancelável  
**Quando** for cancelado  
**Então** o status deve ser `Cancelado`  
**E** o registro deve continuar no banco.

### CT06 — Transição inválida

**Dado** um chamado `Resolvido`  
**Quando** for enviada uma ação incompatível  
**Então** o sistema não deve alterar indevidamente o estado.

### CT07 — Chamado inexistente

**Dado** um ID que não existe  
**Quando** uma action operacional for executada  
**Então** o controller deve retornar `NotFound`.

---

## 18. Critérios de aceite

A funcionalidade será considerada concluída quando:

- [ ] houver enum para os status;
- [ ] não houver mais dependência de texto livre para definir estados;
- [ ] novos chamados iniciarem como `Aberto`;
- [ ] for possível iniciar atendimento;
- [ ] for possível resolver;
- [ ] resolução preencher `DataFechamento`;
- [ ] reabertura limpar `DataFechamento`;
- [ ] cancelamento não apagar o chamado;
- [ ] transições inválidas forem bloqueadas pelo backend;
- [ ] detalhes mostrarem somente ações compatíveis com o estado;
- [ ] as alterações persistirem no SQLite;
- [ ] o dashboard continuar funcionando;
- [ ] `dotnet build` finalizar sem erros.

---

## 19. Não objetivos desta SPEC

Esta implementação não deverá introduzir antecipadamente:

- autenticação;
- autorização por roles;
- ASP.NET Core Identity;
- usuário responsável pelo chamado;
- histórico;
- comentários;
- anexos;
- exclusão física;
- busca;
- filtros;
- paginação;
- notificações.

Esses itens deverão ser tratados em funcionalidades posteriores.

---

## 20. Evolução futura

Após esta funcionalidade, a próxima etapa recomendada é implementar autenticação e autorização.

Nesse momento:

- o `Solicitante` cria e acompanha seus chamados;
- o `Suporte` controla o fluxo operacional;
- o `Administrador` gerencia recursos e permissões do sistema.

As actions definidas nesta SPEC poderão então receber regras de autorização específicas sem necessidade de redesenhar o fluxo de status.
