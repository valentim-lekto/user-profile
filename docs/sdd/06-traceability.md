# 06 — Matriz de rastreabilidade

**Status:** M3 concluído · **Data:** 2026-08-26

## Convenções

- **Design concluído:** requisito e critério estão ligados a uma decisão, contrato/tela, milestone e teste planejado; código e teste ainda não existem.
- **M1 concluído/parcial:** a parcela explicitamente atribuída ao walking skeleton possui implementação e evidência; comportamento de milestones futuros continua pendente.
- **M2 concluído/parcial:** a parcela de cadastro possui implementação e evidência pós-revisão; login, autenticação e perfil continuam pendentes.
- **M3 concluído/parcial:** login, JWT, sessão, guard, interceptor, dashboard e leitura protegida do perfil possuem implementação e evidência; edição de perfil/senha, E2E completo e CI continuam nos milestones futuros.
- **Parcial:** parte documental já existe, mas o critério depende de implementação ou entrega futura.
- **Pendente:** depende integralmente de milestone futuro.
- O estado deve ser atualizado no mesmo commit que altera implementação ou evidência.
- IDs de testes são definidos em [`04-test-strategy.md`](04-test-strategy.md); operações em [`03-api-contract.yaml`](03-api-contract.yaml).

## Requisitos funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `FR-REG-01` | `AC-REG-01`, `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | DTO `RegisterRequest`, `registerUser`, `/register` | M2 | `BE-REG-001/002`, `FE-REG-001` | M2 concluído: contrato, endpoint, formulário e suíte corrigida aprovados. |
| `FR-REG-02` | `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | Schemas OpenAPI e Reactive Form de cadastro; refinamentos de `PREM-INPUT-01` | M2 | `BE-REG-001/002`, `FE-REG-001`, `SPEC-OAS-004` | M2 concluído: limites internos `200/320/128`, política ASCII, validação pós-`Trim`, espaços significativos em senha e JSON case-sensitive aprovados nas duas camadas. |
| `FR-REG-03` | `AC-REG-05` | Normalização, política ASCII, índice `UX_Users_NormalizedEmail`, `registerUser` | M2 | `BE-REG-003/004` | M2 concluído: rejeição Unicode, colisões ASCII normalizadas e corrida pelo índice foram aprovadas. |
| `FR-REG-04` | `AC-REG-01`, `AC-REG-06` | `201 MessageResponse`, navegação `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002`, `E2E-001` | M2 concluído na fatia automatizada/smoke; a jornada E2E acumulada permanece reservada a M5. |
| `FR-LOGIN-01` | `AC-LOGIN-01`, `AC-LOGIN-02` | DTO `LoginRequest`, `loginUser`, `/login` | M3 | `BE-LOGIN-001/002/003`, `FE-LOGIN-001` | M3 concluído: login normalizado, validação e credenciais verificadas por integração/frontend/smoke. |
| `FR-LOGIN-02` | `AC-LOGIN-01`, `SEC-SESSION-01` | Emissor JWT, `LoginResponse`, `AuthService`, `/dashboard` | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | M3 concluído: token curto com claims mínimas, sessão em `sessionStorage` e navegação ao dashboard aprovados. |
| `FR-LOGIN-03` | `AC-LOGIN-02` | `401 ProblemDetails` genérico para credenciais não reconhecidas; `400` somente para payload inválido; estado de erro do login | M3 | `BE-LOGIN-002`, `FE-LOGIN-001`, `E2E-002` | M3 concluído nas suítes e smoke: email inexistente/senha errada retornam resposta byte-idêntica; E2E completo permanece em M5. |
| `FR-AUTH-01` | `AC-DASH-02` | JWT middleware, functional guard, rotas `/dashboard` e `/profile` | M3–M4 | `BE-AUTH-001`, `BE-PASS-004`, `FE-GUARD-001`, `FE-WIRE-001`, `E2E-003` | Parcial M3 concluída: autenticação Bearer, guard, wiring real e rotas protegidas aprovados; autorização dos PUTs de M4 permanece pendente. |
| `FR-AUTH-02` | `SEC-AUTH-01`, `AC-PROF-01` | Leitura de `sub`; `getCurrentProfile`, `updateCurrentProfile`, `changeCurrentPassword` | M3–M4 | `BE-AUTH-002`, `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | Parcial M3 concluída: `GET /api/profile` usa exclusivamente `sub`, ignora IDs do cliente e devolve DTO exato; atualizações são M4. |
| `FR-DASH-01` | `AC-DASH-01`, `AC-DASH-04` | `getCurrentProfile`, `ProfileService`, `/dashboard` | M3 | `BE-PROF-001`, `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M3 concluído: dashboard protegido busca a API e mostra o nome; loading/erro e isolamento entre sessões foram exercitados na suíte, enquanto a UI real comprovou o fluxo feliz. |
| `FR-DASH-02` | `AC-DASH-03` | Link `/dashboard` → `/profile` | M3 | `FE-DASH-001`, `E2E-001` | M3 concluído: navegação ao placeholder protegido foi validada; jornada E2E completa permanece em M5. |
| `FR-PROF-01` | `AC-PROF-01` | `ProfileResponse` com `id/name/email`, `getCurrentProfile`, `/profile` | M3–M4 | `BE-PROF-001/002`, `FE-PROF-001` | Parcial M3 concluída: leitura autenticada com DTO mínimo e destino protegido; edição permanece M4. |
| `FR-PROF-02` | `AC-PROF-02`, `AC-PROF-03`, `AC-PROF-04` | `UpdateProfileRequest`, `updateCurrentProfile`, formulário de dados | M4 | `BE-PROF-003/004/005/006`, `FE-PROF-001/002` | Design concluído; código/teste pendentes. |
| `FR-PROF-03` | `AC-PROF-05` | `ProfileResponse`, signals de loading/sucesso/erro | M4 | `BE-PROF-003`, `FE-PROF-002`, `E2E-001` | Design concluído; código/teste pendentes. |
| `FR-PASS-01` | `AC-PASS-01` | `ChangePasswordRequest`, `changeCurrentPassword`, formulário separado | M4 | `FE-PASS-001`, `SPEC-OAS-004` | Design concluído; código/teste pendentes. |
| `FR-PASS-02` | `AC-PASS-02`, `AC-PASS-03` | PasswordHasher e `400 ValidationProblemDetails` | M4 | `BE-PASS-001/002`, `FE-PASS-001/002` | Design concluído; código/teste pendentes. |
| `FR-PASS-03` | `AC-PASS-04` | `200 MessageResponse`, limpeza de `sessionStorage` | M4 | `BE-PASS-003`, `FE-PASS-002`, `E2E-002` | Design concluído; código/teste pendentes. |
| `FR-UI-01` | `UI-STATE-01`, `AC-LOGIN-03`, `AC-DASH-04` | Signals e estados em cadastro, login, dashboard e perfil/senha | M2–M4 | `FE-REG-001/002`, `FE-LOGIN-001`, `FE-DASH-001`, `FE-PROF-001/002`, `FE-PASS-002` | Parcial M3 concluída: cadastro/login/dashboard possuem feedback, loading, erro e prevenção de repetição; telas de edição/senha permanecem M4. |
| `FR-ERR-01` | `API-ERROR-01` | Schemas/responses ProblemDetails; Nginx converte `413` e `502/504` → `503` | M1–M5 | `BE-ERR-001/002`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `E2E-003`, `OPS-ORIGIN-001` | Parcial M3 concluída: login inválido e recursos protegidos retornam `401 ProblemDetails`/Bearer; `413/415`, `503` e ausência dos marcadores/JWT observado nos logs passaram no smoke. Operações M4/M5 permanecem pendentes. |

## Requisitos não funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `NFR-TECH-01` | `TECH-BACKEND-01` | .NET 10, Controllers, EF Core SQLite 10, JWT e lock NuGet; ADR-0001 | M1–M3 | `TECH-BACKEND-001`, build e suíte `BE-*` | M3 concluído: JWT/configuração aprovados; restore/build com warnings como erros e 69 integrações passaram. |
| `NFR-TECH-02` | `TECH-FRONTEND-01` | Angular standalone/strict, Material, Reactive Forms, signals e lock npm | M1–M4 | `TECH-FRONTEND-001`, build e suíte `FE-*` | Parcial M3 concluída: login/dashboard standalone, signals, lint, 0 vulnerabilidades, 45 testes e build sem warnings aprovados; edição permanece M4. |
| `NFR-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001`, `OPS-COMPOSE-001` | Parcial M3: smoke isolado recriou a API, renovou o token e confirmou persistência/autenticação; revalidação final de M6 permanece pendente. |
| `NFR-OPS-01` | `OPS-DOCKER-01` | `compose.yaml`, Nginx com `413/503 ProblemDetails`, API e volume; `/health` e `/swagger` | M1, M6 | `OPS-COMPOSE-001`, `OPS-ORIGIN-001` | Parcial M3 concluída: config, origem, login/perfil, persistência, `413/415`, `503`, logs e cleanup foram aprovados em projeto/volume isolados; validação final permanece M6. |
| `NFR-OPS-02` | `OPS-DOCKER-02` | Origem única, builds multi-stage e tags completas; ADR-0004 | M1, M5, M6 | `OPS-COMPOSE-001`, `OPS-TAGS-001`, `DOC-RUN-001` | Parcial M1 comprovada: multi-stage, tags exatas e somente `web:8080`; CI/validação final pendentes. |
| `NFR-CONFIG-01` | `SEC-SECRET-01`, `OPS-DOCKER-01` | Base e `.env.example` opcional em M1; chave validada em M3; auditoria em M5 | M1, M3, M5 | `BE-CONFIG-001`, `OPS-SECRET-001`, revisão de configuração | Parcial M3 concluída: chave Base64 externa é validada, configuração inválida falha fechada e fallback é somente Development; auditoria acumulada permanece M5. |
| `NFR-SEC-01` | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` | ADR-0003, DTOs, logging, allowlist do interceptor, wiring de produção e `sessionStorage` | M2–M5 | `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PASS-004`, `BE-DTO-001`, `FE-INT-001/002`, `FE-WIRE-001`, `OPS-SECRET-001` | Parcial M3 concluída: validação JWT, `sub`, token curto, allowlist/wiring real, limpeza de `401` vinculada à mesma sessão, DTO seguro e logs sem senha/marcadores/JWT observado passaram; auditoria final de hash/chave/CI permanece futura. |
| `NFR-TEST-01` | `TEST-FLOW-01` | Estratégia com integração principal, frontend, E2E e Compose | M1–M6 | Gates de `04-test-strategy.md` | Parcial M3 concluída: 69 integrações, 45 testes frontend, smoke isolado e UI real cobrem login/proteção/dashboard; E2E completo e CI permanecem M5. |
| `NFR-DOC-01` | `DOC-RUN-01` | README raiz futuro; índice SDD atual | M6 | `DOC-RUN-001` | Pendente de implementação/README. |
| `NFR-SDD-01` | `DOC-SDD-01`, `DOC-TRACE-01` | `00`–`06`, OpenAPI, plano, testes e ADRs | M1–M6 | `SPEC-TRACE-001`, revisão do índice | Parcial: documentação, contrato e evidências M1–M3 foram atualizados/revalidados; continuidade permanece obrigatória em M4–M6. |
| `NFR-SDD-02` | `DOC-SDD-01` | ADR-0001 a ADR-0004 | Design e contínuo | Revisão de decisões relevantes | Atendido nesta etapa; revisão contínua. |
| `NFR-TRACE-01` | `DOC-TRACE-01` | Este documento e extensões `x-*` do OpenAPI | M1–M6 | `SPEC-TRACE-001` | Parcial: evidências M1–M3 e extensões de contrato estão mapeadas/aprovadas; M4–M6 permanecem pendentes. |
| `NFR-AI-01` | `AI-SDD-01`, `AI-EXPLAIN-01` | `ai-usage.md`, registros resumidos e walkthrough humano | Todos | Revisão de diff/decisões e `DOC-EXPLAIN-001` | Parcial: uso de IA, auditorias e validação de M3 estão resumidos sem substituir `review-log.md`; walkthrough M6 permanece pendente. |
| `NFR-DELIVERY-01` | `DEL-REPO-01` | Publicação explícita pelo responsável | Entrega | Verificação manual da URL pública | Pendente da entrega; nenhum push nesta etapa. |

## Premissas aprovadas

| Requisito/premissa | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `PREM-ARCH-01` | `DOC-SDD-01` | Monólito modular; ADR-0001 | M1 | Inspeção da solution e build | M1 comprovado: uma API modular direta e build aprovado. |
| `PREM-ARCH-02` | `TECH-BACKEND-01` | Um `UserProfile.Api` e um `tests/backend/UserProfile.Api.IntegrationTests` | M1 | Inspeção da solution | M1 comprovado: exatamente um executável e um projeto de integração na solution. |
| `PREM-ARCH-03` | `DOC-TRACE-01` | Features `Auth`/`Profile`; sem patterns proibidos | M1–M6 | Revisão de diff por milestone | Parcial M3: `Auth`/`Profile` diretas, sem repository/facade/camada extra; PUTs de M4 permanecem pendentes. |
| `PREM-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | EF Core SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Parcial M3 aprovada: EF/SQLite/migration/volume, persistência após recriar a API e nova autenticação do usuário persistido foram revalidados; repetição final permanece em M6. |
| `PREM-DATA-02` | `DOC-TRACE-01` | Decisão interna do ADR-0002: entidade/migration `User`, timestamps e índice `UX_Users_NormalizedEmail` | M1–M4 | `BE-DB-001`, `BE-REG-001`, `BE-PROF-003`, `BE-PASS-003` | Parcial M2 aprovada: schema/índice e timestamps exatos sob relógio controlado passaram; preservação/avanço permanecem para M4. |
| `PREM-FE-01` | `UI-STATE-01` | Angular standalone/strict, Reactive Forms, Material | M1–M4 | Build e suíte `FE-*` | Parcial M3 aprovada: cadastro/login/dashboard tipados, Material, signals, lint/build e 45 testes passaram; formulários de edição/senha permanecem futuros. |
| `PREM-LANG-01` | `DOC-SDD-01` | Código/IDs em inglês; documentação em português | Todos | Revisão de diff | Design concluído; verificação contínua. |
| `PREM-EMAIL-01` | `AC-REG-05`, `AC-PROF-04` | Para emails ASCII aceitos: `Trim().ToUpperInvariant()` nos três fluxos | M2–M4 | `BE-REG-003/004`, `BE-LOGIN-003`, `BE-PROF-005` | Parcial M3: cadastro e login normalizado foram aprovados; edição reutilizará a regra em M4. |
| `PREM-INPUT-01` | `AC-REG-02`–`04`, `API-ERROR-01` | Refinamentos internos: nome/email `200/320` após `Trim`, email ASCII, senha/confirmação `128` sem aparar, JSON camelCase case-sensitive e corpo de 1 MiB com `413` | M2–M4 | `BE-REG-001/002/003`, `FE-REG-001`, `BE-LOGIN-002`, `BE-PROF-004`, `BE-PASS-001/002`, `SPEC-OAS-004/005`, `OPS-COMPOSE-001` | Decisão interna, não requisito original. M2 e a parcela de login M3 foram aprovadas em backend, frontend, contrato e smoke; usos de edição/senha permanecem M4. |
| `PREM-REG-01` | `AC-REG-01` | `201` sem token; `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002` | M2 concluído: `201` mínimo, ausência de sessão/token e navegação foram aprovados. |
| `PREM-PASS-01` | `AC-PASS-01`–`04` | Endpoint/form separado e limpeza da sessão | M4 | `BE-PASS-*`, `FE-PASS-*`, `E2E-002` | Design concluído; código/teste pendentes. |
| `PREM-PROF-01` | `AC-PROF-02` | PUT de perfil separado do PUT de senha | M4 | `SPEC-OAS-002`, `BE-PROF-*`, `BE-PASS-*` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-01` | `SEC-AUTH-01` | `sub` validado; nenhum `userId` em requests; `ProfileResponse` pode devolver o ID imutável | M3–M4 | `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | Parcial M3 concluída: `GET /api/profile` e dashboard são resolvidos pelo `sub`, inclusive contra IDs arbitrários do cliente; PUTs permanecem M4. |
| `PREM-AUTH-02` | `AC-DASH-01` | Dashboard usa `getCurrentProfile` com estado por ativação | M3 | `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M3 concluído: dashboard usa `GET /api/profile`, mostra o nome e isola respostas pendentes entre sessões; fluxo feliz também foi validado na UI real e o E2E completo permanece M5. |
| `PREM-AUTH-03` | `SEC-SESSION-01` | JWT de 15 minutos em `sessionStorage`, sem refresh | M3 | `BE-LOGIN-001`, `FE-LOGIN-002`, `FE-INT-002` | M3 concluído: duração, claims, armazenamento, logout e limpeza em `401` somente para a mesma sessão foram aprovados; não há refresh. |
| `PREM-ERR-01` | `API-ERROR-01` | ProblemDetails na API e nas falhas de transporte do proxy, inclusive `413/415` | M1–M5 | `BE-ERR-*`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `E2E-003`, `OPS-ORIGIN-001` | Parcial M3 aprovada por integração, contrato e proxy, inclusive `401` genérico/Bearer, `413/415` e `503`; futuras operações ampliarão a matriz. |
| `PREM-SEED-01` | `DOC-RUN-01` | Dados criados pelo cadastro e por factories de teste | M2, M5, M6 | `E2E-001`, `DOC-RUN-001` | Parcial M2: cadastro e factories criam dados sem seed; E2E/README permanecem pendentes. |
| `PREM-OPS-01` | `OPS-DOCKER-01` | Compose inicia sem criar ou copiar `.env` | M1–M3, M5–M6 | `OPS-COMPOSE-001`, `OPS-SECRET-001` | M1–M3 aprovados sem `.env`, inclusive JWT de Development, smoke/log isolado e cleanup; auditoria acumulada final permanece em M5–M6. |

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
