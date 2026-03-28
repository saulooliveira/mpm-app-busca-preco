# Prompt para geração automática dos testes prioritários

Use o prompt abaixo em um agente de código para implementar os testes automatizados dos cenários críticos priorizados.

---

Você é um **QA Automation Engineer Sênior em .NET 8** trabalhando no repositório `BuscaPreco`.

## Objetivo
Implementar testes automatizados para os cenários:
- **CT-010, CT-011, CT-014, CT-016, CT-018, CT-019, CT-023, CT-027, CT-032, CT-033**

## Contexto técnico do projeto
- Aplicação desktop `net8.0-windows` com WinForms + serviços de background.
- Fluxo principal: terminal TCP envia código -> serviço consulta DBF -> responde preço ou `#nfound`.
- Integrações relevantes: webhook HTTP, SMTP e logs diários.
- Há uma suíte E2E existente em `BuscaPreco/tests/BuscaPreco.E2E/BuscaPrecoSocketE2ETests.cs`.

## Escopo dos testes a gerar

### 1) CT-010 — Consulta positiva de preço via terminal
- Dado produto existente no DBF de fixture,
- Quando terminal envia código válido,
- Então deve receber resposta com descrição e preço formatado corretamente.

### 2) CT-011 — Produto não encontrado
- Dado código inexistente,
- Quando terminal consulta,
- Então deve receber `#nfound` e registrar status “Não Cadastrado”.

### 3) CT-014 — Cache hit
- Dado consulta inicial de código válido,
- Quando repetir consulta dentro do TTL,
- Então segunda resposta deve vir de cache (validar por comportamento e/ou log `Origem=Cache`).

### 4) CT-016 — Invalidação de cache por mudança do DBF
- Dado cache carregado,
- Quando DBF for alterado (timestamp/conteúdo),
- Então cache deve ser invalidado e nova leitura refletir alteração.

### 5) CT-018 — Webhook sucesso (2xx)
- Dado produto não encontrado e `WebhookUrl` configurada,
- Quando evento for disparado,
- Então deve executar POST JSON e tratar 2xx sem erro.

### 6) CT-019 — Webhook falha resiliente
- Dado webhook retornando 5xx/timeout,
- Quando produto não encontrado ocorrer,
- Então fluxo principal não deve quebrar e falha deve ser apenas registrada em log.

### 7) CT-023 — Relatório diário por e-mail
- Dado arquivo de log diário existente,
- Quando serviço enviar relatório,
- Então contadores de total/encontrados/não cadastrados devem ser consistentes e envio SMTP acionado.

### 8) CT-027 — Encerramento limpo via bandeja
- Dado aplicação ativa,
- Quando acionar saída pelo contexto de tray,
- Então servidor deve parar e recursos de UI/socket serem liberados sem exceção.

### 9) CT-032 — Smoke E2E existente
- Reaproveitar/corrigir E2E atual para realmente validar resposta final esperada no socket.
- Evitar asserção vazia quando o cenário esperado é resposta com conteúdo.

### 10) CT-033 — Segurança de transporte
- Criar teste de segurança (integração/arquitetura) que evidencie o risco atual:
  - bind em `IPAddress.Any`;
  - ausência de autenticação no socket.
- O teste pode ser marcado como `Skip`/`Trait("Security", "Risk")` se for teste de evidência (não bloqueante), mas deve documentar claramente o risco real observado no código.

## Requisitos de implementação
1. **Não alterar comportamento de produção sem necessidade.**
2. Preferir testes determinísticos e isolados (fixtures temporárias para DBF/logs).
3. Evitar dependência de rede externa real:
   - Para webhook: usar `HttpMessageHandler` fake/test server.
   - Para SMTP: usar fake/stub/mocking apropriado.
4. Organizar testes por tipo:
   - Unitários para regras de serviço;
   - Integração/E2E para socket/protocolo.
5. Usar convenções xUnit (`[Fact]`, `[Theory]`, `Assert.*`).
6. Nomear métodos no padrão:
   - `Deve_<Comportamento>_Quando_<Condicao>()`
7. Incluir comentários mínimos explicando setup crítico (especialmente socket e DBF).

## Entregáveis
- Novos arquivos de teste cobrindo os 10 cenários.
- Ajustes na suíte E2E existente para CT-032.
- Se necessário, test doubles helpers em pasta de testes.
- Resumo final com:
  - cenário -> arquivo de teste -> método de teste.

## Critérios de aceite
- Todos os cenários CT solicitados possuem ao menos 1 teste automatizado associado.
- Testes rodam localmente (quando ambiente suporta `dotnet`) sem flaky.
- Não há hardcode de credenciais reais.
- Falhas de integração externa são simuladas com doubles.

## Formato de saída esperado do agente
1. Lista de arquivos criados/alterados.
2. Diff resumido por cenário CT.
3. Comandos executados (`dotnet test ...`) e resultado.
4. Limitações de ambiente (se houver).

---

Se precisar escolher prioridade de implementação durante a execução, seguir esta ordem:
1) CT-010, CT-011, CT-014, CT-016  
2) CT-018, CT-019, CT-023  
3) CT-027, CT-032, CT-033
