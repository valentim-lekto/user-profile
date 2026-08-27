# Plano de execução — User Profile

## Objetivo

Entregar a aplicação especificada em incrementos verticais verificáveis, executável por uma única origem com `docker compose up`, mantendo rastreabilidade entre requisitos, contrato, implementação e testes.

Somente um milestone pode estar em andamento. O próximo começa após o anterior cumprir seus gates, ter o diff revisado e registrar o resultado nesta página.

## Critérios de aceite relacionados

- `AC-REG-01`–`AC-REG-06` — cadastro e validações.
- `AC-LOGIN-01`–`AC-LOGIN-03` — autenticação e sessão.
- `AC-DASH-01`–`AC-DASH-04` — dashboard protegido.
- `AC-PROF-01`–`AC-PROF-05` — consulta e edição de perfil.
- `AC-PASS-01`–`AC-PASS-04` — alteração de senha e encerramento da sessão.
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
./scripts/e2e-playwright.sh
```

`scripts/validate-m1-compose.sh` é o smoke funcional acumulado M1+M2+M3+M4 e, em M5, também valida tags completas e configuração operacional dos profiles. Os quatro comandos Compose/script finais reproduzem os gates de backend, frontend, contrato e E2E sem SDKs no host.

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

## Resultado final

M1–M6 estão concluídos quanto ao escopo técnico e documental. A conclusão original de M6 repetiu o ambiente somente com Docker; a revisão independente posterior corrigiu a reproteção visual no `exp` sem alterar a API ou ampliar o escopo de negócio. A evidência corrente contém 101 integrações backend, 64 testes frontend, três jornadas Playwright, contrato, actionlint e smoke completo aprovados. A execução hospedada da CI, a confirmação de `AI-EXPLAIN-01` por uma pessoa e `DEL-REPO-01` permanecem ações externas e não foram marcadas como Verified.
