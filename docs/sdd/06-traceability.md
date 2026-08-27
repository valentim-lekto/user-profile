# 06 — Matriz de rastreabilidade

**Status:** M6 concluído tecnicamente; revisão independente pós-M6 concluída · **Data:** 2026-08-27

## Convenções

- **Design concluído:** requisito e critério estão ligados a uma decisão, contrato/tela, milestone e teste planejado; código e teste ainda não existem.
- **M1 concluído/parcial:** a parcela explicitamente atribuída ao walking skeleton possui implementação e evidência; comportamento de milestones futuros continua pendente.
- **M2 concluído/parcial:** a parcela de cadastro possui implementação e evidência pós-revisão; login, autenticação e perfil continuam pendentes.
- **M3 concluído/parcial:** login, JWT, sessão, guard, interceptor, dashboard e leitura protegida do perfil possuem implementação e evidência; suas jornadas completas foram confirmadas em M5.
- **M4 concluído/parcial:** edição protegida de perfil/senha, formulários, atomicidade, atualização do dashboard e encerramento de sessão possuem implementação e evidência; suas jornadas completas foram confirmadas em M5.
- **M5 concluído/parcial:** cobertura acumulada, acabamento, três jornadas Playwright, perfis Compose e definição de CI possuem implementação e evidência local; a execução hospedada continua dependente de publicação.
- **Verified (M6):** implementação ou artefato confrontado com evidência executada nesta auditoria final; esse estado não é usado para ação externa ou capacidade humana não observada.
- **Verified (revisão pós-M6):** comportamento ou evidência corrente corrigido e reexecutado depois da auditoria original de M6; registros M1–M6 anteriores permanecem históricos.
- **Parcial:** parte documental já existe, mas o critério depende de implementação ou entrega futura.
- **Pendente:** depende integralmente de milestone, ação humana ou evidência
  externa ainda não observada.
- O estado deve ser atualizado no mesmo commit que altera implementação ou evidência.
- IDs de testes são definidos em [`04-test-strategy.md`](04-test-strategy.md); operações em [`03-api-contract.yaml`](03-api-contract.yaml).

## Requisitos funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `FR-REG-01` | `AC-REG-01`, `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | DTO `RegisterRequest`, `registerUser`, `/register` | M2 | `BE-REG-001/002`, `FE-REG-001` | M2 concluído: contrato, endpoint, formulário e suíte corrigida aprovados. |
| `FR-REG-02` | `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | Schemas OpenAPI e Reactive Form de cadastro; refinamentos de `PREM-INPUT-01` | M2 | `BE-REG-001/002`, `FE-REG-001`, `SPEC-OAS-004` | M2 concluído: limites internos `200/320/128`, política ASCII, validação pós-`Trim`, espaços significativos em senha e JSON case-sensitive aprovados nas duas camadas. |
| `FR-REG-03` | `AC-REG-05` | Normalização, política ASCII, índice `UX_Users_NormalizedEmail`, `registerUser` | M2 | `BE-REG-003/004` | M2 concluído: rejeição Unicode, colisões ASCII normalizadas e corrida pelo índice foram aprovadas. |
| `FR-REG-04` | `AC-REG-01`, `AC-REG-06` | `201 MessageResponse`, navegação `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002`, `E2E-001` | M5 comprovado de ponta a ponta: cadastro retorna sucesso e conduz ao login antes do dashboard. |
| `FR-LOGIN-01` | `AC-LOGIN-01`, `AC-LOGIN-02` | DTO `LoginRequest`, `loginUser`, `/login` | M3 | `BE-LOGIN-001/002/003`, `FE-LOGIN-001` | M3 concluído: login normalizado, validação e credenciais verificadas por integração/frontend/smoke. |
| `FR-LOGIN-02` | `AC-LOGIN-01`, `SEC-SESSION-01` | Emissor JWT, `LoginResponse`, `AuthService`, `/dashboard` | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | M3 concluído: token curto com claims mínimas, sessão em `sessionStorage` e navegação ao dashboard aprovados. |
| `FR-LOGIN-03` | `AC-LOGIN-02` | `401 ProblemDetails` genérico para credenciais não reconhecidas; `400` somente para payload inválido; estado de erro do login | M3 | `BE-LOGIN-002`, `FE-LOGIN-001`, `E2E-002` | M5 comprovado: respostas de credenciais inexistentes/incorretas permanecem genéricas e o login inválido conserva a tela/mensagem esperadas. |
| `FR-AUTH-01` | `AC-DASH-02` | JWT middleware, functional guard, timer de `exp`, interceptor e rotas `/dashboard`/`/profile` | M3–M4, revisão pós-M6 | `BE-AUTH-001`, `BE-PASS-004`, `FE-GUARD-001`, `FE-INT-002`, `FE-WIRE-001`, `E2E-001/002/003` | Verified (revisão pós-M6): além da proteção inicial/logout/senha já comprovada, o mesmo JWT é removido em `exp`, uma rota protegida já ativa conduz ao login, sessão posterior/rota pública são preservadas e request protegida sem token válido é cancelada antes da rede. |
| `FR-AUTH-02` | `SEC-AUTH-01`, `AC-PROF-01` | Leitura de `sub`; `getCurrentProfile`, `updateCurrentProfile`, `changeCurrentPassword` | M3–M4 | `BE-AUTH-002`, `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | M4 concluído: GET e os dois PUTs identificam exclusivamente pelo `sub`; overposting, IDs em query/header e isolamento entre usuários foram rejeitados/comprovados. |
| `FR-DASH-01` | `AC-DASH-01`, `AC-DASH-04` | `getCurrentProfile`, `ProfileService`, `/dashboard` | M3 | `BE-PROF-001`, `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M3 concluído: dashboard protegido busca a API e mostra o nome; loading/erro e isolamento entre sessões foram exercitados na suíte, enquanto a UI real comprovou o fluxo feliz. |
| `FR-DASH-02` | `AC-DASH-03` | Link `/dashboard` → `/profile` | M3 | `FE-DASH-001`, `E2E-001` | M5 comprovado: a jornada real navega do dashboard ao perfil, salva e retorna ao dashboard. |
| `FR-PROF-01` | `AC-PROF-01` | `ProfileResponse` com `id/name/email`, `getCurrentProfile`, `/profile` | M3–M4 | `BE-PROF-001/002`, `FE-PROF-001` | M4 concluído: a tela carrega o perfil autenticado e o backend mantém o DTO mínimo, sem senha/hash. |
| `FR-PROF-02` | `AC-PROF-02`, `AC-PROF-03`, `AC-PROF-04` | `UpdateProfileRequest`, `updateCurrentProfile`, formulário de dados | M4 | `BE-PROF-003/004/005/006`, `FE-PROF-001/002` | M4 concluído: validações equivalentes, borda positiva de email 320, normalização, unicidade/race, isolamento, payload mínimo e operações inválidas sem mutação foram aprovados. |
| `FR-PROF-03` | `AC-PROF-05` | `ProfileResponse`, signals de loading/sucesso/erro | M4 | `BE-PROF-003`, `FE-PROF-002`, `E2E-001` | M5 comprovado: submissão/feedback/bloqueio passaram e a jornada real confirmou o novo nome no dashboard e o email em nova consulta do perfil. |
| `FR-PASS-01` | `AC-PASS-01` | `ChangePasswordRequest`, `changeCurrentPassword`, formulário separado | M4 | `FE-PASS-001`, `SPEC-OAS-004` | M4 concluído: formulário e endpoint separados, campos obrigatórios, wiring DOM e limite inclusivo 128 foram aprovados. |
| `FR-PASS-02` | `AC-PASS-02`, `AC-PASS-03` | PasswordHasher e `400 ValidationProblemDetails` | M4 | `BE-PASS-001/002`, `FE-PASS-001/002` | M4 concluído: senha atual inválida, confirmação divergente e novas senhas inválidas preservam integralmente usuário/hash; troca válida invalida a senha antiga e aceita a nova. |
| `FR-PASS-03` | `AC-PASS-04` | `200 MessageResponse`, limpeza de `sessionStorage` condicionada ao token iniciador | M4 | `BE-PASS-003`, `FE-PASS-002`, `E2E-003` | M5 comprovado: sucesso encerra a sessão; antes de qualquer novo login, a jornada reprova o dashboard, depois rejeita a senha antiga e aceita a nova; o teste focado preserva uma sessão posterior. |
| `FR-UI-01` | `UI-STATE-01`, `AC-LOGIN-03`, `AC-DASH-04` | Signals e estados em cadastro, login, dashboard e perfil/senha | M2–M5 | `FE-REG-001/002`, `FE-LOGIN-001`, `FE-DASH-001`, `FE-PROF-001/002`, `FE-PASS-002`, `E2E-001/002/003` | M5 concluído: 57 testes, UI real e locators E2E por label/role comprovam feedback, loading, foco, nomes acessíveis, teclado e layout responsivo nas quatro telas. |
| `FR-ERR-01` | `API-ERROR-01` | Schemas/responses ProblemDetails; Nginx converte `413` e `502/504` → `503` | M1–M5 | `BE-ERR-001/002`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `CI-001` | M5 concluído na fatia: integração/contrato/smoke comprovam `400/401/404/405/409/413/415/500/503`; o workflow repete os gates sem criar quarta jornada E2E. |

## Requisitos não funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `NFR-TECH-01` | `TECH-BACKEND-01` | .NET 10, Controllers, EF Core SQLite 10, JWT e lock NuGet; ADR-0001 | M1–M6 | `TECH-BACKEND-001`, build e suíte `BE-*` | Verified (M6): build sem cache e target Docker aprovaram restore/build; 101 integrações passaram sem falha/skip. |
| `NFR-TECH-02` | `TECH-FRONTEND-01` | Angular standalone/strict, Material, Reactive Forms, signals, npm/lock/allowlist | M1–M6 | `TECH-FRONTEND-001`, build e suíte `FE-*` | Verified (revisão pós-M6): npm/lint, 64 testes sem skip e build de produção passaram no target Docker fixado. |
| `NFR-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001`, `OPS-COMPOSE-001` | Verified (M6): volume foi resetado, migrations criaram banco vazio e nome/email persistiram após restart dos containers. |
| `NFR-OPS-01` | `OPS-DOCKER-01` | `compose.yaml`, Nginx com `413/503 ProblemDetails`, API e volume; `/health` e `/swagger` | M1, M6 | `OPS-COMPOSE-001`, `OPS-ORIGIN-001` | Verified (M6): config sem `.env`, build sem cache, healthchecks, origem/Swagger, restart, smoke e cleanup passaram. |
| `NFR-OPS-02` | `OPS-DOCKER-02` | Origem única, builds multi-stage, perfis de teste e inventário exato; ADR-0004 | M1, M5, M6 | `OPS-COMPOSE-001`, `OPS-TAGS-001`, `CI-001`, `DOC-RUN-001` | Verified (M6): somente `web` publica `127.0.0.1:8080`, tags completas, quatro profiles Docker-only, README reproduzível e workflow por SHAs foram revalidados. |
| `NFR-CONFIG-01` | `SEC-SECRET-01`, `OPS-DOCKER-01` | Base e `.env.example` opcional em M1; chave validada em M3; auditoria em M5 | M1, M3, M5–M6 | `BE-CONFIG-001`, `OPS-SECRET-001`, revisão de configuração | Verified (M6): Compose iniciou sem `.env`; chave real/banco não estão versionados; fallback efêmero e restrição de lifetime estão documentados. |
| `NFR-SEC-01` | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` | ADR-0003, DTOs, logging, allowlists, wiring de produção e `sessionStorage` | M2–M6 | `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PASS-004`, `BE-DTO-001`, `FE-GUARD-001`, `FE-INT-001/002`, `FE-PASS-002`, `FE-WIRE-001`, `OPS-SECRET-001`, `CI-001` | Verified (revisão pós-M6): hash, JWT, `sub`, DTOs, logs/artefatos, ausência de segredo, expiração visual e races de sessão passaram; 0 Alto/Médio aberto. |
| `NFR-TEST-01` | `TEST-FLOW-01` | Estratégia com integração principal, frontend, E2E, Compose e CI | M1–M6 | Gates de `04-test-strategy.md`, `E2E-*`, `CI-001` | Verified localmente (revisão pós-M6): 101 integrações, 64 frontend, 3 E2E, contrato, actionlint e smoke passaram; execução hospedada continua Pending até push. |
| `NFR-DOC-01` | `DOC-RUN-01` | README raiz e índice/relatório SDD | M6 | `DOC-RUN-001` | Verified (M6): README contém execução, URLs, fluxo, testes Docker-only, operação, variáveis, estrutura, decisões, limitações, IA e troubleshooting. |
| `NFR-SDD-01` | `DOC-SDD-01`, `DOC-TRACE-01` | `00`–`07`, OpenAPI, plano, testes e ADRs | M1–M6 | `SPEC-TRACE-001`, revisão do índice | Verified (revisão pós-M6): índice, plano, matriz, relatório/adendo, review log, OpenAPI, ADRs e testes foram confrontados; links locais e contrato passaram. |
| `NFR-SDD-02` | `DOC-SDD-01` | ADR-0001 a ADR-0004 | Design e contínuo | Revisão de decisões relevantes | Atendido nesta etapa; revisão contínua. |
| `NFR-TRACE-01` | `DOC-TRACE-01` | Este documento e extensões `x-*` do OpenAPI | M1–M6 | `SPEC-TRACE-001` | Verified (M6): 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios permanecem mapeados com estados coerentes. |
| `NFR-AI-01` | `AI-SDD-01`, `AI-EXPLAIN-01` | `ai-usage.md`, registros resumidos e walkthrough humano | Todos | Revisão de diff/decisões e `DOC-EXPLAIN-001` | Parcial: `AI-SDD-01` e o roteiro técnico estão Verified; `AI-EXPLAIN-01` permanece Pending human confirmation. |
| `NFR-DELIVERY-01` | `DEL-REPO-01` | Publicação explícita pelo responsável | Entrega | Verificação manual da URL pública | Pending: nenhum remote/push foi configurado por instrução; entrega está pronta para essa ação externa. |

## Premissas aprovadas

| Requisito/premissa | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `PREM-ARCH-01` | `DOC-SDD-01` | Monólito modular; ADR-0001 | M1 | Inspeção da solution e build | M1 comprovado: uma API modular direta e build aprovado. |
| `PREM-ARCH-02` | `TECH-BACKEND-01` | Um `UserProfile.Api` e um `tests/backend/UserProfile.Api.IntegrationTests` | M1 | Inspeção da solution | M1 comprovado: exatamente um executável e um projeto de integração na solution. |
| `PREM-ARCH-03` | `DOC-TRACE-01` | Features `Auth`/`Profile`; sem patterns proibidos | M1–M6 | Revisão de diff por milestone | Verified (revisão pós-M6): fluxo direto preservado; bind e reproteção no `exp` foram corrigidos sem layer, dependência, store ou refresh novos. |
| `PREM-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | EF Core SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Verified (M6): migration em banco vazio e persistência de perfil após restart foram observadas no volume padrão. |
| `PREM-DATA-02` | `DOC-TRACE-01` | Decisão interna do ADR-0002: entidade/migration `User`, timestamps e índice `UX_Users_NormalizedEmail` | M1–M4 | `BE-DB-001`, `BE-REG-001`, `BE-PROF-003`, `BE-PASS-003` | M4 concluído na fatia: schema/índice e relógio controlado comprovam preservação de `CreatedAtUtc` e avanço de `UpdatedAtUtc` somente em atualizações válidas. |
| `PREM-FE-01` | `UI-STATE-01` | Angular standalone/strict, Reactive Forms, Material | M1–M5 | Build, suíte `FE-*` e `E2E-*` | M5 concluído na fatia: quatro telas Material, formulários tipados, signals, locators por nomes acessíveis, responsividade, lint/build, 57 testes e três jornadas passaram. |
| `PREM-LANG-01` | `DOC-SDD-01` | Código/IDs em inglês; documentação em português | Todos | Revisão de diff | Design concluído; verificação contínua. |
| `PREM-EMAIL-01` | `AC-REG-05`, `AC-PROF-04` | Para emails ASCII aceitos: `Trim().ToUpperInvariant()` nos três fluxos | M2–M4 | `BE-REG-003/004`, `BE-LOGIN-003`, `BE-PROF-005` | M4 concluído: cadastro, login e edição reutilizam a mesma regra; colisões normalizadas amigáveis e concorrentes foram aprovadas. |
| `PREM-INPUT-01` | `AC-REG-02`–`04`, `API-ERROR-01` | Refinamentos internos: nome/email `200/320` após `Trim`, email ASCII, senha/confirmação `128` sem aparar, JSON camelCase case-sensitive e corpo de 1 MiB com `413` | M2–M4 | `BE-REG-001/002/003`, `FE-REG-001`, `BE-LOGIN-002`, `BE-PROF-004`, `BE-PASS-001/002`, `SPEC-OAS-004/005`, `OPS-COMPOSE-001` | Decisão interna, não requisito original. Os usos de cadastro, login, edição e senha foram aprovados em backend, frontend, contrato e smoke até M4. |
| `PREM-REG-01` | `AC-REG-01` | `201` sem token; `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002` | M2 concluído: `201` mínimo, ausência de sessão/token e navegação foram aprovados. |
| `PREM-PASS-01` | `AC-PASS-01`–`04` | Endpoint/form separado e limpeza da sessão | M4–M5 | `BE-PASS-*`, `FE-PASS-*`, `E2E-003` | M5 comprovado: operação/form separados, dashboard novamente protegido sem novo login e reautenticação real rejeitando senha antiga/aceitando a nova. |
| `PREM-PROF-01` | `AC-PROF-02` | PUT de perfil separado do PUT de senha | M4 | `SPEC-OAS-002`, `BE-PROF-*`, `BE-PASS-*` | M4 concluído: operações, DTOs, formulários e payloads permanecem estritamente separados. |
| `PREM-AUTH-01` | `SEC-AUTH-01` | `sub` validado; nenhum `userId` em requests; `ProfileResponse` pode devolver o ID imutável | M3–M4 | `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | M4 concluído: GET e ambos os PUTs são resolvidos pelo `sub`; IDs arbitrários/overposting não selecionam nem alteram outro usuário. |
| `PREM-AUTH-02` | `AC-DASH-01` | Dashboard usa `getCurrentProfile` com estado por ativação | M3–M5 | `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M5 comprovado: dashboard usa `GET /api/profile`, isola respostas e a jornada real mostra o nome cadastrado e depois o atualizado. |
| `PREM-AUTH-03` | `SEC-SESSION-01` | JWT de 15 minutos em `sessionStorage`, sem refresh | M3, revisão pós-M6 | `BE-LOGIN-001`, `FE-LOGIN-002`, `FE-GUARD-001`, `FE-INT-002` | Verified (revisão pós-M6): duração, claims, armazenamento, logout, `401` vinculado à mesma sessão, timer de `exp`, lifecycle e preservação de sessão posterior/rota pública passaram; não há refresh. |
| `PREM-ERR-01` | `API-ERROR-01` | ProblemDetails na API e nas falhas de transporte do proxy, inclusive `413/415` | M1–M5 | `BE-ERR-*`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `CI-001` | M5 concluído na fatia: integração, contrato e proxy comprovam a matriz de erros; workflow versionado repete esses gates. |
| `PREM-SEED-01` | `DOC-RUN-01` | Dados criados pelo cadastro e por factories de teste | M2, M5, M6 | `E2E-001`, `DOC-RUN-001` | Verified (M6): README não promete seed; fluxos manuais/E2E usaram cadastros sintéticos e emails únicos. |
| `PREM-OPS-01` | `OPS-DOCKER-01` | Compose inicia sem criar ou copiar `.env` | M1–M6 | `OPS-COMPOSE-001`, `OPS-SECRET-001` | Verified (M6): `.env` ausente; config, build e `up --wait` passaram com defaults seguros de Development. |

## Evidência executável de M1

| ID | Arquivo, método ou comando | Parcela comprovada |
|---|---|---|
| `BE-DB-001` | `tests/backend/UserProfile.Api.IntegrationTests/HealthTests.cs` — `StartupCreatesMigrationHistoryAndUniqueNormalizedEmailIndex` | Migration aplicada, modelo sem drift, sete colunas reais e índice único. |
| `BE-HEALTH-001` | `HealthTests.cs` — `HealthReturnsHealthyAfterStartupMigration` e `HealthReturnsProblemDetailsWhenDatabaseBecomesUnavailable`; `StartupTests.cs` — `StartupFailsWhenInitialMigrationCannotApply` | `200 Healthy`; lock real produz `503 ProblemDetails` em menos de cinco segundos com timeout do comando em um segundo; migration impossível falha no startup. |
| `BE-OAS-001` | `HealthTests.cs` — `SwaggerContainsOnlyTheImplementedOperationsAndRequiredSchemas` | Em M1 o documento runtime continha somente `/health`; o mesmo teste evolui incrementalmente com as operações implementadas. |
| `BE-ERR-001` (parcial M1) | `HealthTests.cs` — `UnknownApiRouteReturnsProblemDetails` | Rota `/api` inexistente retorna `404 application/problem+json`; matriz funcional completa permanece em M2–M5. |
| `TECH-BACKEND-001` | `dotnet restore UserProfile.sln --locked-mode`; `dotnet build UserProfile.sln --no-restore`; `dotnet test UserProfile.sln --no-restore --no-build --verbosity normal` com SDK `10.0.400` | Restore locked, build sem warnings/erros e seis integrações efetivamente descobertas e aprovadas. |
| `TECH-FRONTEND-001` | `src/frontend/user-profile-web/src/app/app.spec.ts`; `npm ci`; `npm run lint`; `npm test`; `npm run build` com Node `24.19.0` | Shell standalone criado/renderizado; lock, lint, dois testes e bundle de produção aprovados. |
| `SPEC-OAS-001`–`005` | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` e mutação negativa temporária | Seis operações, requests/responses, status, segurança, ProblemDetails, campos sensíveis e 42 referências locais; contrato inválido é rejeitado. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001` | `scripts/validate-m1-compose.sh` | Configuração base aprovada; imagens/tags, portas/usuários, SPA, health, Swagger, `404`, falha de upstream em `503` e preservação do volume aprovados no runtime isolado. O bloqueio da repetição com volume padrão foi resolvido pela evidência de M2 abaixo. |
| `OPS-SECRET-001` (parcial M1) | `rg --files-with-matches --hidden --glob '!.git/**'` para padrões AWS/GitHub/OpenAI/JWT/private-key e `JWT_SIGNING_KEY_BASE64` não vazio; inspeção dos `.dockerignore`; check de `.env.example` no smoke | Nenhum segredo real encontrado no repositório ou contexto; exemplo sem valor utilizável. A prova de logs do fluxo de cadastro foi antecipada para a revisão de M2 abaixo; a auditoria acumulada final permanece em M5. |

## Evidência executável de M2 após a revisão

A execução anterior à revisão registrou 29/29 integrações backend e 12/12 testes frontend. Após as correções, a suíte acumulada aprovou 36/36 integrações backend e 13/13 testes frontend, além do contrato e do smoke isolado.

| ID | Artefato ou cenário rastreável | Estado da evidência pós-correções |
|---|---|---|
| `BE-REG-001`, `BE-DTO-001` (parcial) | `RegisterTests.cs`: cadastro válido, bordas inclusivas `3/200`, email ASCII de 320, senha/confirmação `6/128` com espaços significativos, relógio controlado, hash e DTO mínimo | Todos os cenários aprovados na suíte de 36 integrações. |
| `BE-REG-002` | `RegisterTests.cs`: 18 cenários de ausências/bordas/email, mais JSON desconhecido e casing incorreto | Todos retornaram `400 ValidationProblemDetails` sem persistir usuário. |
| `BE-REG-003`, `BE-REG-004` | Duplicidade exata/normalizada, rejeição Unicode antes da normalização e corrida determinística pelo índice | Aprovados: exatamente um usuário nas colisões/race e emails não ASCII rejeitados. |
| `BE-ERR-001`, `BE-ERR-002` | JSON malformado, `404/405/415` e SQLite bloqueado com resposta `500` segura | Aprovados. Estes IDs comprovam respostas; o `413` anterior à API e a segurança de logs são provas do smoke. |
| `BE-OAS-001`, `SPEC-OAS-004`, `SPEC-OAS-005` | OpenAPI normativo e runtime: extensões pós-`Trim`, padrão ASCII, required/case-sensitive, limites internos, senhas `password`/`writeOnly` e respostas `413/415` | Aprovados para seis operações, 52 referências locais e o Swagger runtime da fatia. |
| `FE-REG-001`, `FE-REG-002` | `registration.service.spec.ts` e `register.spec.ts`: contrato relativo, feedback DOM, estados, bordas, email ASCII/Unicode, espaços em senha e `201/400/409/503` | 13/13 testes aprovados, com lint e build de produção verdes. |
| `TECH-BACKEND-001`, `TECH-FRONTEND-001` | Restore locked/build/test .NET na imagem fixada; `npm ci`, lint/test/build na imagem fixada | 36/36 integrações, 13/13 frontend, build .NET com 0 warnings/erros e npm com 0 vulnerabilidades. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001` (parcial) | `scripts/validate-m1-compose.sh`: smoke acumulado M1+M2, same-origin, `201/400/409/413/415`, recriação da API, volume e cleanup | Aprovado em projeto/volume efêmeros; persistência confirmada por `409` após recriar a API. Autenticação pós-recriação permanece para M3/M6. |
| `OPS-SECRET-001` (parcial) | O mesmo smoke envia marcadores sintéticos em query/body/header e inspeciona logs da API/Nginx, além de manter Compose sem segredo versionado | Aprovado: nenhum marcador apareceu nos logs; auditorias de hash/token/chave continuam acumuladas em M5. |

## Evidência executável de M3

| ID | Artefato ou cenário rastreável | Estado da evidência final de M3 |
|---|---|---|
| `BE-LOGIN-001`, `BE-LOGIN-002`, `BE-LOGIN-003` | `AuthenticationTests.cs`: login válido, normalização, payload inválido e credenciais inexistentes/incorretas | Aprovados na suíte de 69 integrações. O token usa `sub`, `jti`, `iat` e `exp` como claims mínimas, além de `iss`/`aud` exigidos para validação; as duas falhas de credenciais devolvem `401 ProblemDetails` byte-idêntico e o login reutiliza a normalização do cadastro. |
| `BE-AUTH-001`, `BE-AUTH-002` | `AuthenticationTests.cs` e `JwtTestTokenFactory.cs`: ausência de token, assinatura/issuer/audience/algoritmo/expiração inválidos, `sub` ausente/malformado e isolamento de usuários | Aprovados: recursos protegidos desafiam com Bearer; somente o `sub` identifica o perfil e IDs enviados pelo cliente não alteram a resposta. |
| `BE-PROF-001`, `BE-PROF-002`, `BE-DTO-001` | `AuthenticationTests.cs`: perfil válido, usuário inexistente e forma exata da resposta | Aprovados: `GET /api/profile` devolve exclusivamente `id`, `name` e `email`, sem hash ou outro campo sensível. |
| `BE-CONFIG-001`, `TECH-BACKEND-001` | Testes de `JwtOptions`/startup e imagem .NET `10.0.400` com restore locked, build e teste com warnings como erros | Chave válida, ausente e inválida foram exercitadas; fallback só em Development. Restore/build sem warnings e 69/69 integrações passaram, sem falhas/skips. |
| `FE-LOGIN-001/002`, `FE-GUARD-001`, `FE-INT-001/002`, `FE-DASH-001`, `FE-WIRE-001` | Specs Angular de login, autenticação/interceptor, guard, dashboard e configuração real | 45/45 testes aprovados: bordas inclusivas, formulário/erro/loading, navegação, allowlist, `401` vinculado à mesma sessão, guard/interceptor conectados pela configuração real, isolamento de perfil entre sessões, nome retornado, perfil protegido e logout. |
| `TECH-FRONTEND-001` | Imagem Node `24.19.0-bookworm-slim`: `npm ci`, lint, teste e build | 494 pacotes/0 vulnerabilidades; lint e build sem warnings; 45/45 testes; 317,37 kB bruto e 87,64 kB estimado. |
| `SPEC-OAS-001`–`005` | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` | Seis operações e 53 referências locais aprovadas; schemas, Bearer, requests/responses e `ProblemDetails` correspondem a M3. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-SECRET-001` | `docker compose config --quiet` e `scripts/validate-m1-compose.sh` | Smoke M1+M2+M3 aprovado em projeto/volume isolados: same-origin, registro/login, `401` equivalente/Bearer, perfil por `sub`, `413/415`, logs sem a senha bem-sucedida, os marcadores ou o primeiro JWT, recriação API com novo token/persistência, `503` e cleanup. |
| `FE-GUARD-001`, `FE-LOGIN-001`, `FE-DASH-001` | UI real em `http://localhost:8080` no Compose padrão | Guard anônimo, erro genérico de login, login/dashboard com nome, navegação ao placeholder protegido, logout e reproteção observados sem warnings/erros de console. O Compose padrão foi encerrado sem apagar o volume. |

## Evidência executável de M4

| ID | Artefato ou cenário rastreável | Estado da evidência final de M4 |
|---|---|---|
| `BE-PROF-003`, `BE-PROF-004`, `BE-PROF-005`, `BE-PROF-006` | `ProfileUpdateTests.cs`: atualização válida, limites/formatos, duplicidade amigável e concorrente, timestamps, overposting e isolamento por `sub` | Aprovados na suíte de 101 integrações, incluindo email válido exatamente em 320 caracteres: somente nome/email do próprio usuário mudam; operações inválidas ou conflitantes preservam integralmente o registro e `ProfileResponse` permanece mínimo. |
| `BE-PASS-001`, `BE-PASS-002`, `BE-PASS-003`, `BE-PASS-004` | `ProfileUpdateTests.cs`: senha atual errada ou válida em 128, nova senha/confirmação inválidas, troca válida, login antigo/novo, autorização e isolamento | Aprovados: falhas preservam hash/dados/timestamps; sucesso aceita a borda inclusiva, avança somente `UpdatedAtUtc`, invalida a senha antiga e permite login com a nova, sem aceitar `userId`. |
| `BE-DTO-001`, `BE-OAS-001`, `TECH-BACKEND-001` | DTOs/Swagger runtime e imagem .NET `10.0.400` com restore locked, build e teste da solution | Nenhuma resposta expõe `PasswordHash`/senha; seis operações runtime correspondem ao contrato, inclusive body obrigatório, media types e `$ref` de request/resposta dos PUTs. Restore/build terminaram com 0 warnings/erros e 101/101 integrações passaram sem skips. |
| `FE-PROF-001`, `FE-PROF-002` | `profile.spec.ts` e specs de dashboard/rotas: carregamento, validação, wiring DOM, payload, estados e nova consulta | Aprovados: o submit renderizado envia somente `name`/`email`, não envia campos de senha, bloqueia todo o formulário e duplo submit e mostra sucesso/erros; nova ativação do dashboard obtém o nome atualizado da API. |
| `FE-PASS-001`, `FE-PASS-002`, `TECH-FRONTEND-001` | `profile.spec.ts` e login/auth specs; imagem Node `24.19.0` com `npm ci`, lint, teste e build | Validadores, borda 128, confirmação cruzada, wiring DOM, bloqueio integral, loading/erro, troca válida e resposta tardia diante de sessão nova passaram; 0 vulnerabilidades, lint aprovado, 56/56 testes sem skips e build de 317,36 kB bruto/87,58 kB estimado. |
| `SPEC-OAS-001`–`005` | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` e asserts do Swagger runtime | Seis operações, 53 referências locais, schemas, Bearer, responses, campos obrigatórios/read-only/write-only e `ProblemDetails` aprovados. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-SECRET-001` | `docker compose config --quiet` e `scripts/validate-m1-compose.sh` | Smoke M1+M2+M3+M4 aprovado em projeto/volume efêmeros: ambos os PUTs, autorização, atomicidade, senha antiga/nova, persistência, logs seguros, `413/415/503` e cleanup integral. |
| `FE-PROF-001`, `FE-PROF-002`, `FE-PASS-001` | UI real em `http://localhost:8080` no Compose padrão | Login/dashboard/perfil, carregamento, atualização cadastral e novo nome no dashboard foram observados; confirmação divergente mostrou feedback acessível e o console ficou limpo. A submissão final da senha não foi feita no navegador e permanece comprovada pelas suítes/smoke. A stack foi encerrada sem `-v`, preservando o volume. |

## Evidência executável de M5

| ID | Artefato ou cenário rastreável | Estado da evidência final de M5 |
|---|---|---|
| `TECH-BACKEND-001`, `BE-DB-001`, `BE-REG-*`, `BE-LOGIN-*`, `BE-AUTH-*`, `BE-PROF-*`, `BE-PASS-*`, `BE-ERR-*`, `BE-DTO-001` | Target/profile `backend-tests` com restore locked, build Release e solution test | 101/101 integrações aprovadas sem falha/skip, usando HTTP/pipeline/serialização/EF Core/SQLite reais e bancos temporários isolados; não há mock da pilha, EF InMemory ou chamada direta a Controller. |
| `TECH-FRONTEND-001`, `FE-REG-*`, `FE-LOGIN-*`, `FE-GUARD-001`, `FE-INT-*`, `FE-DASH-001`, `FE-PROF-*`, `FE-PASS-*`, `FE-WIRE-001` | Target/profile `frontend-tests` com `npm ci`, lint, Vitest e build | 57/57 testes aprovados sem skip; formulários, validators, wiring, loading, bloqueio/duplo submit, mensagens, `401`, navegação, atualização de estado e sessão estão cobertos. |
| `E2E-001` | Playwright: cadastro → login → dashboard → perfil → nome/email atualizados → novas consultas → logout → dashboard reprotegido | Aprovado em viewport de 360 px: nome atualizado no dashboard, email atualizado no perfil, nomes acessíveis e ausência de overflow com nome no limite de 200 caracteres. |
| `E2E-002` | Playwright: dashboard anônimo → login; skip link por teclado; credenciais inválidas genéricas | Aprovado sem preparar estado pela API e sem retry/sleep. |
| `E2E-003` | Playwright: cadastro/login próprios → troca de senha → dashboard reprotegido sem novo login → senha antiga falha → nova autentica | Aprovado com dados/contexto independentes por jornada, projeto/volume isolados por execução da suíte e cleanup restrito. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-TAGS-001`, `OPS-SECRET-001` | Configuração com quatro perfis, `scripts/validate-m1-compose.sh` e inspeção de logs/artefatos | Aprovados: inventário exato, origem/fallback/asset `404`, health/Swagger, persistência, `413/415/503`, filtro testado e recursos efêmeros removidos. Trace forçado e relatórios finais tiveram 0 senha/JWT/Bearer. |
| `CI-001` | `.github/workflows/ci.yml`, `actionlint:1.7.12`, helpers shell e execução local dos perfis/scripts | Workflow válido: Actions por SHA, checkout sem credencial persistida, diagnóstico filtrado, nomes efêmeros registrados, cleanup antes do upload e precedência de falhas testada. A execução hospedada exige push explicitamente fora do escopo. |
| `FR-UI-01`, `PREM-FE-01` | Specs DOM, `E2E-*` e inspeção real desktop/360 px | Shell/telas Material consistentes, landmarks/headings/labels, `aria-live`, foco de erro, skip link, loading e ações responsivas aprovados sem layout excessivo. |

## Evidência executável da conclusão original de M6

| ID | Comando/artefato observado em 2026-08-27 | Estado |
|---|---|---|
| `TECH-BACKEND-001`, `BE-*` | Profile `backend-tests`, após build de produção sem cache | Verified: 101/101 integrações, 0 falhas, 0 skips, HTTP/EF Core/SQLite reais. |
| `TECH-FRONTEND-001`, `FE-*` | Profile `frontend-tests` | Verified: npm/lint, 57/57 testes em 9 arquivos e build de 317,42 kB. |
| `SPEC-OAS-001`–`005` | Profile `contract-tests`; Swagger runtime | Verified: 6 operações, 53 referências locais e quatro URLs públicas da origem única em `200`. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001` | config, build `--no-cache`, `up --wait`, inventário e smoke | Verified: sem `.env`, somente web em `127.0.0.1:8080`, tags completas, health/Swagger/SPA e falha upstream corretos. |
| `OPS-PERSIST-001` | Fluxo sintético, update, `docker compose restart`, nova espera de health e login | Verified: nome/email sobreviveram; chave efêmera invalidou apenas o token, não o usuário. |
| `OPS-SECRET-001`, `SEC-*` | Quatro revisões somente leitura, scans Git/dependências e smoke de logs/sanitizador | Verified: 0 Alto/Médio aberto e nenhum segredo, banco, hash ou token real versionado/exposto. |
| `E2E-001`–`003`, `CI-001` | `e2e-playwright.sh`, `actionlint:1.7.12` e smoke | Verified localmente: 3/3 jornadas em 7,3 s e workflow válido; execução hospedada Pending até publicação. |
| `DOC-RUN-001`, `DOC-SDD-01`, `DOC-TRACE-01` | README raiz, SDD 00–07, ADRs, links e relatório | Verified: comandos e estados refletem a execução; raiz/índice/plano/matriz/IA foram finalizados. |
| `DOC-EXPLAIN-001`, `AI-EXPLAIN-01` | Roteiro técnico no README | Roteiro Verified; capacidade humana Pending human confirmation. |
| `DEL-REPO-01` | Remote e URL pública | Pending: nenhum remote ou push por instrução explícita. |

## Evidência executável da revisão pós-M6

| ID | Comando/artefato observado em 2026-08-27 | Estado |
|---|---|---|
| `FR-AUTH-01`, `AC-DASH-02`, `FE-GUARD-001`, `FE-INT-002`, `SEC-SESSION-01` | Testes focados de `AuthService`, guard e interceptor | Regressões vermelhas com 3 falhas/21 sucessos e depois 2 falhas/8 sucessos; specs finais de autenticação aprovadas 27/27. Expiração ativa, parâmetros de URL, cancelamento sem Bearer, sessão posterior, rota pública e lifecycle são observáveis. |
| `TECH-FRONTEND-001`, `FE-*` | Profile `frontend-tests` após a correção final | Verified: npm/lint, 64/64 testes em 9 arquivos e build de 318,32 kB bruto/87,94 kB estimado. |
| `TECH-BACKEND-001`, `BE-*`, `SPEC-OAS-001`–`005` | Profiles `backend-tests` e `contract-tests` | Verified: 101/101 integrações e contrato com 6 operações/53 referências; backend/contrato não foram alterados pela correção. |
| `E2E-001`–`003`, `OPS-COMPOSE-001`, `CI-001` | `e2e-playwright.sh`, `validate-m1-compose.sh` e `actionlint:1.7.12` | Verified localmente: 3/3 jornadas, smoke acumulado/cleanup e workflow aprovados. |
| `DOC-SDD-01`, `DOC-TRACE-01` | Plano, matriz, relatório/adendo, índice, review log e uso de IA | Verified: snapshot, achados, disposições, comandos, resultados e riscos estão atuais; evidência original de M6 permanece explicitamente histórica. |

## Cobertura após a revisão de M2

- Todos os 19 requisitos funcionais estão ligados a operação/tela, milestone e teste planejado.
- Todos os 14 requisitos não funcionais possuem decisão/evidência planejada.
- Todas as 18 premissas identificadas, incluindo `PREM-INPUT-01`, possuem ponto de verificação.
- Todos os 40 critérios de aceite/qualidade de `01-requirements.md` aparecem nesta matriz.
- A suíte acumulada registra 36 integrações backend e 13 testes frontend efetivamente descobertos e aprovados, sem skips.
- Os quatro requisitos de cadastro e `AC-REG-01`–`06` possuem implementação e evidência pós-revisão.
- OpenAPI normativo/runtime, smoke acumulado, persistência, `413/415`, política de logs e cleanup foram aprovados; M3–M6 permanecem pendentes.

## Cobertura após M3

- Todos os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios continuam ligados a design, milestone e teste planejado.
- Login, emissão/validação JWT, armazenamento de sessão, interceptor, guard, dashboard e leitura de perfil por `sub` estão implementados e comprovados; os PUTs de perfil/senha continuam exclusivamente em M4.
- A suíte acumulada registra 69 integrações backend e 45 testes frontend aprovados, sem falhas ou skips; contrato normativo/runtime, configuração Compose, smoke isolado e UI real também passaram.
- O smoke pós-revisão comprovou que a sessão renovada após recriar a API consulta o perfil persistido e que os logs anteriores à recriação não contêm a senha usada no fluxo válido, os marcadores sintéticos ou o primeiro JWT. A ausência de hash nas respostas é coberta por `BE-DTO-001`; a auditoria acumulada de hash/chave nos logs permanece em M5.
- M4–M6 continuam pendentes, inclusive edição de perfil/senha, jornadas E2E completas, CI, README raiz e validação/documentação finais.

## Cobertura após M4

- Todos os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios continuam ligados a design, milestone e teste planejado.
- Edição cadastral e alteração de senha usam operações/formulários separados, identificação exclusiva por `sub`, validação equivalente, unicidade atômica e limpeza de sessão após sucesso.
- A suíte acumulada registra 101 integrações backend e 56 testes frontend aprovados, sem falhas ou skips; OpenAPI normativo/runtime, configuração Compose, smoke isolado e UI real também passaram.
- O smoke comprovou atualização/persistência, falhas sem mutação, senha antiga rejeitada/nova aceita, logs sem credenciais/marcadores e `413/415/503`; seus recursos efêmeros foram removidos, enquanto a stack padrão foi encerrada sem `-v` e preservou o volume.
- A UI real confirmou a atualização do nome após nova consulta e feedback acessível para confirmação divergente. A troca final de senha não foi submetida no navegador, mas possui evidência backend, frontend e Compose.
- M5–M6 continuam pendentes, inclusive jornadas E2E completas, CI, README raiz, acabamento e validação/documentação finais.

## Cobertura após M5

- Todos os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios continuam ligados a design, milestone e teste planejado; os estados correntes acima incorporam M5 sem alterar o contrato de negócio.
- A suíte acumulada aprovou 101 integrações backend, 57 testes frontend e exatamente três jornadas E2E independentes, sem falhas, skips, retries ou seed compartilhado.
- Contrato OpenAPI, configuração/tags/profiles Compose, smoke acumulado, imagens de produção, workflow CI e cleanup isolado foram validados; a indisponibilidade do upstream permanece no smoke, sem quarta jornada E2E.
- A auditoria de segurança encontrou e encerrou o vazamento inicial de senha sintética em metadados de trace: a versão final gera os segredos no navegador, passa apenas chaves não sensíveis e teve três traces forçados/report/JUnit verificados com 0 senha ou JWT.
- M6 permanecia pendente para README raiz, reprodução limpa, revalidação integral e publicação explícita; a seção seguinte registra seu fechamento técnico.

## Cobertura na conclusão original de M6

- Todos os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios permanecem ligados a design, milestone, teste e estado; nenhum item foi promovido sem evidência.
- Build sem cache, Compose sem `.env`, banco vazio/migration, URLs, restart/persistência, 101 integrações, 57 testes frontend, três E2E, contrato, actionlint e smoke foram observados somente com Docker.
- A auditoria independente terminou com 0 achado Alto ou Médio aberto. Os riscos baixos aceitos e as limitações de produção constam em [`07-validation-report.md`](07-validation-report.md).
- `DOC-RUN-001`, `DOC-SDD-01` e `DOC-TRACE-01` estão Verified; o roteiro `DOC-EXPLAIN-001` existe e foi revisado.
- `AI-EXPLAIN-01`, a execução hospedada da CI e `DEL-REPO-01` permanecem Pending por dependerem, respectivamente, de confirmação humana e publicação; nenhum push foi realizado.

## Cobertura corrente após a revisão pós-M6

- Todos os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios permanecem ligados a design, etapa, teste e estado.
- `AC-DASH-02` agora inclui e comprova expiração enquanto dashboard/perfil já está ativo, cancelamento local de request protegida sem token e preservação contra timers/respostas de sessões anteriores.
- A suíte corrente aprovou 101 integrações backend, 64 testes frontend e 3 jornadas E2E; contrato, actionlint e smoke acumulado também passaram somente com Docker.
- A revisão e a re-revisão terminaram com 0 High, 0 Medium e 0 Low abertos; [`review-log.md`](review-log.md) preserva evidência, correções e decisões.
- `AI-EXPLAIN-01`, CI hospedada e `DEL-REPO-01` continuam Pending; nenhum push ou confirmação humana foi presumido.
