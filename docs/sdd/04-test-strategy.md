# 04 — Estratégia de testes

**Status:** aprovada para implementação · **Data:** 2026-08-24 · **Contrato:** [`03-api-contract.yaml`](03-api-contract.yaml)

## Objetivo

Validar os comportamentos e riscos principais com a menor suíte capaz de fornecer evidência confiável. A integração HTTP real do backend é a base; testes de frontend cobrem comportamento do cliente; poucas jornadas E2E e a validação do Compose comprovam a entrega completa.

## Princípios

- Cada milestone entrega seus próprios testes; M5 não acumula testes que deveriam acompanhar M2–M4.
- O backend é exercitado por HTTP com Controllers, middleware, autenticação, EF Core e SQLite reais.
- EF Core InMemory, mocks de repositório e chamadas diretas a Controllers não substituem integração.
- O mesmo cenário não é repetido em todas as camadas sem um risco específico que justifique.
- Casos positivos, negativos, autorização e formato de erro têm a mesma importância.
- Testes geram usuários, emails, senhas sintéticas e chaves JWT em runtime; não há seed nem credencial fixa versionada.
- Testes não são removidos, ignorados ou afrouxados para obter sucesso artificial.

## Níveis e responsabilidades

| Nível | Ferramenta/abordagem planejada | Responsabilidade principal |
|---|---|---|
| Especificação | Parser/linter OpenAPI e checagens de rastreabilidade | Sintaxe, operações, segurança, schemas, IDs e ausência de escopo extra. |
| Integração backend | `UserProfile.Api.IntegrationTests`, `WebApplicationFactory` e `HttpClient` | Pipeline HTTP real em processo, SQLite real, migrations, autenticação, persistência e ProblemDetails. |
| Frontend focado | Runner padrão do Angular, TestBed, Reactive Forms e `HttpTestingController` | Validações, signals, estados de UI, services, guard, interceptor e navegação. |
| E2E | Playwright, fixado na implementação | Poucas jornadas completas pelo Nginx e API reais. |
| Operação | Docker Compose e verificações HTTP em checkout limpo | Build, origem única, health, volume, configuração e instruções do README. |

`WebApplicationFactory` exercita o pipeline ASP.NET completo sem abrir socket externo. As jornadas E2E e a validação Docker cobrem o tráfego TCP real e o proxy Nginx.

## Ambiente dos testes de integração

- Cada fixture usa um arquivo SQLite exclusivo em diretório temporário.
- A suíte aplica migrations ao banco do teste; pelo menos um teste parte de arquivo inexistente.
- A factory substitui apenas configuração de conexão, chave JWT e relógio; não substitui Controllers, `DbContext`, hasher nem autenticação.
- `TimeProvider` controlável permite testar `iat`/`exp` sem esperas reais.
- A chave JWT do teste é gerada em memória para cada execução.
- Usuários e emails recebem sufixos únicos; a senha sintética é gerada em runtime e reutilizada somente dentro do teste que a criou.
- Cenários de indisponibilidade partem de startup saudável e mantêm um bloqueio exclusivo real sobre o arquivo SQLite, com timeout curto; não usam endpoint, Controller ou health check exclusivo de teste.
- Arquivos temporários são removidos ao fim da fixture, inclusive em falha.
- Testes que compartilham banco são serializados; fixtures independentes podem executar em paralelo.

## Catálogo planejado — backend

| ID | Cenário | Evidência principal |
|---|---|---|
| `BE-REG-001` | Cadastro válido, incluindo limites inclusivos de nome `3/200`, email ASCII de 320 caracteres e senha/confirmação `6/128` (com espaços significativos), retorna `201`, persiste hash verificável diferente do texto puro, inicializa timestamps no relógio controlado e não retorna JWT, senha ou hash. | `AC-REG-01`, `PREM-DATA-02`, `PREM-INPUT-01`, `SEC-SECRET-01` |
| `BE-REG-002` | Cada campo obrigatório ausente, nome curto/longo, email não ASCII ou inválido por casos de borda da política comum (sem ponto no domínio ou com espaço interno)/longo, senha curta/longa, confirmação ausente/curta/longa/divergente ou propriedade JSON desconhecida/com caixa incorreta retorna `400 ValidationProblemDetails`, e nenhum usuário é persistido. | `AC-REG-02`–`04`, `PREM-INPUT-01`, `API-ERROR-01` |
| `BE-REG-003` | Emails ASCII com espaços externos ou caixa diferente colidem em `409`; emails Unicode que escapariam do `ToUpperInvariant` são rejeitados antes da normalização. | `AC-REG-05`, `PREM-EMAIL-01`, `PREM-INPUT-01` |
| `BE-REG-004` | O índice único existe e uma violação concorrente é mapeada para `409`, sem segundo usuário. | `AC-REG-05` |
| `BE-LOGIN-001` | Credenciais válidas retornam Bearer curto com `sub`, `jti`, `iat` e `exp`, sem refresh token. | `AC-LOGIN-01`, `SEC-SESSION-01` |
| `BE-LOGIN-002` | Payload ausente/inválido ou acima dos limites defensivos retorna `400 ValidationProblemDetails`; para payload válido, email inexistente e senha incorreta retornam o mesmo `401 ProblemDetails` genérico com challenge Bearer, sem indicar a credencial divergente nem criar sessão. | `AC-LOGIN-02`, `PREM-INPUT-01`, `API-ERROR-01` |
| `BE-LOGIN-003` | Login usa a mesma normalização de email adotada no cadastro. | `PREM-EMAIL-01` |
| `BE-AUTH-001` | Ausência, assinatura adulterada, issuer/audience inválidos e expiração retornam `401 ProblemDetails` com challenge Bearer. | `AC-DASH-02`, `SEC-AUTH-01`, `API-ERROR-01` |
| `BE-AUTH-002` | Claims mínimas ausentes/malformadas retornam `401`; `sub` válido sem usuário retorna `404`; o GET do perfil existente é resolvido somente por `sub`. | `SEC-AUTH-01` |
| `BE-CONFIG-001` | Chave externa Base64 de ao menos 32 bytes permite startup; ausência fora de `Development`, Base64 inválido ou valor curto falha; ausência em `Development` usa fallback aleatório; nenhum cenário registra a chave. | `SEC-AUTH-01`, `SEC-SECRET-01` |
| `BE-PROF-001` | GET retorna somente ID imutável, nome e email do usuário indicado pelo `sub`. | `AC-DASH-01`, `AC-PROF-01` |
| `BE-PROF-002` | Dois usuários consultam apenas o próprio perfil; query/header arbitrários não influenciam o `sub` usado pelo GET. | `SEC-AUTH-01`, `AC-PROF-01` |
| `BE-PROF-003` | PUT válido atualiza e persiste nome/email do usuário atual, preserva `CreatedAtUtc` e avança `UpdatedAtUtc`. | `AC-PROF-02`, `AC-PROF-05`, `PREM-DATA-02` |
| `BE-PROF-004` | PUT aplica validações equivalentes ao cadastro; cada falha preserva nome, email, email normalizado e timestamps. | `AC-PROF-03` |
| `BE-PROF-005` | Email de outro usuário retorna `409` sem alteração parcial; manter o próprio email não conflita. | `AC-PROF-04` |
| `BE-PROF-006` | Dois usuários alteram somente o próprio perfil; `userId` extra no JSON retorna `400`, e query/header arbitrários não influenciam o `sub` usado pelo PUT. | `SEC-AUTH-01`, `AC-PROF-01`, `AC-PROF-02` |
| `BE-PASS-001` | Senha atual incorreta, ausente ou acima de 128 caracteres retorna `400` e preserva nome, email, email normalizado, hash e timestamps. | `AC-PASS-02`, `PREM-INPUT-01` |
| `BE-PASS-002` | Nova senha ausente/curta/longa ou confirmação ausente/curta/longa/divergente retorna `400`; toda a entidade é preservada, a senha antiga continua autenticando e a nova não autentica. | `AC-PASS-03`, `PREM-INPUT-01` |
| `BE-PASS-003` | Alteração válida retorna `200`, preserva `CreatedAtUtc`, avança `UpdatedAtUtc`; senha antiga falha e nova senha autentica. | `AC-PASS-04`, `PREM-DATA-02` |
| `BE-PASS-004` | O endpoint de senha rejeita Bearer ausente/inválido; com dois usuários altera somente a senha indicada pelo `sub`, e rejeita `userId` extra no JSON. | `AC-DASH-02`, `SEC-AUTH-01` |
| `BE-DTO-001` | Nenhuma resposta expõe senha, hash ou email normalizado; somente `ProfileResponse` expõe o ID imutável previsto no contrato. | `AC-PROF-01`, `SEC-SECRET-01` |
| `BE-ERR-001` | Erros previstos e gerados pelo pipeline, incluindo JSON malformado, media type não suportado `415`, rota `/api` inexistente e método não permitido, usam `ProblemDetails`/`ValidationProblemDetails` e `application/problem+json`; o `413` anterior à API é coberto por `OPS-COMPOSE-001`. | `API-ERROR-01` |
| `BE-ERR-002` | Após startup saudável, cadastro contra SQLite bloqueado percorre o handler real e retorna `500` sem stack trace, SQL ou segredo na resposta. | `SEC-SECRET-01`, `API-ERROR-01` |
| `BE-DB-001` | Startup aplica migrations a banco vazio; o schema real contém exatamente os sete campos definidos no ADR-0002 com tipo/nulabilidade/chave esperados, cria o índice único e não deixa mudança pendente entre modelo e snapshot. | `OPS-DOCKER-01`, `PREM-DATA-02`, ADR-0002 |
| `BE-HEALTH-001` | `/health` retorna `200` após startup; com timeout padrão de 30 segundos na conexão da API, bloqueio exclusivo posterior retorna `503 application/problem+json` em menos de 5 segundos. Falha de migration é testada como falha de startup. | `OPS-DOCKER-01`, `API-ERROR-01` |
| `BE-OAS-001` | O OpenAPI gerado em runtime contém somente as operações já implementadas; em M3, exatamente `/health`, `/api/auth/register`, `/api/auth/login` e `GET /api/profile`, com segurança, status, schemas, extensões pós-`Trim`, JWT Bearer e DTOs coerentes com o contrato normativo. | `DOC-SDD-01`, `DOC-TRACE-01`, `SPEC-OAS-002`, `SPEC-OAS-003`, `SPEC-OAS-004` |

## Catálogo planejado — frontend

| ID | Cenário | Evidência principal |
|---|---|---|
| `FE-REG-001` | Formulário tipado exige quatro campos, valida e exibe os erros locais, não envia payload inválido, prova limites inclusivos/máximos após o mesmo tratamento do backend, aceita espaços significativos em senha, aceita email ASCII válido exatamente em 320 caracteres, rejeita Unicode e as mesmas bordas de formato e exige igualdade das senhas. | `AC-REG-02`–`04`, `PREM-INPUT-01` |
| `FE-REG-002` | Loading bloqueia nova submissão; `201` leva ao login com sucesso; erro permanece visível sem criar sessão. | `AC-REG-01`, `AC-REG-06`, `UI-STATE-01` |
| `FE-LOGIN-001` | Formulário e service aplicam os limites defensivos inclusivos de email/senha, incluindo email ASCII válido com 320 caracteres e senha com 128, tratam loading, bloqueiam submissão duplicada, exibem o `401` genérico de credenciais e tratam validação `400`/erro inesperado. | `AC-LOGIN-02`, `AC-LOGIN-03`, `PREM-INPUT-01` |
| `FE-LOGIN-002` | Sucesso grava token apenas em `sessionStorage` e navega ao dashboard. | `AC-LOGIN-01`, `SEC-SESSION-01` |
| `FE-GUARD-001` | Guard permite sessão não expirada e bloqueia token ausente, malformado ou expirado. | `AC-DASH-02` |
| `FE-INT-001` | Interceptor anexa Bearer somente às URLs relativas protegidas de perfil; não o anexa a login, cadastro, health, URL absoluta nem destino externo. | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01` |
| `FE-INT-002` | `401` de request que levava Bearer limpa sessão e conduz ao login somente se a sessão corrente ainda usa o mesmo token; um `401` tardio não remove uma sessão mais recente. O `401` do login público não levava Bearer, não dispara limpeza global e continua disponível para a tela. | `AC-LOGIN-02`, `AC-DASH-02`, `SEC-SESSION-01` |
| `FE-DASH-001` | Dashboard busca perfil, mostra loading/erro, saúda pelo nome retornado, navega ao perfil e faz logout removendo somente o token da aplicação antes de voltar ao login. Uma nova ativação usa estado de perfil novo, de modo que a resposta pendente da sessão anterior não bloqueia nem preenche a seguinte. | `AC-DASH-01`, `AC-DASH-03`, `AC-DASH-04`, `SEC-SESSION-01` |
| `FE-WIRE-001` | A configuração real da aplicação conecta as rotas protegidas ao guard e o `HttpClient` ao interceptor: acesso anônimo redireciona e uma sessão válida produz Bearer no GET do dashboard. | `AC-DASH-01`, `AC-DASH-02`, `SEC-AUTH-01` |
| `FE-PROF-001` | Perfil carrega nome/email com estados de loading/erro e valida edição com as regras do cadastro. | `AC-PROF-01`, `AC-PROF-03`, `UI-STATE-01` |
| `FE-PROF-002` | Atualização envia exatamente nome/email — nunca senha, mesmo vazia —, mostra loading, bloqueia submissão duplicada e apresenta feedback de sucesso/erro, incluindo `409`; nova consulta do dashboard mostra o nome persistido. | `AC-PROF-02`, `AC-PROF-04`, `AC-PROF-05`, `UI-STATE-01` |
| `FE-PASS-001` | Formulário separado exige senha atual, nova senha e confirmação, e aplica os limites defensivos `128` a todas as entradas. | `AC-PASS-01`, `AC-PASS-03`, `PREM-INPUT-01` |
| `FE-PASS-002` | Loading bloqueia submissão duplicada; sucesso remove o JWT e navega ao login com feedback; senha atual incorreta mantém a sessão e mostra erro. | `AC-PASS-02`, `AC-PASS-04`, `UI-STATE-01` |

Os testes de frontend não reimplementam criptografia, EF ou validação JWT. Services recebem respostas HTTP controladas; guard e interceptor são testados como funções no contexto de injeção Angular.

## Jornadas E2E mínimas

| ID | Jornada | Abrangência |
|---|---|---|
| `E2E-001` | Cadastrar → ver sucesso no login → autenticar → ver saudação → editar nome/email → recarregar e confirmar persistência. | Caminho feliz principal. |
| `E2E-002` | Login inválido → login válido → alterar senha → confirmar sessão encerrada → senha antiga falha → nova senha autentica. | Erros, senha e sessão. |
| `E2E-003` | Abrir rota protegida sem token; depois de autenticar, parar realmente o serviço `api`, recarregar o dashboard e verificar que o proxy respondeu `503 application/problem+json` e a tela exibiu indisponibilidade. | Guard, proxy, `API-ERROR-01` e estado de indisponibilidade. |

As jornadas usam a origem publicada pelo Nginx e não chamam a API diretamente para preparar estado. Dados são criados pelo cadastro, sem seed. `E2E-003` controla o serviço pelo Compose e observa a falha real do proxy; não intercepta nem simula a chamada no browser.

## Validação de especificação e contrato

| ID | Checagem |
|---|---|
| `SPEC-OAS-001` | YAML parseável e documento reconhecido como OpenAPI 3.0.3. |
| `SPEC-OAS-002` | Seis `operationId` únicos, cinco operações de negócio e `/health`; nenhuma rota fora de escopo. |
| `SPEC-OAS-003` | Operações de perfil têm Bearer, não definem `userId` e seus request bodies rejeitam propriedades extras; públicas declaram `security: []`. |
| `SPEC-OAS-004` | Campos obrigatórios, nomes case-sensitive, validações pós-`Trim`, limites, formatos e status coincidem com requisitos/design. |
| `SPEC-OAS-005` | Erros referenciam ProblemDetails; operações JSON declaram `413/415`; payload de login inválido usa `400`, credenciais não reconhecidas usam `401` genérico e todo `401` declara `WWW-Authenticate` como obrigatório; indisponibilidade declara `503`; respostas não contêm campos sensíveis além do ID imutável permitido em `ProfileResponse`. |
| `SPEC-TRACE-001` | Cada requisito/critério aplicável possui linha em `06-traceability.md` e teste planejado. |

`scripts/validate-openapi.rb` executa `SPEC-OAS-001`–`005` sobre o contrato
versionado, incluindo métodos extras, schemas, status, segurança, `userId`,
ProblemDetails e campos sensíveis. O teste `BE-OAS-001` cobre a parcela já
implementada do documento exposto em runtime. Quando as operações funcionais
existirem, CI deve comparar a documentação exposta com o contrato versionado ou
validar ambos pelo mesmo conjunto de testes de contrato. Divergência quebra o
build.

## Validação Docker e entrega

| ID | Cenário | Evidência principal |
|---|---|---|
| `TECH-BACKEND-001` | Solution/projetos e lock NuGet usam ASP.NET Core/C#, EF Core SQLite e JWT nas versões fixadas; restore locked e build passam. | `TECH-BACKEND-01` |
| `TECH-FRONTEND-001` | `package.json`/lockfile usam Angular standalone/strict, Reactive Forms e Material nas versões fixadas; `npm ci` e build passam. | `TECH-FRONTEND-01` |
| `OPS-COMPOSE-001` | `scripts/validate-m1-compose.sh` é o smoke acumulado M1+M2+M3+M4: em projeto/volume nomeado efêmero executa `docker compose up --build --wait` sem `.env` nem SDKs, valida origem única, cadastro, login, os dois PUTs, falhas sem mutação, `401` equivalente com Bearer, `413/415`, ausência de credenciais/marcadores/JWT nos logs, recria a API preservando perfil/senha, renova a sessão e remove somente os recursos temporários ao final. | `OPS-DOCKER-01`, `OPS-DOCKER-02`, `OPS-DOCKER-03`, `API-ERROR-01` |
| `OPS-ORIGIN-001` | O mesmo smoke verifica SPA, `/api/*`, `/swagger/*` e `/health` por `http://localhost:8080`, ausência de porta pública da API, `404 ProblemDetails`, upstream parado convertido em `503 ProblemDetails` e mapeamento explícito de `502`/`504`. | `API-ERROR-01`, ADR-0004 |
| `OPS-PERSIST-001` | Criar usuário, recriar serviços sem remover volume e autenticar novamente. | `OPS-DOCKER-03` |
| `OPS-TAGS-001` | Dockerfiles não contêm `latest` nem tags incompletas e usam as versões do design. | `OPS-DOCKER-02` |
| `OPS-SECRET-001` | Compose inicia sem segredo versionado; `.env.example` é opcional e não contém valor utilizável; o smoke envia marcadores sintéticos em query/body/header, usa uma senha sintética no fluxo válido e comprova que logs da API/Nginx não contêm esses valores nem o JWT observado; a auditoria acumulada final também cobre hash e chave. | `SEC-SECRET-01`, `SEC-LOG-01` |
| `DOC-RUN-001` | Uma pessoa segue o README em ambiente limpo e reproduz comandos, URLs e cadastro de dados. | `DOC-RUN-01` |
| `DOC-EXPLAIN-001` | Walkthrough manual cobre ADRs, fluxo `sub`, senha/JWT, proxy/SQLite, estados do frontend e um caminho rastreado de requisito até teste, com resultado resumido. | `AI-EXPLAIN-01` |

## Gates por milestone

| Milestone | Gates mínimos |
|---|---|
| M1 | Build backend/frontend; `dotnet test` deve descobrir e aprovar exatamente os testes M1, não apenas retornar exit code zero; `TECH-FRONTEND-001`, parte aplicável de `TECH-BACKEND-001`, `SPEC-OAS-*`, `BE-DB-001`, `BE-HEALTH-001`, `BE-OAS-001`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001`, `.env.example` sem segredo, ProblemDetails runtime, Swagger e smoke Compose. |
| M2 | `BE-REG-*`, `FE-REG-*`, `BE-ERR-001/002`, `BE-OAS-001`, parcela M2 de `OPS-COMPOSE-001`/`OPS-SECRET-001`, assertion aplicável de `BE-DTO-001` e regressão dos gates M1. |
| M3 | `BE-LOGIN-*`, `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PROF-001/002`, `TECH-BACKEND-001`, parte de `.env.example`/logs de `OPS-SECRET-001`, `FE-LOGIN-*`, `FE-GUARD-*`, `FE-INT-*`, `FE-DASH-*`, `FE-WIRE-*` e assertions aplicáveis de `BE-ERR-001`/`BE-DTO-001`. |
| M4 | `BE-PROF-003/004/005/006`, `BE-PASS-*`, `FE-PROF-*`, `FE-PASS-*`, parcela M4 de `OPS-COMPOSE-001`/`OPS-SECRET-001` e assertions aplicáveis de `BE-ERR-001`/`BE-DTO-001`. |
| M5 | `E2E-*`, suíte acumulada completa, CI, `OPS-TAGS-001`, auditorias de log/segredo e build de produção. |
| M6 | Reexecução de `TECH-*`, `OPS-*`, `DOC-RUN-001`, `DOC-EXPLAIN-001`, `SPEC-TRACE-001`, revisão manual e execução completa em checkout limpo. |

## Política de cobertura

Não será fixado percentual arbitrário. A cobertura é orientada à matriz e aos riscos: todos os critérios funcionais, autorização, unicidade, formato de erro e operação Docker precisam de evidência automatizada ou validação manual explicitamente registrada. Código trivial de configuração não recebe teste unitário isolado quando já é exercitado por integração.

## Critério de conclusão da suíte

- Todos os testes planejados para o milestone atual estão implementados e passam.
- Não existem skips, exclusões ou retries ocultando falhas determinísticas.
- Bancos, containers e processos temporários são limpos após a execução.
- Falhas produzem evidência suficiente sem imprimir valores sensíveis.
- A matriz de rastreabilidade reflete os IDs implementados e seu estado real.
- O gate de M4 também reexecuta `OPS-COMPOSE-001` de forma acumulada: os dois PUTs, preservação após falhas, senha antiga/nova, persistência após recriar a API, logs seguros e cleanup isolado devem passar; a interface real cobre o fluxo cadastral e a inspeção dos dois formulários sem antecipar as jornadas Playwright de M5.
