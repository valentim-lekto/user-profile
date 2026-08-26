# 06 — Matriz de rastreabilidade

**Status:** atualizado após M2 · **Data:** 2026-08-25

## Convenções

- **Design concluído:** requisito e critério estão ligados a uma decisão, contrato/tela, milestone e teste planejado; código e teste ainda não existem.
- **M1 concluído/parcial:** a parcela explicitamente atribuída ao walking skeleton possui implementação e evidência; comportamento de milestones futuros continua pendente.
- **M2 concluído/parcial:** a parcela de cadastro possui implementação e evidência; login, autenticação e perfil continuam pendentes.
- **Parcial:** parte documental já existe, mas o critério depende de implementação ou entrega futura.
- **Pendente:** depende integralmente de milestone futuro.
- O estado deve ser atualizado no mesmo commit que altera implementação ou evidência.
- IDs de testes são definidos em [`04-test-strategy.md`](04-test-strategy.md); operações em [`03-api-contract.yaml`](03-api-contract.yaml).

## Requisitos funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `FR-REG-01` | `AC-REG-01`, `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | DTO `RegisterRequest`, `registerUser`, `/register` | M2 | `BE-REG-001/002`, `FE-REG-001` | M2 concluído: contrato, endpoint, formulário e testes implementados. |
| `FR-REG-02` | `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | Schemas OpenAPI e Reactive Form de cadastro | M2 | `BE-REG-002`, `FE-REG-001` | M2 concluído: required/padrão comum/trim e limites `200/320/128` comprovados nas duas camadas. |
| `FR-REG-03` | `AC-REG-05` | Normalização, índice `UX_Users_NormalizedEmail`, `registerUser` | M2 | `BE-REG-003/004` | M2 concluído: precheck, índice autoritativo e corrida determinística retornam `409` sem segundo usuário. |
| `FR-REG-04` | `AC-REG-01`, `AC-REG-06` | `201 MessageResponse`, navegação `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002`, `E2E-001` | M2 concluído na fatia automatizada/smoke; a jornada E2E acumulada permanece em M5. |
| `FR-LOGIN-01` | `AC-LOGIN-01`, `AC-LOGIN-02` | DTO `LoginRequest`, `loginUser`, `/login` | M3 | `BE-LOGIN-001/002`, `FE-LOGIN-001` | Design concluído; código/teste pendentes. |
| `FR-LOGIN-02` | `AC-LOGIN-01`, `SEC-SESSION-01` | Emissor JWT, `LoginResponse`, `AuthService`, `/dashboard` | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | Design concluído; código/teste pendentes. |
| `FR-LOGIN-03` | `AC-LOGIN-02` | `400 ValidationProblemDetails` genérico, estado de erro do login | M3 | `BE-LOGIN-002`, `FE-LOGIN-001`, `E2E-002` | Design concluído; código/teste pendentes. |
| `FR-AUTH-01` | `AC-DASH-02` | JWT middleware, functional guard, rotas `/dashboard` e `/profile` | M3–M4 | `BE-AUTH-001`, `BE-PASS-004`, `FE-GUARD-001`, `E2E-003` | Design concluído; código/teste pendentes. |
| `FR-AUTH-02` | `SEC-AUTH-01`, `AC-PROF-01` | Leitura de `sub`; `getCurrentProfile`, `updateCurrentProfile`, `changeCurrentPassword` | M3–M4 | `BE-AUTH-002`, `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | Design concluído; código/teste pendentes. |
| `FR-DASH-01` | `AC-DASH-01`, `AC-DASH-04` | `getCurrentProfile`, `ProfileService`, `/dashboard` | M3 | `BE-PROF-001`, `FE-DASH-001`, `E2E-001` | Design concluído; código/teste pendentes. |
| `FR-DASH-02` | `AC-DASH-03` | Link `/dashboard` → `/profile` | M3 | `FE-DASH-001`, `E2E-001` | Design concluído; código/teste pendentes. |
| `FR-PROF-01` | `AC-PROF-01` | `ProfileResponse`, `getCurrentProfile`, `/profile` | M3–M4 | `BE-PROF-001/002`, `FE-PROF-001` | Design concluído; código/teste pendentes. |
| `FR-PROF-02` | `AC-PROF-02`, `AC-PROF-03`, `AC-PROF-04` | `UpdateProfileRequest`, `updateCurrentProfile`, formulário de dados | M4 | `BE-PROF-003/004/005/006`, `FE-PROF-001/002` | Design concluído; código/teste pendentes. |
| `FR-PROF-03` | `AC-PROF-05` | `ProfileResponse`, signals de loading/sucesso/erro | M4 | `BE-PROF-003`, `FE-PROF-002`, `E2E-001` | Design concluído; código/teste pendentes. |
| `FR-PASS-01` | `AC-PASS-01` | `ChangePasswordRequest`, `changeCurrentPassword`, formulário separado | M4 | `FE-PASS-001`, `SPEC-OAS-004` | Design concluído; código/teste pendentes. |
| `FR-PASS-02` | `AC-PASS-02`, `AC-PASS-03` | PasswordHasher e `400 ValidationProblemDetails` | M4 | `BE-PASS-001/002`, `FE-PASS-001/002` | Design concluído; código/teste pendentes. |
| `FR-PASS-03` | `AC-PASS-04` | `200 MessageResponse`, limpeza de `sessionStorage` | M4 | `BE-PASS-003`, `FE-PASS-002`, `E2E-002` | Design concluído; código/teste pendentes. |
| `FR-UI-01` | `UI-STATE-01`, `AC-LOGIN-03`, `AC-DASH-04` | Signals e estados em cadastro, login, dashboard e perfil/senha | M2–M4 | `FE-REG-002`, `FE-LOGIN-001`, `FE-DASH-001`, `FE-PROF-001/002`, `FE-PASS-002` | Parcial M2: cadastro possui loading/sucesso/erro e bloqueio de duplo submit; demais telas pertencem a M3/M4. |
| `FR-ERR-01` | `API-ERROR-01` | Schemas/responses ProblemDetails; middleware e conversão `502/504` → `503` no Nginx | M1–M5 | `BE-ERR-001/002`, `BE-HEALTH-001`, `SPEC-OAS-005`, `E2E-003`, `OPS-ORIGIN-001` | Parcial M2: além de health/404/proxy, cadastro cobre `400/409/415/405/500` em ProblemDetails e feedback `400/409/503`; futuras operações permanecem pendentes. |

## Requisitos não funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `NFR-TECH-01` | `TECH-BACKEND-01` | .NET 10, Controllers, EF Core SQLite 10, JWT e lock NuGet; ADR-0001 | M1–M3 | `TECH-BACKEND-001`, build e suíte `BE-*` | Parcial M2 comprovada: SDK/runtime/EF/Controllers/locks, cadastro e build aprovados; JWT permanece em M3. |
| `NFR-TECH-02` | `TECH-FRONTEND-01` | Angular standalone/strict, Material, Reactive Forms, signals e lock npm | M1–M4 | `TECH-FRONTEND-001`, build e suíte `FE-*` | Parcial M2 comprovada: cadastro standalone/strict com Material, Reactive Forms, signals, lint, 12 testes e build; telas futuras permanecem pendentes. |
| `NFR-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Parcial M2 comprovada: schema/índice e usuário persistem após recriar a API com o volume padrão; autenticação pós-recriação e revalidação final pertencem a M3/M6. |
| `NFR-OPS-01` | `OPS-DOCKER-01` | `compose.yaml`, Nginx com `503 ProblemDetails`, API e volume; `/health` e `/swagger` | M1, M6 | `OPS-COMPOSE-001`, `OPS-ORIGIN-001` | Parcial M2: configuração base foi revalidada com volume Docker padrão, mesma origem, health/Swagger/cadastro e persistência; validação final permanece em M6. |
| `NFR-OPS-02` | `OPS-DOCKER-02` | Origem única, builds multi-stage e tags completas; ADR-0004 | M1, M5, M6 | `OPS-COMPOSE-001`, `OPS-TAGS-001`, `DOC-RUN-001` | Parcial M1 comprovada: multi-stage, tags exatas e somente `web:8080`; CI/validação final pendentes. |
| `NFR-CONFIG-01` | `SEC-SECRET-01`, `OPS-DOCKER-01` | Base e `.env.example` opcional em M1; chave validada em M3; auditoria em M5 | M1, M3, M5 | `BE-CONFIG-001`, `OPS-SECRET-001`, revisão de configuração | Parcial M1 comprovada: Compose sem `.env` e exemplo sem valor utilizável; configuração JWT pendente em M3. |
| `NFR-SEC-01` | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` | ADR-0003, DTOs, logging, allowlist do interceptor e `sessionStorage` | M2–M5 | `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PASS-004`, `BE-DTO-001`, `FE-INT-001`, `OPS-SECRET-001` | Parcial M2: cadastro usa DTOs, hash Identity e não devolve/loga senha ou hash; autenticação, sessão e auditoria final de logs permanecem em M3–M5. |
| `NFR-TEST-01` | `TEST-FLOW-01` | Estratégia com integração principal, frontend, E2E e Compose | M1–M6 | Gates de `04-test-strategy.md` | Parcial M2: cadastro coberto por integração HTTP/SQLite, frontend e smoke padrão; login, proteção, dashboard, perfil, senha e E2E permanecem pendentes. |
| `NFR-DOC-01` | `DOC-RUN-01` | README raiz futuro; índice SDD atual | M6 | `DOC-RUN-001` | Pendente de implementação/README. |
| `NFR-SDD-01` | `DOC-SDD-01`, `DOC-TRACE-01` | `00`–`06`, OpenAPI, plano, testes e ADRs | M1–M6 | `SPEC-TRACE-001`, revisão do índice | Parcial: baseline e evidências M1/M2 atualizadas; continuidade obrigatória. |
| `NFR-SDD-02` | `DOC-SDD-01` | ADR-0001 a ADR-0004 | Design e contínuo | Revisão de decisões relevantes | Atendido nesta etapa; revisão contínua. |
| `NFR-TRACE-01` | `DOC-TRACE-01` | Este documento e extensões `x-*` do OpenAPI | M1–M6 | `SPEC-TRACE-001` | Parcial: planejamento e evidências M1/M2 registrados; M3–M6 pendentes. |
| `NFR-AI-01` | `AI-SDD-01`, `AI-EXPLAIN-01` | `ai-usage.md`, registros resumidos e walkthrough humano | Todos | Revisão de diff/decisões e `DOC-EXPLAIN-001` | Parcial: registros até M2 atualizados; walkthrough final permanece em M6. |
| `NFR-DELIVERY-01` | `DEL-REPO-01` | Publicação explícita pelo responsável | Entrega | Verificação manual da URL pública | Pendente da entrega; nenhum push nesta etapa. |

## Premissas aprovadas

| Requisito/premissa | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `PREM-ARCH-01` | `DOC-SDD-01` | Monólito modular; ADR-0001 | M1 | Inspeção da solution e build | M1 comprovado: uma API modular direta e build aprovado. |
| `PREM-ARCH-02` | `TECH-BACKEND-01` | Um `UserProfile.Api` e um `tests/backend/UserProfile.Api.IntegrationTests` | M1 | Inspeção da solution | M1 comprovado: exatamente um executável e um projeto de integração na solution. |
| `PREM-ARCH-03` | `DOC-TRACE-01` | Features `Auth`/`Profile`; sem patterns proibidos | M1–M6 | Revisão de diff por milestone | Parcial M2: feature `Auth` direta criada sem repository/facade; `Profile` permanece para M3/M4. |
| `PREM-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | EF Core SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Parcial M2 comprovada: EF/SQLite/migration/volume e usuário persistente após recriação; autenticação/revalidação final pendentes. |
| `PREM-DATA-02` | `DOC-TRACE-01` | Decisão interna do ADR-0002: entidade/migration `User`, timestamps e índice `UX_Users_NormalizedEmail` | M1–M4 | `BE-DB-001`, `BE-REG-001`, `BE-PROF-003`, `BE-PASS-003` | Parcial M2 comprovada: schema/índice e inicialização idêntica/non-default dos timestamps; preservação/avanço permanecem para M4. |
| `PREM-FE-01` | `UI-STATE-01` | Angular standalone/strict, Reactive Forms, Material | M1–M4 | Build e suíte `FE-*` | Parcial M2 comprovada: formulário de cadastro tipado, Material e estados por signals; demais formulários permanecem pendentes. |
| `PREM-LANG-01` | `DOC-SDD-01` | Código/IDs em inglês; documentação em português | Todos | Revisão de diff | Design concluído; verificação contínua. |
| `PREM-EMAIL-01` | `AC-REG-05`, `AC-PROF-04` | `Trim().ToUpperInvariant()` nos três fluxos | M2–M4 | `BE-REG-003/004`, `BE-PROF-005` | Parcial M2: cadastro comprovado; login e edição reutilizarão a regra em M3/M4. |
| `PREM-REG-01` | `AC-REG-01` | `201` sem token; `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002` | M2 concluído: `201` mínimo, sem sessão/token, e navegação com aviso comprovados. |
| `PREM-PASS-01` | `AC-PASS-01`–`04` | Endpoint/form separado e limpeza da sessão | M4 | `BE-PASS-*`, `FE-PASS-*`, `E2E-002` | Design concluído; código/teste pendentes. |
| `PREM-PROF-01` | `AC-PROF-02` | PUT de perfil separado do PUT de senha | M4 | `SPEC-OAS-002`, `BE-PROF-*`, `BE-PASS-*` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-01` | `SEC-AUTH-01` | `sub` validado; nenhum `userId` nos contratos | M3–M4 | `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-02` | `AC-DASH-01` | Dashboard usa `getCurrentProfile` | M3 | `FE-DASH-001`, `E2E-001` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-03` | `SEC-SESSION-01` | JWT de 15 minutos em `sessionStorage`, sem refresh | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | Design concluído; código/teste pendentes. |
| `PREM-ERR-01` | `API-ERROR-01` | ProblemDetails na API e nas falhas de transporte do proxy | M1–M5 | `BE-ERR-*`, `BE-HEALTH-001`, `SPEC-OAS-005`, `E2E-003`, `OPS-ORIGIN-001` | Parcial M2 comprovada por integração, UI e proxy; futuras operações ampliarão a matriz. |
| `PREM-SEED-01` | `DOC-RUN-01` | Dados criados pelo cadastro e por factories de teste | M2, M5, M6 | `E2E-001`, `DOC-RUN-001` | Parcial M2: cadastro e factories criam dados sem seed; E2E/README permanecem pendentes. |
| `PREM-OPS-01` | `OPS-DOCKER-01` | Compose inicia sem criar ou copiar `.env` | M1, M5–M6 | `OPS-COMPOSE-001`, `OPS-SECRET-001` | M1 comprovado sem `.env`; auditoria/validação final permanecem em M5–M6. |

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
| `OPS-SECRET-001` (parcial M1) | `rg --files-with-matches --hidden --glob '!.git/**'` para padrões AWS/GitHub/OpenAI/JWT/private-key e `JWT_SIGNING_KEY_BASE64` não vazio; inspeção dos `.dockerignore`; check de `.env.example` no smoke | Nenhum segredo real encontrado no repositório ou contexto; exemplo sem valor utilizável. Auditoria de logs com fluxos funcionais permanece em M5. |

## Evidência executável de M2

| ID | Arquivo, método ou comando | Parcela comprovada |
|---|---|---|
| `BE-REG-001`, `BE-DTO-001` (parcial) | `RegisterTests.cs` — `ValidRegistrationReturnsCreatedAndPersistsProtectedPassword` | Email válido exatamente em 320 caracteres é aceito; `201` contém apenas `message`; nome/email são aparados; normalização é exata; timestamps são iguais/non-default; hash difere do texto e é verificável; nenhum token/senha/hash/ID/email normalizado é retornado. |
| `BE-REG-002` | `RegisterTests.cs` — theory `InvalidRegistrationReturnsValidationProblemDetails` e `UnknownJsonPropertyReturnsValidationProblemDetails` | Doze cenários de campos/limites e `userId` extra retornam `400 ValidationProblemDetails` camelCase sem persistir usuário. |
| `BE-REG-003` | `ExactDuplicateEmailReturnsConflictWithoutSecondUser` e `DuplicateEmailIgnoresCaseAndOuterSpaces` | Duplicidade exata e equivalente por caixa/espaços retorna `409 ProblemDetails` e mantém uma linha. |
| `BE-REG-004` | `ConcurrentDuplicateEmailReturnsOneCreatedAndOneConflict`; `RegistrationSaveBarrier` test-only | As duas requisições alcançam `SaveChanges` após os prechecks; o índice produz exatamente um `201`, um `409` e uma linha, sem hook de produção. |
| `BE-ERR-001`, `BE-ERR-002` | `MalformedJsonReturnsValidationProblemDetails`, `UnsupportedMediaTypeReturnsProblemDetails`, `UnsupportedMethodReturnsProblemDetails`, `UnknownApiRouteReturnsProblemDetails` e `DatabaseFailureReturnsSafeProblemDetails` | Pipeline cobre `400/404/405/415`; lock SQLite real retorna `500 application/problem+json` em menos de cinco segundos sem segredo, SQL ou stack na resposta. |
| `BE-OAS-001` | `HealthTests.cs` — `SwaggerContainsOnlyTheImplementedOperationsAndRequiredSchemas` | Runtime expõe exatamente `/health` e `/api/auth/register`, com operação, respostas, required, `additionalProperties: false`, padrão de email, limites e senhas `password`/`writeOnly`. |
| `FE-REG-001`, `FE-REG-002` | `registration.service.spec.ts` e `register.spec.ts` | Dez testes novos cobrem contrato relativo, signals, validators equivalentes (incluindo email válido de 320 caracteres), confirmação, loading/duplo submit, `201` → `/login` com aviso e sem sessão, `400/409/503` acessíveis e retry. |
| `TECH-BACKEND-001`, `TECH-FRONTEND-001` | Restore locked/build/test .NET na imagem `10.0.400`; `npm ci`, lint/test/build na imagem Node `24.19.0` | 29/29 integrações, 12/12 testes frontend, 0 warnings/erros de build e 0 vulnerabilidades npm. |
| `SPEC-OAS-001`–`005` | `scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` | Contrato aprovado com seis operações, 42 referências e checagem explícita dos novos `maxLength`. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001` (parcial) | Compose padrão, curls same-origin, recriação da API e navegador local | Build/health passaram sem `.env`; `/`, `/register`, health e Swagger responderam; cadastro `201`, validação `400` e duplicidade `409`; o usuário permaneceu após recriação. Autenticação pós-recriação será completada em M3/M6. |

## Cobertura após M2

- Todos os 19 requisitos funcionais estão ligados a operação/tela, milestone e teste planejado.
- Todos os 14 requisitos não funcionais possuem decisão/evidência planejada.
- Todas as 17 premissas identificadas possuem ponto de verificação.
- Todos os 40 critérios de aceite/qualidade de `01-requirements.md` aparecem nesta matriz.
- A suíte acumulada registra 29 integrações backend efetivamente descobertas e 12 testes frontend, sem skips; 23 integrações e 10 testes frontend pertencem à fatia M2.
- Os quatro requisitos de cadastro e `AC-REG-01`–`06` possuem implementação/evidência. Linhas compartilhadas de UI, erro, segurança, operação e persistência estão marcadas como parciais apenas onde dependem de M3–M6.
- O OpenAPI normativo e o Swagger runtime da fatia foram validados; o Compose padrão comprovou origem única e persistência, removendo o bloqueio ambiental registrado em M1.
