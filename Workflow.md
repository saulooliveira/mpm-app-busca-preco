# Workflow de Contribuição

Este documento define o fluxo de trabalho de versionamento e contribuição do projeto **BuscaPreco**.

## Estratégia de Branching

Fluxo oficial:

```text
feature/* -> develop -> release/* -> master
```

### Regras práticas

- `feature/*`
  - Branches de desenvolvimento de funcionalidade/correção.
  - Ex.: `feature/tcp-timeout-handler`, `feature/log-consulta-estruturado`.
- `develop`
  - Branch de integração contínua da próxima versão.
  - Recebe merge/PRs originados de `feature/*`.
- `release/*`
  - Branch de estabilização para publicação.
  - Ex.: `release/1.0.3`.
  - Recebe PR da `develop`, com foco em validação final e ajustes de release.
- `master`
  - Produção.
  - Recebe somente código validado vindo de `release/*`.

## Padrão de Commits (Conventional Commits)

Formato recomendado:

```text
<tipo>(<escopo-opcional>): <descrição curta>
```

### Tipos mais usados

- `feat`: nova funcionalidade
- `fix`: correção de bug
- `chore`: tarefa técnica/manutenção
- `refactor`: refatoração sem alteração funcional
- `docs`: documentação
- `test`: testes
- `ci`: pipeline e automações

### Exemplos

```text
feat(application): adiciona serviço de consulta de preço por código de barras
fix(infrastructure): corrige tratamento de timeout no listener TCP
chore(deps): atualiza pacote dBASE.NET para versão compatível
docs(readme): documenta arquitetura em camadas e fluxo mermaid
ci(github-actions): adiciona build e testes para branches feature
```

## Boas práticas adicionais

- Prefira PRs pequenos e objetivos.
- Sempre valide build local antes de abrir PR.
- Em mudanças de configuração, nunca versionar segredos em `config.yaml`; use `config.example.yaml`.
