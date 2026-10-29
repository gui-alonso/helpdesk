# PLAN — Gestão do ciclo de atendimento pelo Suporte

## 1. Objetivo

Implementar no projeto **HelpDeskMVC** a primeira evolução do ciclo de vida dos chamados, permitindo que o perfil de **Suporte** controle o andamento do atendimento.

Nesta etapa, o foco será preparar o sistema para que um chamado possa evoluir entre os estados:

- `Aberto`
- `EmAndamento`
- `Resolvido`
- `Cancelado`

O Solicitante não deverá alterar diretamente o status operacional do chamado e não deverá excluir chamados definitivamente.

> Observação: o projeto ainda não possui autenticação e perfis implementados. Portanto, nesta fase, as actions e telas serão construídas com a responsabilidade funcional de "Suporte", deixando a autorização por role para uma etapa posterior.

---

## 2. Estado atual relacionado à funcionalidade

O projeto já possui:

- ASP.NET Core MVC;
- Entity Framework Core;
- SQLite;
- entidade `Chamado`;
- campo de status;
- `DataAbertura`;
- `DataFechamento`;
- cadastro de chamados;
- listagem;
- detalhes;
- dashboard com indicadores;
- persistência no banco;
- migrations.

Atualmente, um chamado é criado com status inicial `Aberto`, mas ainda não existe um fluxo para evolução do atendimento.

---

## 3. Escopo desta implementação

### Incluído

- Padronizar os possíveis status do chamado.
- Permitir visualizar um chamado e executar ações de atendimento.
- Permitir iniciar o atendimento de um chamado aberto.
- Permitir marcar um chamado como resolvido.
- Preencher `DataFechamento` automaticamente ao resolver.
- Permitir reabrir um chamado resolvido, caso a regra seja implementada nesta etapa.
- Limpar `DataFechamento` ao reabrir.
- Permitir cancelamento controlado.
- Atualizar os indicadores do dashboard conforme o novo status.
- Exibir mensagens de sucesso ou erro após as operações.
- Persistir todas as alterações pelo Entity Framework Core.

### Fora do escopo

- Login.
- ASP.NET Core Identity.
- Roles reais de `Solicitante`, `Suporte` e `Administrador`.
- Atribuição de chamado a um atendente específico.
- Comentários.
- Histórico de alterações.
- Anexos.
- Notificações.
- Exclusão definitiva de chamados.
- SLA.
- Priorização.
- Busca, filtros e paginação.

---

## 4. Regras de negócio

### RN01 — Novo chamado

Todo chamado criado deverá iniciar com:

`Aberto`

A `DataAbertura` será preenchida no momento da criação.

A `DataFechamento` deverá permanecer nula.

### RN02 — Iniciar atendimento

Somente chamados com status `Aberto` poderão ser movidos para:

`EmAndamento`

### RN03 — Resolver chamado

Chamados em atendimento poderão ser movidos para:

`Resolvido`

Ao resolver:

- atualizar o status para `Resolvido`;
- preencher `DataFechamento` com a data/hora atual.

### RN04 — Reabrir chamado

Caso a reabertura seja incluída nesta etapa:

`Resolvido -> EmAndamento`

Ao reabrir:

- atualizar o status;
- definir `DataFechamento = null`.

### RN05 — Cancelamento

O cancelamento não deverá remover o registro do banco.

O status deverá passar para:

`Cancelado`

A exclusão física do chamado não faz parte deste fluxo.

### RN06 — Transições inválidas

O backend deverá impedir mudanças de status que não respeitem as regras previstas.

Exemplo:

`Aberto -> Resolvido`

não deverá ocorrer diretamente caso a regra adotada exija atendimento prévio.

---

## 5. Modelagem

### 5.1 Status

Recomenda-se substituir o armazenamento de status como texto livre por um `enum`.

Exemplo conceitual:

```csharp
public enum ChamadoStatus
{
    Aberto,
    EmAndamento,
    Resolvido,
    Cancelado
}
```

A entidade `Chamado` deverá utilizar esse tipo no campo `Status`.

### 5.2 Data de fechamento

`DataFechamento` deverá continuar opcional.

Exemplo:

```csharp
public DateTime? DataFechamento { get; set; }
```

---

## 6. Fluxo funcional

### Criar chamado

```text
Solicitante cria chamado
        |
        v
      Aberto
```

### Atendimento

```text
Aberto
   |
   | Iniciar atendimento
   v
EmAndamento
   |
   | Resolver
   v
Resolvido
```

### Reabertura

```text
Resolvido
   |
   | Reabrir
   v
EmAndamento
```

### Cancelamento

```text
Aberto ou EmAndamento
        |
        | Cancelar
        v
     Cancelado
```

---

## 7. Alterações previstas por camada

### Models

- Criar `ChamadoStatus`.
- Alterar o tipo da propriedade `Status`.
- Manter `DataFechamento` como nullable.

### Data / Migrations

- Criar nova migration após alteração do modelo.
- Atualizar o banco com `dotnet ef database update`.
- Validar compatibilidade com registros existentes.

### Controllers

Adicionar actions específicas para o fluxo de atendimento, evitando uma alteração genérica de status enviada livremente pelo formulário.

Sugestões:

- `IniciarAtendimento(Guid id)`
- `Resolver(Guid id)`
- `Reabrir(Guid id)`
- `Cancelar(Guid id)`

As operações que alteram estado devem preferencialmente utilizar `POST`.

### Views

Na tela de detalhes, exibir botões conforme o status atual.

Exemplo:

#### Aberto

- Iniciar atendimento
- Cancelar

#### Em andamento

- Resolver
- Cancelar

#### Resolvido

- Reabrir

#### Cancelado

- nenhuma ação operacional nesta etapa

### Dashboard

Confirmar que os indicadores utilizam os novos valores padronizados do status.

---

## 8. Ordem de implementação

### Etapa 1 — Padronização do status

- [ ] Criar enum `ChamadoStatus`.
- [ ] Atualizar `Chamado`.
- [ ] Ajustar criação do chamado para usar `ChamadoStatus.Aberto`.
- [ ] Ajustar consultas que atualmente comparam strings.

### Etapa 2 — Persistência

- [ ] Criar migration.
- [ ] Atualizar SQLite.
- [ ] Verificar dados existentes.
- [ ] Executar aplicação e conferir leitura dos chamados.

### Etapa 3 — Controller

- [ ] Criar action `IniciarAtendimento`.
- [ ] Criar action `Resolver`.
- [ ] Criar action `Cancelar`.
- [ ] Criar `Reabrir`, se incluído.
- [ ] Validar existência do chamado.
- [ ] Validar status atual antes da transição.
- [ ] Persistir alterações de forma assíncrona.

### Etapa 4 — Interface

- [ ] Atualizar `Detalhes`.
- [ ] Exibir status atual.
- [ ] Exibir `DataFechamento` quando aplicável.
- [ ] Mostrar apenas ações permitidas para o estado atual.
- [ ] Adicionar confirmação antes de cancelar.
- [ ] Adicionar feedback visual após operações.

### Etapa 5 — Dashboard

- [ ] Validar contadores.
- [ ] Corrigir consultas afetadas pela troca para enum.
- [ ] Conferir chamados recentes.

### Etapa 6 — Validação

- [ ] Criar chamado.
- [ ] Confirmar status `Aberto`.
- [ ] Iniciar atendimento.
- [ ] Confirmar `EmAndamento`.
- [ ] Resolver.
- [ ] Confirmar `DataFechamento`.
- [ ] Reabrir.
- [ ] Confirmar `DataFechamento = null`.
- [ ] Cancelar.
- [ ] Confirmar que o registro continua no banco.
- [ ] Testar transições inválidas.
- [ ] Executar `dotnet build`.

---

## 9. Critérios de aceite

- [ ] Novo chamado é criado como `Aberto`.
- [ ] Chamado aberto pode iniciar atendimento.
- [ ] Chamado em atendimento pode ser resolvido.
- [ ] Ao resolver, `DataFechamento` é preenchida.
- [ ] Ao reabrir, `DataFechamento` é removida.
- [ ] Cancelar não exclui o registro.
- [ ] O sistema impede transições inválidas.
- [ ] A interface mostra apenas ações compatíveis com o status.
- [ ] O dashboard continua exibindo contagens corretas.
- [ ] Todas as mudanças são persistidas no SQLite.
- [ ] O projeto compila sem erros.

---

## 10. Próxima evolução após esta entrega

Após concluir este fluxo, a evolução recomendada é implementar autenticação e autorização, formalizando os três perfis:

- `Solicitante`
- `Suporte`
- `Administrador`

Nesse momento, as actions criadas nesta funcionalidade deverão receber autorização específica para o perfil `Suporte` e, quando necessário, para `Administrador`.
