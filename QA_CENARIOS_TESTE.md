# Análise QA Sênior — BuscaPreco

## 1) Funcionalidades identificadas no repositório

1. **Inicialização da aplicação WinForms com DI e configuração YAML**: inicia host, registra serviços, sobe contexto de tray e background services.
2. **Servidor TCP para terminais Gertec**: abre socket, aceita conexões, gerencia lista de terminais e reconexão.
3. **Handshake/protocolo com terminal físico**: troca comandos de inicialização (`#ok`, `#config02?`, `#paramconfig?`, `#updconfig?`) e processa comandos subsequentes.
4. **Consulta de preço por código de barras**: recebe código, busca produto no repositório DBF e retorna descrição/preço ou “não encontrado”.
5. **Cache em memória de consultas e tracking de produtos**: evita leituras repetidas e mantém snapshot para uso promocional.
6. **Auditoria/log estruturado de consultas**: registra data/hora, código, status, origem (cache/banco), preço e nome.
7. **Alerta webhook para produto não cadastrado**: dispara POST assíncrono quando consulta não encontra produto.
8. **Screensaver promocional em idle**: após inatividade do terminal, envia produtos do cache para exibição no terminal.
9. **Relatório diário por e-mail**: agenda envio por horário configurado e resume consultas do log diário.
10. **Leitura e sincronização de base DBF**: copia DBF para diretório local da aplicação e reconstrói cache ao detectar modificação.
11. **Listagem completa de produtos e exportação MGV7**: carrega produtos filtrados do DBF e exporta arquivo texto em layout legado.
12. **Interface de bandeja (System Tray)**: status, forçar sincronização de preços, abrir tela de configuração e sair da aplicação.
13. **Tela de configuração (WinForms)**: coleta campos de configuração do terminal e grava arquivo `config.yaml` local.
14. **Teste E2E de socket+DBF**: valida fluxo de consulta via socket com fixture de DBF.

## 2) Cenários de teste

| ID | Funcionalidade | Cenário de Teste | Objetivo | Prioridade |
|---|---|---|---|---|
| CT-001 | Inicialização e DI | Subir aplicação com `config.yaml` válido e verificar host iniciado, tray visível e serviços registrados. | Garantir bootstrap funcional em ambiente padrão. | Alta |
| CT-002 | Inicialização e DI | Iniciar sem `config.yaml` ou com seção obrigatória ausente (`DbfConfig`, `Terminal`). | Validar falha controlada e diagnóstico por log. | Alta |
| CT-003 | Inicialização e DI | `config.yaml` com tipos inválidos (porta string, TTL negativo etc.). | Validar robustez de parsing e comportamento default/inválido. | Média *(inferido para hardening)* |
| CT-004 | Servidor TCP | Abrir servidor na porta configurada e conectar 1 terminal; confirmar inclusão em lista de conectados. | Garantir disponibilidade do serviço de rede principal. | Alta |
| CT-005 | Servidor TCP | Simular erro de bind (porta ocupada) e validar retentativa com `ReconnectDelayMs`. | Validar resiliência/recovery automático. | Alta |
| CT-006 | Servidor TCP | Chamar `Start` múltiplas vezes concorrentes. | Evitar múltiplos listeners/estado inconsistente. | Média |
| CT-007 | Protocolo terminal | Executar handshake completo e validar sequência esperada de comandos. | Confirmar compatibilidade com protocolo do equipamento. | Alta |
| CT-008 | Protocolo terminal | Enviar resposta de handshake malformada/curta. | Validar tratamento de erro sem crash global. | Alta |
| CT-009 | Protocolo terminal | Timeout consecutivo de leitura (sem resposta ao `#live?`) até desconexão forçada. | Garantir limpeza de sessão inativa. | Média |
| CT-010 | Consulta preço | Fluxo positivo: código existente retorna `#<descricao>|<preco>` ao terminal. | Validar jornada principal de negócio. | Alta |
| CT-011 | Consulta preço | Fluxo negativo: código inexistente retorna `#nfound` e gera log “Não Cadastrado”. | Validar resposta de erro funcional ao cliente. | Alta |
| CT-012 | Consulta preço | Borda: código recebido com `#`, `\0`, `\r\n` e espaços; normalização correta. | Garantir sanitização de entrada do terminal. | Alta |
| CT-013 | Consulta preço | Carga: alto volume de consultas concorrentes para mesmo código. | Validar performance e segurança de thread no cache. | Média |
| CT-014 | Cache de produto | Primeira consulta (banco) e segunda consulta (cache) dentro do TTL. | Confirmar ganho de desempenho e origem correta no log. | Alta |
| CT-015 | Cache de produto | Expiração do TTL e recarga do produto. | Validar coerência temporal do cache. | Média |
| CT-016 | Cache + DBF | Alterar arquivo DBF após carga e consultar novamente. | Verificar invalidação/reconstrução de cache ao detectar mudança física. | Alta |
| CT-017 | Auditoria/log | Validar formato mínimo de linha de auditoria e campos obrigatórios (código/status/origem/preço). | Assegurar rastreabilidade operacional e insumo para relatório. | Alta |
| CT-018 | Webhook alerta | Com `WebhookUrl` configurada e produto não cadastrado, validar POST JSON e status 2xx. | Garantir integração essencial de alerta. | Alta |
| CT-019 | Webhook alerta | Webhook fora do ar/5xx/timeout; operação principal deve continuar. | Validar isolamento de falha de integração externa. | Alta |
| CT-020 | Segurança webhook | Código com caracteres especiais no payload (`"`, `\n`, Unicode) sem quebrar JSON. | Detectar risco de payload inválido/injeção de conteúdo. | Alta |
| CT-021 | Screensaver promocional | Após inatividade > `IdleTimeoutSeconds`, enviar produto aleatório do cache para terminais conectados. | Validar comportamento promocional automático. | Média |
| CT-022 | Screensaver promocional | Produto com descrição >20 chars deve ser truncado para terminal. | Validar regra de borda de display/protocolo. | Baixa |
| CT-023 | Relatório diário e-mail | Com log diário existente, enviar e-mail e validar contadores (total/encontrados/não cadastrados). | Garantir fechamento operacional diário. | Alta |
| CT-024 | Relatório diário e-mail | Sem arquivo de log do dia: deve logar warning e não tentar envio inválido. | Validar falha segura sem exceção não tratada. | Média |
| CT-025 | Segurança credenciais | Validar que credenciais SMTP não aparecem em logs em erros comuns de envio. | Prevenir vazamento de segredo sensível. | Alta *(inferido para segurança operacional)* |
| CT-026 | Tela de bandeja | Menu “Forçar Busca de Preços” executa listagem e exibe balloon com quantidade. | Validar ação operacional manual. | Média |
| CT-027 | Tela de bandeja | Clique em “Sair” encerra servidor/socket e remove ícone sem processo órfão. | Garantir shutdown limpo da aplicação. | Alta |
| CT-028 | Tela configuração | Salvar configuração com dados válidos grava arquivo YAML no diretório da aplicação. | Validar persistência local de configuração. | Média |
| CT-029 | Tela configuração | Campo `time` não numérico ao salvar. | Validar validação de input e mensagem de erro ao usuário. | Média |
| CT-030 | Permissões de arquivo | Sem permissão de escrita no diretório de execução ao salvar `config.yaml`/copiar DBF. | Validar tratamento de erro de I/O e continuidade segura. | Alta |
| CT-031 | Exportação MGV7 | Exportar lista de produtos e validar layout fixo por campo/tamanho. | Garantir integração com sistema legado MGV7. | Média |
| CT-032 | Teste E2E existente | Executar suíte E2E e validar handshake/socket/fixture DBF sem regressão. | Usar cobertura atual como smoke de integração crítica. | Alta |
| CT-033 | Segurança de transporte | Avaliar exposição do socket em `IPAddress.Any` em rede aberta sem autenticação. | Evidenciar risco de acesso não autorizado à consulta. | Alta *(inferido como teste de segurança/pen-test)* |
| CT-034 | Autorização/permissão funcional | Verificar ausência de papéis/perfis: qualquer cliente TCP pode consultar preços. | Mapear limitação de autorização atual para mitigação futura. | Alta *(inferido com base no design atual)* |

## 3) Riscos críticos encontrados

1. **Ausência de autenticação/autorização no socket TCP**: qualquer cliente que alcance a porta consegue interagir com o serviço.
2. **Socket bindado em `IPAddress.Any`**: amplia superfície de ataque em redes internas/externas sem segmentação.
3. **Payload de webhook construído por concatenação de string**: risco de JSON inválido e possível manipulação de conteúdo com entrada não saneada.
4. **Credenciais SMTP em configuração local**: risco operacional caso arquivo/configuração/logs sejam expostos.
5. **Manipulação de parsing de protocolo por `Substring` sem validação forte**: entradas malformadas podem causar exceções e desconexões.

## 4) Possíveis lacunas de teste

1. Não há suíte unitária para serviços de aplicação (`BuscaPrecosService`, `ScreensaverPromocionalBackgroundService`, `RelatorioDiarioBackgroundService`).
2. Não há testes automatizados da UI WinForms (tray/configuração).
3. Falta teste automatizado de reconexão/recovery do servidor sob falhas reais de rede.
4. Falta teste de concorrência no cache em cenários de alta carga.
5. Falta teste de segurança (fuzzing de comandos TCP, varredura de exposição de porta, testes de secrets).
6. O E2E atual parece não validar retorno positivo de preço via socket até o fim (asserção final compara resposta vazia).

## 5) Priorização de automação (primeiro ciclo)

1. **Automatizar primeiro (Alta)**
   - CT-010, CT-011, CT-014, CT-016, CT-018, CT-019, CT-023, CT-027, CT-032, CT-033.
2. **Segundo ciclo (Média)**
   - CT-005, CT-007, CT-012, CT-013, CT-024, CT-026, CT-028, CT-031.
3. **Terceiro ciclo (Baixa/Complementar)**
   - CT-022 e cenários de UX complementar.

## 6) Plano de execução priorizado (solicitado)

Abaixo está a fila de execução priorizada **exatamente** com os cenários solicitados:

1. **CT-010** — Consulta positiva de preço via terminal.
2. **CT-011** — Produto não encontrado com retorno `#nfound`.
3. **CT-014** — Reconsulta com cache válido (origem cache).
4. **CT-016** — Invalidação de cache após mudança do DBF.
5. **CT-018** — Webhook de não cadastrado com retorno 2xx.
6. **CT-019** — Resiliência quando webhook falha (5xx/timeout).
7. **CT-023** — Relatório diário por e-mail com consolidação correta.
8. **CT-027** — Encerramento limpo pelo menu de bandeja.
9. **CT-032** — Execução da suíte E2E existente como smoke crítico.
10. **CT-033** — Segurança de transporte (exposição da porta TCP sem autenticação).

### Estratégia sugerida de automação para esta fila

- **Bloco A (regressão funcional crítica):** CT-010, CT-011, CT-014, CT-016.
- **Bloco B (integrações essenciais):** CT-018, CT-019, CT-023.
- **Bloco C (estabilidade operacional):** CT-027, CT-032.
- **Bloco D (segurança):** CT-033.

### Critério de pronto (DoD) para cada cenário priorizado

- Caso automatizado versionado com nome do ID do cenário.
- Evidência de execução (log/report) anexável.
- Resultado esperado validado com assert explícito.
- Sem dependência manual para execução básica em pipeline.
