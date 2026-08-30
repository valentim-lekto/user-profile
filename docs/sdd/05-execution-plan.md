# Plano de execução — User Profile

## Objetivo

Entregar a aplicação especificada em incrementos verticais verificáveis, executável por uma única origem com `docker compose up`, mantendo rastreabilidade entre requisitos, contrato, implementação e testes.

Somente um milestone pode estar em andamento. O próximo começa após o anterior cumprir seus gates, ter o diff revisado e registrar o resultado nesta página.

## Critérios de aceite relacionados

- `AC-REG-01`–`AC-REG-06` — cadastro e validações.
- `AC-LOGIN-01`–`AC-LOGIN-03` — autenticação e sessão.
- `AC-DASH-01`–`AC-DASH-04` — dashboard protegido.
- `AC-PROF-01`–`AC-PROF-05` — consulta e edição de perfil.
- `AC-PASS-01`–`AC-PASS-05` — alteração de senha, concorrência e encerramento da sessão.
- `UI-STATE-01`, `API-ERROR-01` — estados de UI e erros HTTP.
- `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` — segurança.
- `TECH-BACKEND-01`, `TECH-FRONTEND-01` — tecnologias obrigatórias e builds reproduzíveis.
- `OPS-DOCKER-01`–`OPS-DOCKER-03` — execução e persistência.
- `DOC-RUN-01`, `DOC-SDD-01`, `DOC-TRACE-01` — documentação e rastreabilidade.
- `TEST-FLOW-01`, `AI-SDD-01`, `AI-EXPLAIN-01`, `DEL-REPO-01` — qualidade e entrega.

## Contexto

- A especificação, o design, o OpenAPI, a estratégia de testes, a matriz e quatro ADRs estão definidos antes do scaffold.
- Não existe código de aplicação no início deste plano.
- Arquitetura: um executável `UserProfile.Api`, um projeto `UserProfile.Api.IntegrationTests`, Angular standalone e SQLite em volume.
- Versões e tags exatas estão em [`02-technical-design.md`](02-technical-design.md).
- O contrato normativo está em [`03-api-contract.yaml`](03-api-contract.yaml).
- O avaliador final deve precisar somente de Docker e Docker Compose.

## Milestones

### M1 — Walking skeleton e Docker

**Estado:** implementação concluída em 2026-08-25; a execução original de M2 resolveu o bloqueio do volume Docker padrão, e o smoke acumulado M1+M2 confirmou novamente o gate ao final da revisão independente de M2

Entregas:

- criar solution, `UserProfile.Api` com Controllers e o único projeto de integração;
- criar Angular standalone/strict com routing, Reactive Forms e Angular Material;
- fixar SDK com `rollForward: disable`, pacotes, bootstrap único dos `packages.lock.json` via `--use-lock-file`, `package-lock.json` e todas as tags Docker aprovadas; após versionar os locks, todo restore usa `--locked-mode`;
- implementar estrutura mínima por funcionalidades, `User` com os sete campos definidos internamente no ADR-0002, `DbContext`, configuração, migration inicial com timestamps/índice único e aplicação de migrations no startup;
- disponibilizar `/health` com checagem SQLite e Swagger/OpenAPI no backend;
- criar Dockerfiles multi-stage, Nginx same-origin encaminhando `/api`, `/swagger` e `/health`, com conversão explícita de `502/504` do upstream para `503 ProblemDetails`, volume e `compose.yaml` sem dependência de `.env`; a única probe do Compose roda no `web` com `wget -q -O /dev/null http://127.0.0.1:8080/health`, e `web` depende de `api` como `service_started`;
- criar `.env.example` opcional, sem valor utilizável, sem tornar sua cópia pré-requisito do Compose;
- criar testes mínimos de startup, migration, health, build e smoke do Compose;
- criar validação automatizada local da sintaxe/estrutura do OpenAPI; a integração desse check ao CI ocorre em M5.

Gates observáveis:

- backend e frontend compilam;
- testes de M1 passam;
- `docker compose up --build --wait` disponibiliza `http://localhost:8080`, `/health` e `/swagger` somente depois que a probe do `web` atravessa Nginx, API e SQLite por `/health`;
- com a API parada após startup, `/health` pelo Nginx retorna `503 application/problem+json` em vez de HTML, e a inspeção da configuração confirma o mesmo mapeamento para timeout `504`;
- nenhum `latest`, segredo ou porta pública adicional existe.

### M2 — Cadastro

**Estado:** implementação e revisão independente concluídas em 2026-08-25

Entregas:

- implementar os refinamentos internos de `PREM-INPUT-01`: limites pós-`Trim` de nome/email em `200/320`, limite de senha/confirmação em `128`, política ASCII explícita de email, nomes JSON camelCase sensíveis a caixa e corpo HTTP limitado a 1 MiB;
- implementar normalização, `PasswordHasher<User>`, rejeição de propriedades JSON não mapeadas ou com caixa incorreta e `POST /api/auth/register` conforme OpenAPI, incluindo `413/415 ProblemDetails` nas fronteiras aplicáveis;
- implementar tela de cadastro, validações reativas e estados loading/sucesso/erro;
- redirecionar ao login com mensagem de sucesso, sem criar sessão;
- implementar `BE-REG-*`, `FE-REG-*`, `BE-OAS-001`, `BE-ERR-001/002` e a parcela M2 de `OPS-COMPOSE-001`/`OPS-SECRET-001` da estratégia de testes.

Gates observáveis:

- cadastro válido retorna `201`, persiste hash e nunca retorna token;
- limites inclusivos, email ASCII, semântica pós-`Trim`, senha com espaços significativos e nomes JSON case-sensitive possuem bordas positivas e negativas observáveis;
- invalidações retornam `400`, duplicidade normalizada retorna `409`, corpo acima de 1 MiB retorna `413` e media type não suportado retorna `415`, sempre em ProblemDetails;
- o smoke versionado acumulado `scripts/validate-m1-compose.sh` comprova origem única, persistência após recriar a API e ausência dos marcadores sintéticos de query/body/header nos logs;
- todos os testes M1+M2 e checks documentais passam, o diff referencia `AC-REG-*`/`PREM-INPUT-01` e a revisão independente registra o resultado final.

### M3 — Login e dashboard

**Estado:** concluído em 2026-08-26

Entregas:

- implementar emissão/validação JWT, chave externa Base64 de ao menos 32 bytes, falha fechada para configuração inválida e fallback aleatório somente quando ausente em `Development`;
- completar em `.env.example` os nomes de configuração JWT introduzidos neste milestone, mantendo somente placeholders não utilizáveis;
- implementar `POST /api/auth/login` com `400 ValidationProblemDetails` para payload inválido, `401 ProblemDetails` idêntico para email inexistente/senha incorreta e sem refresh token;
- implementar sessão em `sessionStorage`, functional interceptor com allowlist apenas para URLs relativas protegidas e functional route guard;
- implementar `GET /api/profile` resolvendo exclusivamente o claim `sub` e retornando somente ID imutável, nome e email;
- implementar login e dashboard com boas-vindas, logout, navegação protegida e estados de UI; `/profile` recebe apenas um placeholder protegido como destino, sem antecipar os formulários do M4;
- implementar `BE-LOGIN-*`, `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PROF-001/002`, `TECH-BACKEND-001`, `FE-LOGIN-*`, `FE-GUARD-*`, `FE-INT-*`, `FE-DASH-*` e `FE-WIRE-*`, sempre verificando ProblemDetails, challenge Bearer obrigatório nos recursos protegidos e os DTOs da fatia.

Gates observáveis:

- login válido cria token de 15 minutos com claims mínimas e navega ao dashboard;
- login inválido retorna a mesma mensagem para email ou senha incorretos;
- dashboard não abre sem JWT válido e mostra o nome obtido da API;
- chave ausente/inválida fora de `Development` impede startup e o token nunca é enviado a destino público, absoluto ou externo;
- `.env.example` contém somente nomes e placeholders não utilizáveis, e o Compose continua iniciando sem copiá-lo;
- nenhuma operação recebe `userId` do cliente.

### M4 — Edição de perfil e senha

**Estado:** concluído em 2026-08-26

Entregas:

- implementar `PUT /api/profile` com validação e unicidade;
- implementar `PUT /api/profile/password` com senha atual, nova senha e confirmação;
- implementar tela de perfil com formulários separados para dados e senha;
- encerrar a sessão do frontend após troca de senha bem-sucedida;
- implementar `BE-PROF-003/004/005/006`, `BE-PASS-*`, `FE-PROF-*` e `FE-PASS-*`, incluindo autorização por `sub` dos dois endpoints novos e sempre verificando ProblemDetails e os DTOs da fatia.
- ampliar e executar o smoke Compose acumulado para os dois PUTs, falhas sem mutação, nova autenticação, persistência após recriação e logs seguros; observar o fluxo cadastral e os formulários na interface real sem antecipar o E2E automatizado de M5.

Gates observáveis:

- nome/email e senha mudam por operações separadas;
- erros usam ProblemDetails; email duplicado usa `409`;
- senha atual incorreta não altera o hash; nova senha válida passa a autenticar;
- sucesso de senha remove o JWT do `sessionStorage`;
- Compose confirma atualização/persistência e ausência das credenciais sintéticas nos logs, com recursos efêmeros isolados e removidos ao final.

### M5 — Testes E2E, CI e acabamento

**Estado:** concluído em 2026-08-26

Entregas:

- fixar Playwright e implementar somente `E2E-001`–`E2E-003`, conforme as jornadas independentes da estratégia atualizada: fluxo feliz com edição/logout, login inválido com rota anônima protegida e troca de senha com reautenticação;
- disponibilizar perfis Compose de qualidade e E2E para executar backend, frontend e navegador sem SDKs locais;
- configurar CI para restore/build/test backend, install/build/test frontend, contrato, imagens, E2E e Compose smoke, com artefatos somente em falha e cleanup obrigatório;
- auditar a cobertura acumulada de DTOs e ProblemDetails e concluir verificações de logs, segredo e tags;
- revisar acessibilidade básica dos formulários, feedback visual e submissões duplicadas;
- revisar dependências, imagens, configuração não sensível e ausência de escopo extra.

Gates observáveis:

- suíte completa passa sem skips;
- `E2E-001`–`003` atravessam Nginx, Angular, API e SQLite reais, usam dados isolados e não dependem de ordem ou seed;
- screenshot e trace são retidos somente em falha, sem sleeps fixos ou retries que escondam falha determinística;
- a indisponibilidade real do upstream continua comprovada pelo smoke acumulado do Compose;
- CI falha em drift de contrato, build, teste ou tag proibida;
- logs e artefatos não contêm senha, hash, token ou chave.

### M6 — Validação final e documentação

**Estado:** concluído tecnicamente em 2026-08-27. `AI-EXPLAIN-01` aguarda
confirmação humana e `DEL-REPO-01` aguarda associação/publicação explícita.

Entregas:

- criar/atualizar README raiz com comandos, URL, porta, health e procedimento de cadastro;
- validar checkout limpo somente com Docker e Docker Compose;
- validar recriação dos serviços preservando o volume SQLite;
- atualizar estado de todos os itens em `06-traceability.md`, plano e documentos SDD;
- revisar OpenAPI contra comportamento real, diff completo e decisões ADR;
- executar e registrar o walkthrough manual `DOC-EXPLAIN-001` sem transcrever conversas;
- registrar evidências finais e limitações; a publicação do repositório permanece ação explícita do responsável.

Gates observáveis:

- `TECH-*`, `OPS-COMPOSE-*`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-TAGS-001`, `OPS-SECRET-001` e `DOC-RUN-001` foram reexecutados e passaram;
- o roteiro `DOC-EXPLAIN-001` foi criado e revisado, sem promover `AI-EXPLAIN-01` antes da confirmação humana;
- documentação reproduz exatamente o ambiente observado;
- todos os critérios possuem evidência e estado final correto;
- build, testes, E2E e revisão do diff estão aprovados.

### Atividade pós-M6 — mutation testing do backend

**Estado:** concluída localmente em 2026-08-27; execução hospedada permanece pendente até publicação

Entregas:

- criar `BE-MUT-001` e `CI-MUT-001`, ligados a `NFR-TEST-01`/`TEST-FLOW-01`, sem novo requisito funcional ou ADR;
- fixar `dotnet-stryker` `4.16.0` em tool manifest raiz e configurar somente a allowlist crítica do backend no projeto de integração;
- criar target/profile Docker reproduzível, com HTML/JSON em diretório ignorado e sem depender de SDK local;
- executar a baseline temporária, classificar survivors e adicionar apenas testes xUnit ligados a comportamento observável;
- fixar o ratchet final a partir do score real e reexecutar com exit code zero;
- criar workflow próprio manual/semanal, sem gatilho de push/PR, e registrar a execução hospedada como pendente até ser observada.

Gates observáveis:

- suíte xUnit normal permanece verde e nenhum teste é enfraquecido;
- somente os arquivos-alvo possuem mutantes ativos/executados, sem `NoCoverage`, timeout ou erro inexplicado; fontes externas podem aparecer apenas como `Ignored` pelo mutate filter;
- configuração final possui `break > 0`, atende ao ratchet e produz HTML/JSON sem dado sensível;
- Compose, inventários, smoke, actionlint e `git diff --check` passam sem alterar comportamento da aplicação, OpenAPI ou frontend; refatorações internas behavior-preserving no JWT precisam permanecer cobertas;
- cleanup do profile não toca no volume da aplicação.

### Atividade pós-M6 — refinamento visual do frontend

**Estado:** concluída localmente em 2026-08-28, incluindo a simplificação final do dashboard

Entregas:

- modernizar somente a apresentação do shell, login, cadastro, dashboard e perfil, sem alterar API, navegação, payloads ou regras de negócio;
- usar Angular Material e CSS proporcional, com paleta inspirada na linguagem pública da Lekto sem copiar marca ou ativos;
- manter o `id` imutável no `ProfileResponse`, mas retirar sua exposição visual por não fazer parte dos dados cadastrais exigidos pelo enunciado;
- retirar do dashboard os três cartões meramente descritivos de dados pessoais, senha e sessão, preservando o hero, o resumo do perfil e as ações reais;
- preservar labels, nomes acessíveis, hierarchy de headings, `aria-live`, foco, loading, bloqueio de duplo submit e os seletores das jornadas existentes;
- ampliar as asserções responsivas das jornadas existentes, sem criar uma nova jornada E2E.

Gates observáveis:

- lint, testes e build do frontend passam no profile Docker existente;
- `E2E-001`–`003` continuam verdes e comprovam ausência de overflow em 360 px também nas superfícies de autenticação;
- teste focado e `E2E-001` comprovam que o `id` recebido continua fora do DOM, enquanto nome/email permanecem carregados e editáveis;
- `FE-DASH-001` comprova que a saudação, o resumo, a navegação e o logout permanecem disponíveis sem os três cartões descritivos;
- inspeção real desktop/mobile confirma composição, contraste, foco visível e console limpo;
- backend, OpenAPI, banco, autenticação e Compose principal não recebem mudança funcional.

### Atividade pós-M6 — correção responsiva da revisão

**Estado:** concluída localmente em 2026-08-28

Entregas:

- priorizar o formulário de login/cadastro quando a viewport for simultaneamente estreita e baixa;
- manter a ordem visual das ações móveis igual à ordem do DOM e do foco por teclado;
- limitar somente a apresentação do nome defensivo de 200 caracteres no hero e no resumo do dashboard, preservando seu texto integral no DOM e no perfil;
- ampliar `E2E-001` para comprovar os três comportamentos, as quatro telas em 320 px, o formulário completo em landscape e ambas as ações do dashboard, sem criar nova jornada ou alterar API, estado, navegação ou regra de negócio.

Gates observáveis:

- regressões novas falham contra o layout anterior e passam após a correção;
- lint, suíte frontend, build e três jornadas E2E permanecem verdes;
- navegador real confirma login/cadastro em landscape curto, sequência de foco coerente e dashboard utilizável em 320/360 px com nome no limite;
- `git diff --check` passa e backend, OpenAPI, banco, Compose e contratos permanecem inalterados.

### Atividade pós-M6 — robustez de persistência após revisão DB

**Estado:** concluída localmente em 2026-08-28

Entregas:

- tornar a troca de senha um compare-and-swap atômico pelo `Id` do `sub` e pelo hash observado, sem migration ou token de concorrência global;
- exigir no healthcheck igualdade entre os conjuntos de IDs de migrations aplicadas e esperadas, preservando timeout de um segundo e cancelamento;
- restaurar no Compose o timeout SQLite de cinco segundos, mantendo margem para os 30 segundos do proxy;
- fortalecer a corrida de cadastro para validar integralmente o `409 ProblemDetails` do caminho autoritativo do índice único;
- atualizar contrato, estratégia, rastreabilidade e baseline de mutation testing afetada pelos arquivos críticos.

Gates observáveis:

- regressões novas falham contra o comportamento anterior e passam após as correções;
- suíte backend Docker, contrato OpenAPI, configuração Compose e smoke acumulado passam;
- mutation testing é recalibrado a partir de execução real, sem manter contagens ou `CompileError` obsoletos;
- nenhuma senha, hash, token, chave ou banco entra no diff, logs ou relatórios;
- `git diff --check` passa e frontend/funcionalidades de negócio permanecem fora do escopo.

### Atividade pós-M6 — recuperação de startup e eficiência de queries

**Estado:** concluída localmente em 2026-08-28

Entregas:

- recuperar, sob a premissa de instância única, somente a tabela técnica `__EFMigrationsLock` que pode permanecer órfã após interrupção, limitando preparação e aplicação das migrations e preservando histórico/dados;
- abrir uma única conexão SQLite por execução do health check;
- evitar a consulta de conflito de email quando `NormalizedEmail` não mudou, mantendo o índice único como garantia autoritativa;
- trocar o `LIKE` do teste de migration pela comparação do ID exato versionado;
- manter API, schema, frontend e funcionalidades de negócio inalterados.

Gates observáveis:

- regressões reais comprovam recuperação do lock órfão, falha de schema inválido, uma abertura de conexão no health e ausência da consulta redundante de conflito;
- suíte backend Docker, contrato, configuração Compose e smoke acumulado passam;
- mutation testing é reexecutado e sua baseline é atualizada somente a partir do relatório real;
- `git diff --check` passa e nenhum segredo, banco ou relatório gerado é versionado.

### Atividade pós-M6 — fechamento da revisão completa de queries/startup

**Estado:** concluída localmente em 2026-08-28

Entregas:

- mover a rotina de migrations para `IHostedLifecycleService.StartingAsync`, depois do registro dos sinais do host e antes do listener HTTP;
- comprovar `SIGTERM` em processo real durante startup bloqueado, além das provas já existentes de deadline e lock órfão;
- manter o lifecycle fora da allowlist Stryker aprovada, mas documentar sua responsabilidade crítica e a cobertura por integração/processo;
- reutilizar `ApiFactory.WithInterceptor` e os endpoints HTTP nos testes de abertura única do health e ausência de precheck redundante;
- manter o teste direto com dois `DbContext` para a corrida da própria conta, pois essa condição exige estado rastreado obsoleto deliberado.

Gates observáveis:

- regressão de lifecycle falha no código anterior e passa depois da movimentação;
- testes focados e suíte backend Docker passam sem enfraquecimento;
- mutation testing, contrato, configuração e smoke acumulado permanecem aprovados;
- documentação registra somente resultados realmente observados; `git diff --check` e scan de artefatos/segredos passam.

### Atividade pós-M6 — fortalecimento dos oráculos de startup e queries

**Estado:** concluída localmente em 2026-08-30

Entregas:

- tornar a ausência de listener durante migrations observável independentemente do filtro global de logs;
- provar que o cancelamento do host alcança o token consumido pela operação, não somente um callback no token bruto;
- exigir saída zero do subprocesso e normalizar somente o cancelamento solicitado pelo host antes da prontidão;
- contar consultas LINQ síncronas e assíncronas no gate que proíbe o precheck redundante;
- manter API HTTP, schema, frontend e regras de negócio inalterados.

Gates observáveis:

- mover temporariamente a migration de `StartingAsync` para `StartedAsync` reprova o teste de prontidão;
- substituir temporariamente o CTS ligado por um CTS independente reprova o teste de cancelamento;
- remover a normalização na fronteira do processo reproduz exit code `134`, enquanto o caminho corrigido exige zero;
- reintroduzir temporariamente o precheck síncrono com `Any()` reprova o teste de contagem;
- os três mutantes descartáveis são removidos antes do build e da suíte backend completa.

## Progresso

- `2026-08-24` — `design concluído` — artefatos de design e planejamento criados; M1–M6 permanecem pendentes e nenhum código foi implementado.
- `2026-08-24` — `revisão independente concluída` — commit `b184432` auditado; 0 High, 15 Medium e 6 Low corrigidos; contrato, IDs, links, segredos e diff revalidados em [`review-log.md`](review-log.md); M1–M6 permanecem pendentes.
- `2026-08-24` — `M1 iniciado` — estrutura de testes alinhada a `tests/backend`, proxy Swagger e `.env.example` antecipado por instrução explícita; implementação e evidências ainda pendentes.
- `2026-08-25` — `M1 concluído` — walking skeleton backend/frontend, migration SQLite, testes, imagens multi-stage e origem única validados após auditoria final; M2–M6 permanecem pendentes e nenhum endpoint de negócio foi criado.
- `2026-08-25` — `revisão independente de M1 concluída` — commit `8db5592` auditado; 1 High, 12 Medium e 11 Low confirmados. O High e 11 Medium foram corrigidos; 1 Medium operacional ficou bloqueado naquele momento pela VM Docker sem espaço e foi encerrado em M2; 8 Low triviais foram corrigidos e 3 Low adiados com justificativa em [`review-log.md`](review-log.md).
- `2026-08-25` — `implementação original de M2 concluída` — cadastro vertical Angular → Nginx → Controller → SQLite entregue com hash Identity, duplicidade normalizada/concorrente e estados acessíveis; a execução então observada registrou 29 integrações backend, 12 testes frontend e smoke no volume Docker padrão.
- `2026-08-25` — `revisão independente de M2 concluída` — o commit `c02b67f` foi auditado; 1 High, 10 Medium e 5 Low foram confirmados e corrigidos. `PREM-INPUT-01`, email ASCII, validação pós-`Trim`, JSON case-sensitive, `413/415 ProblemDetails`, política de logs e smoke acumulado M1+M2 foram revalidados com 36 integrações backend, 13 testes frontend e runtime Docker isolado; detalhes em [`review-log.md`](review-log.md).
- `2026-08-26` — `M3 iniciado` — contrato atualizado antes do código para refletir a instrução explícita desta fatia: credenciais não reconhecidas usam `401` genérico e `ProfileResponse` contém somente `id`, `name` e `email`.
- `2026-08-26` — `M3 concluído` — login/JWT, proteção de rotas/endpoints, sessão em `sessionStorage`, dashboard e placeholder protegido de perfil foram validados por 69 integrações backend, 42 testes frontend, OpenAPI normativo, smoke Compose isolado e UI real. M4–M6, E2E completos e CI permanecem pendentes.
- `2026-08-26` — `revisão independente de M3 concluída` — o commit `b1f2468` foi auditado; 1 High, 5 Medium e 4 Low foram confirmados e corrigidos. Concorrência entre sessões, `401` tardio, Swagger runtime, logs, wiring real do Angular, bordas, estabilidade e rastreabilidade foram revalidados com 69 integrações backend, 45 testes frontend e smoke isolado; detalhes em [`review-log.md`](review-log.md).
- `2026-08-26` — `M4 iniciado` — critérios e contrato foram revalidados antes do código; atomicidade das operações inválidas, payload cadastral sem campos de senha e atualização do dashboard por nova consulta passaram a ter gates explícitos. Implementação e evidências ainda estão pendentes.
- `2026-08-26` — `M4 concluído` — os PUTs protegidos de perfil/senha e os dois formulários separados foram aprovados com 99 integrações backend, 55 testes frontend, OpenAPI normativo/runtime, smoke Compose acumulado e UI real. M5–M6, E2E completos, CI e documentação final permanecem pendentes.
- `2026-08-26` — `revisão independente de M4 concluída` — 0 High, 4 Medium e 1 Low foram corrigidos; isolamento de sessão tardia, bloqueio dos formulários, wiring DOM, associações do Swagger runtime e bordas inclusivas passaram em 101 integrações backend e 56 testes frontend, além do smoke acumulado.
- `2026-08-26` — `M5 iniciado` — as três jornadas E2E foram alinhadas à instrução atual antes do código; a prova de upstream indisponível permanece no smoke Compose, e perfis de qualidade/E2E, CI e acabamento visual entram nesta única etapa sem funcionalidade de negócio nova.
- `2026-08-26` — `M5 concluído` — 101 integrações backend, 57 testes frontend, contrato, smoke acumulado e três jornadas Playwright independentes passaram em perfis Docker; acabamento Material/responsivo/acessível, workflow CI, tags fixas, cleanup isolado e artefatos sem credenciais foram revalidados. Somente M6 permanece pendente.
- `2026-08-26` — `revisão independente de M5 concluída` — o commit `eaad3cd` foi auditado; 0 High, 8 Medium e 11 Low foram corrigidos, incluindo seis inconsistências triviais descobertas na re-revisão do próprio patch corretivo. Provas E2E de nomes acessíveis/persistência/reproteção, inventário exato, dependências, diagnósticos, cleanup, CI imutável e rastreabilidade passaram nos gates finais; detalhes em [`review-log.md`](review-log.md).
- `2026-08-27` — `M6 concluído tecnicamente` — quatro auditorias independentes e a re-revisão final encerraram todos os Médios, inclusive o bind publicado fora do loopback e a afirmação incorreta sobre CSP. Build sem cache, Compose sem `.env`, URLs, restart/persistência, 101 integrações, 57 testes frontend, três E2E, contrato, smoke e actionlint passaram somente com Docker. README, relatório final, plano, matriz, índice, `.env.example` e uso de IA foram fechados; publicação e confirmação humana permanecem explicitamente externas.
- `2026-08-27` — `revisão independente pós-M6 concluída` — o commit documental `ee2933d` e a entrega acumulada foram auditados contra `3f6fbc4`; 0 High, 2 Medium e 2 Low foram confirmados e corrigidos. A expiração do JWT agora reprotege dashboard/perfil já ativos, inclusive com parâmetros de URL; chamadas protegidas sem token válido não saem anonimamente; timers respeitam sessão/rota/lifecycle e a evidência corrente foi atualizada. Regressões vermelhas, 64 testes frontend, 101 integrações backend, contrato, 3 E2E, smoke e actionlint foram observados; detalhes em [`review-log.md`](review-log.md).
- `2026-08-27` — `correção da revisão completa concluída` — o snapshot `a73d33b` foi revisado por correção/segurança, stale, simplicidade e KISS. A primeira execução Docker oficial do frontend falhou por sete timeouts e a repetição na mesma imagem passou 64/64; o runner agora carrega timeout global de 30 segundos sem retry. A re-revisão do patch removeu ainda dois overrides locais obsoletos de 10 segundos que anulavam parcialmente essa política. Três decoders idênticos de `ProblemDetails` foram consolidados em `core/http`, com três testes defensivos e sem alterar o contrato HTTP ou criar nova camada. O profile reconstruído e duas execuções simultâneas finais aprovaram lint, 67/67 testes em 10 arquivos e build; backend 101/101, OpenAPI e Compose config também passaram.
- `2026-08-27` — `mutation testing pós-M6 iniciado` — `BE-MUT-001`/`CI-MUT-001`, allowlist crítica, execução Docker, política de baseline/ratchet e workflow manual/semanal foram especificados antes da implementação; score e evidência permanecem pendentes até execução real.
- `2026-08-27` — `mutation testing pós-M6 concluído localmente` — suíte normal com 111 integrações, baseline limpa de 97,41%, ratchet 97/97/97, profile Docker, relatórios HTML/JSON, inventários, smoke e workflow manual/semanal foram implementados e validados; somente a execução hospedada continua pendente.
- `2026-08-28` — `refinamento visual ajustado ao escopo original` — o `id` imutável permaneceu no `ProfileResponse`, conforme contrato M3, mas foi removido do DOM da tela de perfil por não ser dado cadastral exigido pelo enunciado. A regressão falhou primeiro contra a apresentação anterior e, após a remoção direta do bloco/estilos, lint, 67/67 testes, build e 3/3 E2E passaram.
- `2026-08-28` — `dashboard simplificado ao escopo do desafio` — os três cartões sem ação de dados pessoais, senha e sessão foram retirados por repetirem funções já acessíveis no perfil/logout. A regressão falhou primeiro com 67/68 testes; depois da remoção direta do markup e dos estilos órfãos, lint, 68/68 testes, build, 3/3 E2E e inspeção real do dashboard passaram.
- `2026-08-28` — `correção responsiva da revisão concluída` — três regressões sequenciais reproduziram formulário fora da viewport em `667×375`, ordem visual inversa ao Tab em `360×800` e nome defensivo sem limite vertical. Media query, ordem de coluna e clamps CSS diretos encerraram os achados; lint, 68/68 testes, build, 3/3 E2E e inspeção real em `320×568`, `360×800` e `667×375` passaram.
- `2026-08-28` — `robustez de persistência pós-revisão DB concluída` — regressões reproduziram health falso-positivo e duas trocas concorrentes aceitas; CAS por hash, conjunto exato de migrations, timeout SQLite de cinco segundos e contrato do `409` concorrente foram implementados. Build/113 integrações, OpenAPI, config/smoke Compose e Stryker 97,47% passaram; frontend e schema permaneceram inalterados.
- `2026-08-28` — `recuperação de startup e eficiência de queries concluídas` — três regressões reproduziram lock técnico órfão sem recuperação, duas aberturas por health e precheck redundante para email canonicamente igual. A re-revisão acrescentou provas para as duas fases do deadline e a corrida do próprio usuário. O patch direto recupera somente `__EFMigrationsLock` na instância única, aplica deadline total de 15 segundos, reutiliza uma conexão, evita o precheck quando a chave canônica não muda, exclui corretamente a própria conta quando muda e usa ID exato no oráculo de migration. Build/119 integrações, contrato e config/smoke Compose passaram; API, schema e frontend permaneceram inalterados.
- `2026-08-28` — `fechamento da revisão completa de queries/startup concluído` — migrations passaram para `IHostedLifecycleService.StartingAsync`, depois do registro dos sinais do host e antes do listener. Uma integração em subprocesso envia `SIGTERM` durante um lock real, observa o cancelamento cooperativo e comprova saída antes do deadline interno sem prontidão ou resíduo técnico. Os testes de health/perfil reutilizam `ApiFactory.WithInterceptor` e HTTP real; o cenário deliberadamente concorrente continua direto. Build/120 integrações, contrato, configuração/smoke, probes e Stryker 97,47% passaram.
- `2026-08-30` — `oráculos de startup e queries fortalecidos` — o subprocesso tornou o log de lifetime observável, um teste direto comprovou o token entregue a `MigrateAsync` e o observer passou a contar queries síncronas/assíncronas. Durante a organização dos commits, exigir exit code zero revelou o abort `134` que o oráculo anterior aceitava; a fronteira de execução passou a normalizar somente `SIGTERM` anterior à prontidão. O teste focado, build e 121/121 integrações, contrato, Compose, smoke e três probes `200` passaram; a baseline Stryker anterior permanece aplicável porque lifecycle e `Program.cs` estão fora da allowlist.

## Evidências de M1

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend | SDK `10.0.400`: `dotnet restore UserProfile.sln --locked-mode`, `dotnet build UserProfile.sln --no-restore` e `dotnet test UserProfile.sln --no-restore --no-build --verbosity normal` | A revisão independente constatou que o comando original retornava sucesso sem descobrir testes. Depois de marcar o projeto de integração como projeto de teste, o restore locked passou, o build terminou com 0 warnings/0 erros e o VSTest descobriu e aprovou 6/6 integrações: schema SQLite exato, health `200/503` com limite de duração, contrato OpenAPI runtime de `/health`, falha de startup e `404 ProblemDetails`. |
| Frontend | Node `24.19.0`: `npm ci`, `npm run lint`, `npm test` e `npm run build` | Lint aprovado; 2/2 testes aprovados; bundle de produção com 265,02 kB bruto. |
| Dependências frontend | `npm audit --package-lock-only --audit-level=low` | 0 vulnerabilidades após atualizar Vitest de `4.0.8` para `4.1.11` por `GHSA-5xrq-8626-4rwp`. |
| Contrato | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` e mutação negativa do `operationId` em cópia temporária | `SPEC-OAS-001`–`005` aprovados para 6 operações e 42 referências locais; a cópia inválida foi rejeitada como esperado. |
| Compose | `docker compose config --quiet`; tentativas padrão; `env COMPOSE_PROJECT_NAME=user-profile-m1-review-verified COMPOSE_FILE=compose.yaml:/private/tmp/user-profile-m1-review-verified.override.yaml M1_REVIEW_DATA_DIR=/private/tmp/user-profile-m1-review-verified-data.fbw7w5 scripts/validate-m1-compose.sh` sem `.env` | Configuração base aprovada. Tags, imagens, usuários, portas, SPA, health, Swagger, `404`, conversão `502/504` → `503` e teardown foram aprovados pelo script final em projeto isolado; somente o backing do volume nomeado foi desviado para o host. A repetição com volume Docker padrão estava bloqueada naquela revisão e foi aprovada posteriormente em M2. |
| Origem única | `curl` em `/`, rota SPA, `/health`, `/swagger/index.html`, `/swagger/v1/swagger.json` e `/api/not-implemented` | SPA/fallback, health e Swagger responderam pela porta 8080; rota API inexistente preservou `404 application/problem+json`; API sem binding de host. |
| Falha do upstream | `docker compose stop api` e `curl http://localhost:8080/health` | Nginx retornou `503 application/problem+json` com corpo ProblemDetails; `nginx -T` confirmou mapeamento explícito de `502` e `504`. |
| Cleanup | `docker compose down` sem `-v` | Contêineres/rede deste projeto removidos e volume `user-profile-sdd-challenge_user-profile-data` preservado. |

O primeiro startup do Compose encontrou `SQLite Error 13` porque a VM Docker compartilhada estava sem espaço. Foram removidos somente três volumes temporários criados durante esta validação (`user-profile-m1-node-modules`, `user-profile-m1-node-modules-clean` e `user-profile-m1-nuget`). Na repetição final após corrigir os timestamps, o disco voltou a ficar cheio: o volume vazio/stale deste próprio projeto foi recriado, uma imagem backend anterior e dois registros exatos do cache de `dotnet publish UserProfile.Api` foram removidos. Nenhum recurso de outro projeto foi apagado. Com 150 MB livres, o Compose reconstruiu, ambos os serviços ficaram saudáveis, os smokes passaram e o teardown final preservou o novo volume correto. O hash do commit local é informado no handoff da etapa, pois um commit não pode registrar o próprio hash em seu conteúdo.

Na revisão independente posterior, a VM voltou a ficar sem espaço e volumes Docker normais falharam antes da migration. `docker compose config` continuou aprovado e o smoke final completo passou em projeto isolado, usando o mesmo volume nomeado com armazenamento temporariamente apoiado no host. Essa adaptação e o cleanup pontual estão detalhados em [`review-log.md`](review-log.md). A execução original de M2 e a nova execução acumulada M1+M2 usaram volumes Docker normais e encerraram `REV-M1-020`; o volume principal nunca foi alterado pela revisão.

## Evidências de M2 após a revisão independente

A baseline anterior à revisão registrava 29 integrações backend e 12 testes frontend. As execuções abaixo são posteriores às correções e comprovam a suíte acumulada atual.

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend | Imagem `mcr.microsoft.com/dotnet/sdk:10.0.400-noble`: restore locked, build e teste da solution | Restore aprovado; build com 0 warnings/0 erros; 36/36 integrações descobertas e aprovadas. Além da baseline, a suíte prova bordas inclusivas `3/200` e `6/128`, senha com espaços significativos, emails Unicode rejeitados, JSON case-sensitive, relógio fixo e Swagger corrigido. |
| Frontend | Imagem `node:24.19.0-bookworm-slim`: `npm ci`, lint, teste e build de produção | 494 pacotes instalados com 0 vulnerabilidades; lint aprovado; 13/13 testes aprovados; bundle de produção com 494,59 kB bruto. O teste DOM comprova mensagens locais e ausência de request inválida. |
| Contrato | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml`; `BE-OAS-001` na suíte backend | `SPEC-OAS-001`–`005` aprovados para seis operações e 52 referências locais; Swagger runtime aprovou extensões pós-`Trim`, padrão ASCII, ausência das constraints raw contraditórias e respostas `413/415`. |
| Compose, persistência e logs | `scripts/validate-m1-compose.sh` | O smoke acumulado criou projeto/volume efêmeros, aprovou origem única, `201/400/409/413/415`, recriou a API e confirmou persistência pelo novo `409`; marcadores sintéticos de query/body/header não apareceram nos logs da API/Nginx. |
| UI observável | `register.spec.ts` na imagem Node e SPA construída/servida no smoke | Validadores, mensagens locais renderizadas, ausência de request inválida, loading, duplo submit, sucesso e estados remotos passaram. A inspeção manual original permanece histórica; nenhuma nova alegação de navegador foi necessária. |
| Cleanup | Trap do smoke com `docker compose down --volumes --remove-orphans`; inspeção somente leitura posterior | Contêineres, rede e volume `user-profile-m2-smoke-17663_*` foram removidos; o volume normal do repositório não participou da execução. |

Na execução original de M2, o bloqueio ambiental registrado na revisão de M1 não se repetiu. O smoke acumulado pós-correções confirmou novamente a configuração em volume Docker normal isolado, encerrando documentalmente `REV-M1-020`. A autenticação posterior à recriação continua reservada a M3; em M2, a persistência é comprovada pela colisão normalizada após recriar a API.

## Evidências de M3

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend | Imagem `mcr.microsoft.com/dotnet/sdk:10.0.400-noble`: restore locked, build e `dotnet test UserProfile.sln -p:RestoreLockedMode=true --verbosity minimal`, com `TreatWarningsAsErrors` | Restore e build aprovados sem warnings; 69/69 integrações aprovadas, sem falhas ou skips. A suíte cobre login normalizado, JWT válido/inválido/expirado, issuer/audience/assinatura/algoritmo, `sub`, DTO mínimo, resposta `401` idêntica e configuração da chave. |
| Frontend | Imagem `node:24.19.0-bookworm-slim`: `npm ci`, `npm run lint`, `npm test` e `npm run build` | 494 pacotes instalados, 0 vulnerabilidades, lint aprovado, 45/45 testes aprovados e build sem warnings (317,37 kB bruto; 87,64 kB estimado). Além da cobertura anterior, as regressões provam isolamento entre sessões, `401` tardio, wiring real de guard/interceptor e bordas inclusivas de login. |
| Contrato | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml`; `BE-OAS-001` na suíte backend | OpenAPI normativo aprovado para seis operações e 53 referências locais; o Swagger runtime também comprova `security: []` nas operações públicas e constraints/read-only exatos de `LoginResponse`/`ProfileResponse`. |
| Compose e segurança | `docker compose config --quiet`; `scripts/validate-m1-compose.sh` em projeto/volume isolados | Configuração aprovada. O smoke acumulado M1+M2+M3 validou origem única, cadastro, login normalizado, `401` byte-idêntico para email inexistente/senha errada, challenge Bearer, perfil exclusivamente pelo `sub` com DTO exato, `413/415`, ausência da senha válida, dos marcadores e do primeiro JWT nos logs, recriação da API com novo token e persistência, `503` do proxy e cleanup dos recursos efêmeros. |
| UI real | `http://localhost:8080` no Compose padrão, encerrado com `docker compose down --remove-orphans` sem volumes | Guard anônimo, erro genérico de login, login válido/dashboard com nome, navegação ao placeholder de perfil, logout e reproteção foram observados; console sem warnings/erros. O volume nomeado padrão foi preservado. |

O SDK/Node do host não correspondiam exatamente às versões fixadas; por isso, as validações de build/teste foram executadas nas imagens Docker fixadas pelo design. Não houve desvio de versão, segredo real ou remoção de recursos de outros projetos.

## Evidências de M4

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend | Imagem `mcr.microsoft.com/dotnet/sdk:10.0.400-noble`: restore locked, build e teste da solution com warnings como erros | Restore locked aprovado; build com 0 warnings/0 erros; 101/101 integrações aprovadas, sem falhas ou skips. Os cenários cobrem atualização válida, bordas inclusivas de email/senha atual, validações, conflito normalizado/race do índice, isolamento por `sub`, overposting, timestamps, senha atual/confirmação inválidas sem mutação, troca válida, autenticação exclusiva com a nova senha e associações request/response do Swagger runtime. |
| Frontend | Imagem `node:24.19.0-bookworm-slim`: `npm ci`, `npm run lint`, `npm test` e `npm run build` | Instalação com 0 vulnerabilidades, lint aprovado, 56/56 testes aprovados sem skips e build sem warnings (317,36 kB bruto; 87,58 kB estimado). A suíte prova wiring DOM, carregamento, validadores, payload somente com nome/email, bloqueio integral/duplo submit, estados de sucesso/erro e isolamento de uma sessão nova diante do sucesso tardio da troca de senha. |
| Contrato | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml`; Swagger runtime na suíte backend | OpenAPI normativo aprovado para seis operações e 53 referências locais; o contrato runtime dos dois PUTs, schemas, autenticação Bearer, responses e `ProblemDetails` também passou. |
| Compose, persistência e segurança | `docker compose config --quiet`; `scripts/validate-m1-compose.sh` em projeto/volume efêmeros | Configuração aprovada. O smoke acumulado M1+M2+M3+M4 validou os dois PUTs, autorização por `sub`, conflitos/validações sem alteração parcial, senha antiga rejeitada e nova aceita, persistência após recriação, logs sem credenciais/marcadores, `413/415/503` e cleanup integral dos recursos efêmeros. |
| UI real | `http://localhost:8080` no Compose padrão | Login, dashboard, navegação ao perfil, carregamento dos dados, atualização cadastral e novo nome após retornar ao dashboard foram observados; confirmação de senha divergente apresentou mensagem acessível e o console permaneceu sem warnings/erros. A submissão final da troca de senha no navegador não foi executada, pois o mesmo comportamento já foi coberto pelas suítes e pelo smoke automatizado. |
| Cleanup | Stack padrão encerrada com `docker compose down --remove-orphans`, sem `-v` | Contêineres/rede da stack foram encerrados e o volume nomeado padrão foi preservado; o smoke removeu somente seus recursos efêmeros isolados. |

O SDK e o Node disponíveis no host divergiam dos patches fixados; por isso restore/build/test foram executados nas imagens específicas do design, sem alterar locks ou versões. Nenhum push foi realizado.

## Evidências de M5

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend acumulado | Perfil `backend-tests` do Compose, target `test` da imagem .NET `10.0.400-noble`: restore locked, build Release com warnings como erros e teste da solution | 101/101 integrações aprovadas, sem falhas ou skips. A suíte usa `HttpClient`/`WebApplicationFactory`, EF Core e SQLite isolado reais e cobre banco vazio/migrations, cadastro, duplicidade, login, JWT, isolamento por `sub`, PUTs, senha, ProblemDetails e ausência de dados sensíveis. |
| Frontend acumulado | Perfil `frontend-tests` do Compose, target `test` da imagem `node:24.19.0-bookworm-slim`: npm `11.17.0`, `npm ci` com allowlist estrita, lint, Vitest e build de produção | A primeira prova negativa rejeitou dois scripts transitivos ainda não decididos; após negá-los explicitamente, instalação com 0 vulnerabilidades, lint, 57/57 testes sem skips e build de 317,42 kB passaram. |
| Contrato e configuração | Perfil `contract-tests` com `ruby:3.4.10-slim-bookworm`; configuração com todos os perfis; `actionlint:1.7.12` | OpenAPI aprovado para seis operações e 53 referências locais; configuração padrão/todos os profiles e workflow aprovados; imagens/estágios usam inventário exato e Actions usam SHAs completos. |
| E2E real | `./scripts/e2e-playwright.sh` com `mcr.microsoft.com/playwright:v1.62.0-noble`, Nginx/API/SQLite reais, projeto/volume por suíte e contexto/dados por jornada | `E2E-001`–`003` passaram sem retry ou seed: labels/roles acessíveis, nome/email persistidos, logout e troca de senha reprotegendo o dashboard, senha antiga rejeitada e nova aceita. Waits usam health/estado observável e o teardown removeu somente os recursos efêmeros próprios. |
| Artefatos e segurança | Trace forçado da implementação original; relatório/JUnit da correção; probe automatizada do filtro compartilhado | 0 senha concreta, JWT ou Bearer nos artefatos inspecionados. O runner recebe somente chaves não sensíveis; logs brutos não são publicados, e falha preserva `ps`, serviços, imagens e saída filtrada antes do cleanup. |
| Compose e smoke | `scripts/validate-m1-compose.sh` acumulado M1+M2+M3+M4 sobre o conteúdo corretivo final | Origem única, fallback de rota, `404` genérico de assets com precedência do proxy, health, Swagger, cadastro/login/perfil/senha, autorização, persistência, `413/415/503`, logs, inventário exato, sanitizador e cleanup aprovados em recursos isolados. |
| Acabamento/UI | Suíte DOM e inspeção real em desktop e viewport de 360 px | Login, cadastro, dashboard e perfil usam shell/Material consistentes, landmarks/headings/labels, `aria-live`, foco após erro, skip link por teclado, loading visível e ações responsivas sem overflow horizontal, inclusive nome no limite defensivo. |
| CI | `.github/workflows/ci.yml` e execução local dos mesmos profiles/scripts | O job usa Actions por SHA, checkout sem credencial persistida, executa todos os gates e registra os nomes efêmeros. O cleanup final aceita somente o prefixo único da execução, tenta novamente cada projeto, sanitiza sua saída e ocorre antes do upload; falha de teardown reprova sucesso sem substituir falha primária. A execução hospedada depende de push explicitamente fora do escopo. |

Durante a implementação original, uma repetição redundante do smoke foi afetada por contenção externa comprovada. A revisão independente preservou o incidente como histórico, mas encerrou a lacuna executando com sucesso o smoke completo sobre o conteúdo corretivo final e verificando que contêineres, volumes e redes dos projetos efêmeros não permaneceram.

## Evidências da conclusão original de M6

| Gate | Execução observada em 2026-08-27 | Resultado |
|---|---|---|
| Baseline e isolamento | Leitura integral; `git status`; inventário por labels; `docker compose down --volumes --remove-orphans` | Worktree inicialmente limpa e zero containers do desafio; somente a rede residual e o volume deste projeto foram removidos; outro projeto Compose permaneceu intacto. |
| Build/boot | Docker Compose `v2.37.1-desktop.1`; `docker compose config --quiet`; `docker compose build --no-cache --progress plain`; `docker compose up --detach --wait --wait-timeout 300` | Configuração sem `.env`, web restrito a `127.0.0.1:8080`, imagens de produção construídas e origem única saudável sobre banco vazio/migration. |
| Runtime e persistência | HTTP real em `/`, `/health`, Swagger e APIs; navegador; `docker compose restart`; nova espera de health | SPA/health/Swagger `200`; guard e validações visuais aprovados; perfil persistiu e token da chave efêmera anterior foi rejeitado. |
| Backend | Profile `backend-tests` | 101/101 integrações, 0 falhas, 0 skips; runner em 6 s. |
| Frontend | Profile `frontend-tests` | npm/lint, 57/57 testes em 9 arquivos e build de 317,42 kB aprovados. |
| Contrato/CI | Profile `contract-tests`; `rhysd/actionlint:1.7.12` em container | 6 operações/53 referências e workflow aprovados. |
| Jornadas e smoke | `./scripts/e2e-playwright.sh`; `./scripts/validate-m1-compose.sh` | 3/3 E2E em 7,3 s; smoke completo de origem/auth/perfil/senha/persistência/erros/logs/cleanup aprovado. |
| Segurança e coerência | Revisores somente leitura de segurança, testes, Docker e SDD; scans de Git/dependências/segredos | 0 Alto e 0 Médio aberto após restringir o web a `127.0.0.1:8080` e corrigir a documentação de CSP; nenhum banco/segredo real versionado; locks válidos; riscos baixos e trade-offs registrados. |
| Documentação | README raiz, relatório 07, matriz, índice, `.env.example`, frontend README e este plano | `DOC-RUN-001` Verified; roteiro técnico criado; `AI-EXPLAIN-01`, CI hospedada e `DEL-REPO-01` mantidos Pending sem fabricar evidência. |

O relatório detalhado, incluindo as correções do próprio script de auditoria, está em [`07-validation-report.md`](07-validation-report.md).

## Evidências da revisão independente pós-M6

| Gate | Execução observada em 2026-08-27 | Resultado |
|---|---|---|
| Snapshot e revisão | `git status --short --branch`, `git log -5`, diff integral `3f6fbc4..ee2933d` e leitura acumulada de SDD, código, testes, Docker/CI e histórico | Etapa M6 e snapshot `ee2933d5d880f9ea0a401a39fffa7fec43e5c0a0` fixados com worktree inicial limpa; lentes de correção/segurança, stale, simplicidade e KISS concluídas antes da edição. |
| Regressão observável | Testes focados `src/app/core/auth/*.spec.ts` acrescentados antes do código | Primeira execução vermelha com 3 falhas/21 sucessos reproduziu timer/redirect/request; a re-revisão de URL produziu depois 2 falhas/8 sucessos para matrix params. |
| Correção frontend | `AuthService`, interceptor e sete testes de sessão/lifecycle/URL | Timer vinculado ao token e cancelado em troca/logout/destroy; rota protegida ativa conduz ao login, inclusive com matrix params/query/fragment; rota pública e sessão posterior são preservadas; request protegida sem token é cancelada. |
| Frontend final | Profile `frontend-tests` | npm/lint, 64/64 testes em 9 arquivos e build de 318,32 kB bruto/87,94 kB estimado aprovados. |
| Gates acumulados | Profiles `backend-tests`/`contract-tests`, `e2e-playwright.sh`, `validate-m1-compose.sh` e `actionlint:1.7.12` | 101/101 integrações, OpenAPI 6 operações/53 referências, 3/3 E2E, smoke completo e workflow aprovados; nenhum segredo real usado. |
| Re-revisão e documentação | Diff corretivo completo, `review-log.md`, plano, matriz, relatório, índice e uso de IA | 0 High, 0 Medium e 0 Low abertos; evidência original de M6 preservada como histórica e estado corrente atualizado sem promover CI hospedada, `AI-EXPLAIN-01` ou `DEL-REPO-01`. |

## Evidências da correção posterior à revisão completa

| Gate | Execução observada em 2026-08-27 | Resultado |
|---|---|---|
| Baseline do achado | Profile Docker oficial no snapshot `a73d33b`; repetição isolada na mesma imagem | Primeira execução: lint aprovado e 57/64 testes, com sete timeouts de 5/10 segundos; repetição: 64/64. A oscilação confirmou um falso negativo do gate sob contenção, não falha funcional determinística. |
| Configuração e refatoração | `vitest.config.ts` carregado por `@angular/build:unit-test`; busca de overrides; parser comum em `core/http/problem-details.ts` | Timeout global de 30 segundos aplicado sem retry ou override local menor; três cópias idênticas removidas e os três services preservaram a mesma leitura defensiva. |
| Frontend final | Profile `frontend-tests` reconstruído após a re-revisão; duas repetições simultâneas do mesmo profile | Três execuções verdes: lint, 67/67 testes em 10 arquivos e build de 318,10 kB bruto/87,84 kB estimado; as repetições sob contenção também passaram integralmente. |
| Gates acumulados aplicáveis | Profiles `backend-tests`/`contract-tests` e `docker compose config --quiet` | 101/101 integrações, OpenAPI com 6 operações/53 referências e configuração Compose aprovados. E2E/smoke não foram repetidos porque API, rotas, templates e contrato de negócio não mudaram. |

## Evidências da atividade pós-M6 de mutation testing

| Gate | Execução observada | Resultado |
|---|---|---|
| Suíte backend normal | `docker compose --profile backend-tests run --rm --build backend-tests` | Restore/build Release aprovados e 113/113 integrações passaram, sem falha ou skip. |
| Baseline Stryker | Profile `mutation-tests` com `break-at = 0` temporário, recalibrado após a correção DB | 473 mutantes criados e 198 executados; baseline limpa de 97,47% em 00:03:21, com 193 killed, 5 survived, 108 ignored, 3 mutações não compiláveis em `CompileError` e zero `NoCoverage`, timeout ou erro de execução. |
| Ratchet e relatórios | `stryker-config.json` final e `docker compose --profile mutation-tests run --rm --build mutation-tests` | `break/low/high = 97/97/97`; execução definitiva em 00:03:00 com 193 killed, 5 survived, 0 timeout/erro, HTML/JSON e gate de 3 `CompileError` aprovados; os cinco survivors equivalentes permanecem visíveis. |
| Infraestrutura local | `docker compose --profile mutation-tests config --quiet`, smoke Compose, actionlint e revisão estática | Profile isolado, inventários, gate contra falso-verde por timeout e workflow manual/semanal aprovados localmente; cleanup não usa `--volumes`. Execução hospedada permanece Pending até publicação e observação. |

## Evidências da robustez de persistência pós-revisão DB

| Gate | Execução observada em 2026-08-28 | Resultado |
|---|---|---|
| Regressões antes da correção | Três integrações focadas no profile `backend-tests` | Cadastro concorrente permaneceu verde; health com IDs divergentes retornou `Healthy` e duas trocas concorrentes retornaram `200`, produzindo exatamente 2 falhas/1 sucesso. |
| Backend final | `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release sem warnings e 113/113 integrações aprovadas; reexecução final em 6 s. O CAS produziu um único vencedor e o health rejeitou cardinalidade igual com IDs errados. |
| Contrato e operação | Profile `contract-tests`, `docker compose config` e `./scripts/validate-m1-compose.sh` | OpenAPI 6 operações/53 referências, timeout SQLite renderizado em 5 s e smoke completo aprovados; projeto/volume efêmeros foram removidos. |
| Mutação final | Baseline temporária e profile `mutation-tests` final | Score 97,47%, ratchet 97/97/97, 193 killed/5 survived/108 ignored/3 `CompileError`, sem timeout, `NoCoverage` ou erro de runtime; HTML/JSON e gate aprovados. |
| Aplicação principal | Rebuild/restart preservando volume; requests a `/`, `/health` e `/swagger/index.html` | Containers `api`/`web` saudáveis e três URLs em `200`; recursos de outros projetos não foram alterados. |

## Evidências da recuperação de startup e eficiência de queries

Esta tabela registra a baseline histórica daquela correção. O fechamento posterior, com lifecycle e `SIGTERM`, está na tabela seguinte.

| Gate | Execução observada em 2026-08-28 | Resultado |
|---|---|---|
| Regressões antes da correção | Três integrações focadas no target Docker | 3/3 falharam como esperado: duas aberturas no health, uma consulta de conflito para email canonicamente inalterado e startup retido pelo lock órfão. |
| Regressões e backend final | Testes focados seguidos de `docker compose --profile backend-tests run --rm --build backend-tests` | Recuperação do lock, deadline na preparação e em `MigrateAsync`, schema conflitante, abertura única, ausência do precheck redundante e corrida do próprio usuário passaram; build Release e 119/119 integrações aprovados. |
| Contrato e operação | Profile `contract-tests`, `docker compose --profile mutation-tests config --quiet` e `./scripts/validate-m1-compose.sh` | OpenAPI permaneceu com 6 operações/53 referências; configuração e smoke completo passaram. A primeira tentativa isolada encontrou a porta `8080` ocupada pela própria stack de demonstração; ela foi pausada sem remover o volume, o smoke passou e a stack foi restaurada. |
| Mutation testing final | `docker compose --profile mutation-tests run --rm --build mutation-tests` | 484 mutantes descobertos e 198 executados; score 97,47%, ratchet 97/97/97, 193 killed, 5 survived equivalentes, 119 ignored, 3 `CompileError` classificados e zero timeout/`NoCoverage`/erro de runtime em `00:08:23`. HTML/JSON e gate aprovados. |
| Aplicação principal | Rebuild/restart preservando volume; requests a `/`, `/health` e `/swagger/index.html` | Containers `api`/`web` saudáveis e três URLs em `200`; nenhum recurso de outro projeto foi alterado. |

## Evidências do fechamento da revisão completa de queries/startup

| Gate | Execução observada em 2026-08-28 | Resultado |
|---|---|---|
| Lifecycle e sinal real | Testes focados `BE-DB-002/003`, incluindo subprocesso bloqueado e `SIGTERM` real | 7/7 cenários focados passaram. O processo observou o token do host, não abriu listener, saiu em menos de 10 segundos e preservou usuário/histórico sem lock técnico residual. |
| Backend final | `docker compose --profile backend-tests run --rm backend-tests` | Build Release e 120/120 integrações aprovados, 0 falha e 0 skip, em 31 segundos. |
| Contrato e operação | Profile `contract-tests`, Compose config e `./scripts/validate-m1-compose.sh` | OpenAPI permaneceu com 6 operações/53 referências; configuração e smoke acumulado passaram sem alterar o volume principal. |
| Mutation testing final | `docker compose --profile mutation-tests run --rm --build mutation-tests` | 491 mutantes descobertos e 198 executados; score 97,47%, ratchet 97/97/97, 193 killed, 5 survived equivalentes, 106 ignored, 3 `CompileError` classificados e zero timeout/`NoCoverage`/erro de runtime em `00:04:30`; HTML/JSON, gate e exit code zero. |
| Aplicação principal | Rebuild/restart preservando volume; probes em `/`, `/health` e `/swagger/index.html` | Serviços ativos e três recursos em `200` no localhost:8080; nenhum recurso de outro projeto foi alterado. |

Execuções intermediárias de mutação foram rejeitadas, e não promovidas a baseline: uma continha 12 timeouts; outra revelou que a autoexclusão por `Id` precisava de uma regressão concorrente; e o primeiro passe após essa regressão ainda teve um timeout isolado. Uma tentativa de exclusão pontual também foi descartada por ocultar dois mutantes. A solução final preservou a condição necessária, matou seu mutante com comportamento observável e manteve somente a exclusão histórica já justificada em `JwtBearerConfiguration`.

## Evidências correntes do refinamento visual pós-M6

| Gate | Execução observada em 2026-08-28 | Resultado |
|---|---|---|
| Regressão do dashboard | Profile `frontend-tests` depois de atualizar especificação e teste, antes do template | Lint aprovado; 67/68 testes passaram e a única falha comprovou que os três textos redundantes ainda estavam no DOM. |
| Frontend final | `docker compose --profile frontend-tests run --rm --build frontend-tests` | Lint, 68/68 testes em 10 arquivos e build de 327,52 kB bruto/90,29 kB estimado aprovados. |
| Jornadas e publicação local | `./scripts/e2e-playwright.sh`; recriação somente do serviço `web`; navegador real em `http://localhost:8080/dashboard` | 3/3 E2E em 5,0 s; stack principal saudável e volume/API preservados; em 1280 px o DOM manteve um `h1`, um `h2`, perfil/logout, nenhum dos três textos e nenhum overflow. `E2E-001` repetiu o gate de overflow em 360 px. |

## Evidências da correção responsiva pós-revisão

| Gate | Execução observada em 2026-08-28 | Resultado |
|---|---|---|
| Regressões antes de cada correção | Três execuções sucessivas de `./scripts/e2e-playwright.sh` | Vermelhos determinísticos, sempre com E2E-002/003 verdes: formulário de cadastro com viewport ratio `0`; link secundário em `y=677` abaixo do botão posterior em `y=620`; e nome sem clamp/altura limitada. |
| Frontend final | `docker compose --profile frontend-tests run --rm --build frontend-tests` | Lint aprovado, 68/68 testes em 10 arquivos e build de 327,59 kB bruto/90,31 kB estimado. |
| Jornadas finais | `./scripts/e2e-playwright.sh` | 3/3 jornadas aprovadas em 5,0 s; `E2E-001` comprovou as quatro telas sem overflow em 320 px, formulário completo rolável em landscape curto, ordem visual/Tab, ambas as ações na primeira viewport e contenção visível do nome de 200 caracteres sem nova jornada. |
| Publicação, inspeção real e re-revisão dos oráculos | Recriação somente do serviço `web`; `/health`; navegador em `667×375`, `360×800` e `320×568`; E2E reforçado | API/volume preservados e health saudável. Login/cadastro completos ficaram alcançáveis em landscape; as quatro telas passaram sem overflow em 320 px; ações seguiram DOM/foco; nome integral de 200 caracteres ficou visível em 3/2 linhas, e `Ir para o perfil`/`Sair` terminaram dentro da primeira viewport móvel. |

Ao iniciar um milestone, alterar somente seu estado para `em andamento`. Ao concluir, registrar data, comandos, evidências, desvios e hash do commit antes de iniciar o próximo.

## Comandos

Comandos planejados para o scaffold de M1, executados somente na etapa de implementação:

```sh
dotnet new sln -n UserProfile --format sln
dotnet new webapi -n UserProfile.Api -o src/backend/UserProfile.Api -f net10.0 --use-controllers
dotnet new xunit -n UserProfile.Api.IntegrationTests -o tests/backend/UserProfile.Api.IntegrationTests -f net10.0
dotnet sln UserProfile.sln add src/backend/UserProfile.Api/UserProfile.Api.csproj tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj
dotnet add tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj reference src/backend/UserProfile.Api/UserProfile.Api.csproj
npx @angular/cli@22.1.3 new user-profile-web --directory src/frontend/user-profile-web --standalone --strict --routing --style=scss --skip-git --package-manager=npm
```

Depois de adicionar todos os `PackageReference` previstos para M1, executar uma única vez o bootstrap dos locks, revisar os arquivos gerados e versioná-los:

```sh
dotnet restore UserProfile.sln --use-lock-file
```

Comandos recorrentes posteriores:

```sh
dotnet restore UserProfile.sln --locked-mode
dotnet build UserProfile.sln --no-restore
dotnet test UserProfile.sln --no-restore --no-build --verbosity normal
npm ci --prefix src/frontend/user-profile-web
npm run lint --prefix src/frontend/user-profile-web
npm run build --prefix src/frontend/user-profile-web
npm test --prefix src/frontend/user-profile-web
ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml
scripts/validate-m1-compose.sh
docker compose --profile backend-tests run --build --rm backend-tests
docker compose --profile frontend-tests run --build --rm frontend-tests
docker compose --profile contract-tests run --rm contract-tests
docker compose --profile mutation-tests run --rm --build mutation-tests
./scripts/e2e-playwright.sh
```

`scripts/validate-m1-compose.sh` é o smoke funcional acumulado M1+M2+M3+M4 e, em M5, também valida tags completas e configuração operacional dos profiles. Os cinco comandos Compose/script finais reproduzem os gates de backend, frontend, contrato, mutação e E2E sem SDKs no host.

Os nomes de scripts npm devem ser confirmados no scaffold e então congelados. Mudança de comando exige atualização deste plano e do README antes do código dependente.

## Validação observável

- A URL única carrega a SPA e as chamadas `/api` permanecem na mesma origem.
- `/health` só fica saudável após migrations e acesso ao SQLite.
- Cadastro cria dados sem autenticar; login cria sessão curta; dashboard consulta a API.
- Rotas e endpoints protegidos rejeitam ausência, adulteração ou expiração do token.
- Toda resposta `401` inclui challenge Bearer obrigatório; login inválido usa corpo genérico equivalente para email inexistente/senha incorreta, enquanto indisponibilidade do upstream chega ao browser como `503 ProblemDetails`.
- Perfil do usuário A nunca consulta ou altera o usuário B.
- Atualizações persistem após recriar serviços mantendo o volume.
- Respostas e logs nunca expõem campos sensíveis.
- Cada resultado é ligado a teste e critério em `06-traceability.md`.

## Riscos

- **Drift OpenAPI/implementação** — validar contrato em CI e revisar status/schemas em cada milestone.
- **Concorrência SQLite/migrations** — manter uma instância; falhar startup em migration; documentar o limite.
- **Token em `sessionStorage`** — evitar HTML inseguro e dependências desnecessárias; expiração curta e limpeza em `401`.
- **Enumeração no cadastro** — o `409` de email duplicado é observável para cumprir o feedback explícito de erro; não expor outros dados e manter login genérico. Controles de abuso de produção permanecem fora da demonstração.
- **Token antigo após troca de senha** — aceitar validade até `exp`; não ampliar para revogação fora do escopo.
- **Chave sem `.env`** — gerar somente em `Development`; exigir configuração externa nos demais ambientes.
- **Tags envelhecidas** — reconfirmar as tags fixadas em M1; qualquer upgrade deve ser explícito e testado.
- **E2E instável** — limitar a três jornadas, esperar estados observáveis e não usar sleeps arbitrários.
- **Custo e ruído da mutação** — limitar à lógica crítica, executar manual/semanal e manter survivors/equivalências visíveis; não mudar regra de negócio por score.
- **Escopo crescente** — rejeitar patterns, endpoints e funcionalidades não ligados à matriz.

## Descobertas

- .NET 10/EF Core 10 são LTS e as versões `10.0.11` estão suportadas até novembro de 2028.
- Angular `22.1.3` é estável e aceita Node `^24.15.0`; Node `24.19.0` está em LTS.
- O “projeto de integração” foi esclarecido como o único projeto de testes HTTP do backend.
- Com SQLite, “persistência no Compose” é o arquivo em volume, não um serviço de banco.
- O requisito de Compose sem `.env` e a proibição de segredo versionado exigem chave efêmera apenas em `Development`.
- O logout após senha não revoga JWT no servidor; ampliar esse comportamento seria novo escopo.

## Decision log

- `2026-08-24` — Fixadas versões estáveis e tags completas após consulta às fontes oficiais; previews e aliases flutuantes foram excluídos.
- `2026-08-24` — Escolhido fluxo direto Controllers → EF Core/componentes concretos para preservar o monólito proporcional.
- `2026-08-24` — Fixada duração JWT em 15 minutos e tolerância de relógio em 30 segundos, sem refresh/revogação.
- `2026-08-24` — Definida normalização `Trim().ToUpperInvariant()` para cadastro, login e edição; email aparado preserva caixa para exibição.
- `2026-08-24` — Fixados `200` para login/updates, `201` para cadastro, `400` para validação, senha atual e credenciais de login inválidas, `401` com challenge Bearer para recursos protegidos, `409` para email e `503` para health/proxy.
- `2026-08-24` — Definida origem única em `http://localhost:8080`, com API interna e Nginx encaminhando `/api`, `/swagger` e `/health`.
- `2026-08-25` — M1 manteve `CreatedAtUtc`/`UpdatedAtUtc` como decisão interna agora formalizada no ADR-0002; a revisão independente restaurou o histórico que antes recomendara sua remoção e programou as provas de ciclo de vida em M2/M4.
- `2026-08-25` — A revisão independente descobriu que o comando backend original não executava testes; `IsTestProject`, contrato/runtime OpenAPI, schema real, timeout de health, ignores Docker, smoke Compose e rastreabilidade foram corrigidos antes do novo commit.
- `2026-08-25` — M1 manteve apenas o shell Angular e `/health`; a complexidade retida limita-se a migrations, SQLite real, ProblemDetails e proxy exigidos pelos critérios.
- `2026-08-25` — A revisão de M2 formalizou em `PREM-INPUT-01` que `200/320/128` e o limite de corpo de 1 MiB são refinamentos defensivos internos, não exigências do challenge; nome/email usam limites após `Trim`, email aceita somente a política ASCII documentada, senhas preservam espaços e nomes JSON camelCase são case-sensitive.
- `2026-08-25` — A revisão de M2 alinhou contrato e gates para `413/415 ProblemDetails`; o smoke versionado foi definido como acumulado M1+M2 e a prova de que logs não contêm marcadores de query/body/header pertence a `OPS-SECRET-001`, enquanto `BE-ERR-002` prova somente a segurança da resposta `500`.
- `2026-08-25` — M2 manteve o fluxo direto `AuthController` → EF Core/`PasswordHasher<User>`; o índice único é a autoridade e somente a violação SQLite `2067` vira `409`. A única instrumentação adicional é uma barreira em interceptor exclusivo dos testes para provar a corrida sem hook de produção.
- `2026-08-25` — O frontend de M2 criou somente cadastro, service/signals e um placeholder de login para receber o aviso; JWT, sessão, guard, interceptor e login funcional permanecem em M3.
- `2026-08-26` — A instrução explícita de M3 substituiu a decisão documental anterior de `400` para credenciais não reconhecidas por `401 ProblemDetails` genérico com challenge Bearer e passou a exigir `id` em `ProfileResponse`; payload estruturalmente inválido permanece `400` e nenhum request recebe `userId`.
- `2026-08-26` — M3 confirmou fluxo direto `Controllers` → EF Core/`JwtTokenIssuer` e `AuthService`/signals, sem refresh, NgRx ou camada extra. A complexidade retida restringe-se à validação JWT/configuração, hash fictício para reduzir sinal de timing, allowlist do interceptor e lazy routes exigidos pela fatia.
- `2026-08-26` — A revisão independente de M3 manteve o desenho direto: `ProfileService` passou de singleton para provider do dashboard, isolando ativações sem store ou generation tracker; o interceptor compara o Bearer original ao token corrente antes de limpar a sessão. Um filtro focado completa somente os metadados de resposta que o Swagger não inferia.
- `2026-08-26` — M4 manteve o fluxo KISS `Controller` → EF Core/`PasswordHasher<User>` e `component` → service/signals, sem repository, facade, NgRx ou camada adicional. A complexidade ficou restrita à atomicidade, à corrida do índice único, à autorização/limpeza segura de sessão e aos estados observáveis exigidos.
- `2026-08-26` — M5 não alterou endpoints nem regras de negócio: reutilizou as suítes acumuladas, acrescentou somente três jornadas Playwright diretas, targets/profiles Docker, um workflow e ajustes visuais locais. O isolamento usa projeto/volume por execução da suíte e contexto/dados por jornada, sem orquestrador ou framework adicional.
- `2026-08-26` — A revisão independente de M5 fechou `REV-M1-015`–`017`: npm passou a impor a allowlist, assets inexistentes com extensão retornam `404` e o collector sem consumidor foi removido. Actions foram fixadas por SHA e a decisão de status do teardown ganhou prova negativa direta, sem framework de CI adicional.
- `2026-08-27` — M6 não alterou código da aplicação nem contrato de negócio. Uma correção direta de configuração restringiu a publicação HTTP ao loopback e um assert no smoke tornou a decisão executável; documentação encerrou os demais achados. O usuário Nginx, `forbidOnly`, CSP e rate limiting permaneceram hardenings baixos documentados, sem abstrações novas.
- `2026-08-27` — A revisão pós-M6 corrigiu `AC-DASH-02` no serviço de autenticação e no interceptor sem store, refresh, listener global ou nova dependência: um único timer por sessão compara o token capturado, respeita a rota ativa e é cancelado no lifecycle; a API permanece autoridade.
- `2026-08-27` — A revisão completa posterior escolheu duas correções KISS: um único limite Vitest de 30 segundos no runner, sem retry, e um módulo compartilhado somente para o contrato/parser defensivo de `ProblemDetails`; timers, allowlists, services e mensagens específicas permanecem distintos.
- `2026-08-27` — Mutation testing foi definido como gate pós-M6 do backend, não como novo milestone funcional: Stryker `4.16.0`, allowlist crítica, execução Docker manual/semanal e ratchet derivado da primeira baseline real, sem gate de PR ou mutation testing frontend.
- `2026-08-28` — O refinamento visual modernizou somente o shell e as quatro telas Material. Paleta, tipografia do sistema, cartões e formas CSS foram inspirados na linguagem pública da Lekto sem copiar marca/ativos; labels, estados, rotas, payloads e regras permaneceram intactos. O `id` continua obrigatório no DTO de transporte, mas não é renderizado porque o desafio pede consulta visual somente de nome, email e senha.
- `2026-08-28` — A simplificação final do dashboard removeu somente o bloco de três cartões informativos e seus estilos exclusivos. Hero, dados retornados, navegação, logout, loading/erro e contratos permaneceram intactos; nenhuma abstração ou funcionalidade substituta foi criada.
- `2026-08-28` — A correção responsiva pós-revisão ocultou apenas o painel editorial em viewport simultaneamente estreita/baixa, alinhou o empilhamento móvel à ordem do DOM e limitou visualmente o nome no dashboard. O texto integral continua no DOM/API/perfil; não houve TypeScript, dependência, estado, rota ou contrato novo.

## Resultado final

M1–M6 estão concluídos quanto ao escopo técnico e documental. As revisões posteriores corrigiram sessão, estabilidade, apresentação, robustez SQLite, recuperação de startup e eficiência de queries sem ampliar funcionalidade. A evidência corrente contém 121 integrações backend, 68 testes frontend, três jornadas Playwright, contrato e smoke completo aprovados. Mutation testing foi recalibrado localmente para 97,50%, com ratchet 97/97/97 e relatórios HTML/JSON; concorrência de senha, lifecycle cooperativo sob `SIGTERM`, recuperação/deadline de ambas as fases de migration, health com uma conexão/IDs exatos, precheck condicional com exclusão correta da própria conta e margem de timeout do Compose estão comprovados. Os oráculos detectam listener prematuro, CTS não ligado e precheck síncrono redundante. A execução hospedada da CI, a confirmação de `AI-EXPLAIN-01` por uma pessoa e `DEL-REPO-01` permanecem ações externas e não foram marcadas como Verified.
