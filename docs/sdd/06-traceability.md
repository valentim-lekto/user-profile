# 06 — Matriz de rastreabilidade

**Status:** M5 concluído · **Data:** 2026-08-26

## Convenções

- **Design concluído:** requisito e critério estão ligados a uma decisão, contrato/tela, milestone e teste planejado; código e teste ainda não existem.
- **M1 concluído/parcial:** a parcela explicitamente atribuída ao walking skeleton possui implementação e evidência; comportamento de milestones futuros continua pendente.
- **M2 concluído/parcial:** a parcela de cadastro possui implementação e evidência pós-revisão; login, autenticação e perfil continuam pendentes.
- **M3 concluído/parcial:** login, JWT, sessão, guard, interceptor, dashboard e leitura protegida do perfil possuem implementação e evidência; suas jornadas completas foram confirmadas em M5.
- **M4 concluído/parcial:** edição protegida de perfil/senha, formulários, atomicidade, atualização do dashboard e encerramento de sessão possuem implementação e evidência; suas jornadas completas foram confirmadas em M5.
- **M5 concluído/parcial:** cobertura acumulada, acabamento, três jornadas Playwright, perfis Compose e definição de CI possuem implementação e evidência local; somente a execução hospedada, dependente de publicação, e os gates finais de M6 permanecem futuros.
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
| `FR-REG-04` | `AC-REG-01`, `AC-REG-06` | `201 MessageResponse`, navegação `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002`, `E2E-001` | M5 comprovado de ponta a ponta: cadastro retorna sucesso e conduz ao login antes do dashboard. |
| `FR-LOGIN-01` | `AC-LOGIN-01`, `AC-LOGIN-02` | DTO `LoginRequest`, `loginUser`, `/login` | M3 | `BE-LOGIN-001/002/003`, `FE-LOGIN-001` | M3 concluído: login normalizado, validação e credenciais verificadas por integração/frontend/smoke. |
| `FR-LOGIN-02` | `AC-LOGIN-01`, `SEC-SESSION-01` | Emissor JWT, `LoginResponse`, `AuthService`, `/dashboard` | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | M3 concluído: token curto com claims mínimas, sessão em `sessionStorage` e navegação ao dashboard aprovados. |
| `FR-LOGIN-03` | `AC-LOGIN-02` | `401 ProblemDetails` genérico para credenciais não reconhecidas; `400` somente para payload inválido; estado de erro do login | M3 | `BE-LOGIN-002`, `FE-LOGIN-001`, `E2E-002` | M5 comprovado: respostas de credenciais inexistentes/incorretas permanecem genéricas e o login inválido conserva a tela/mensagem esperadas. |
| `FR-AUTH-01` | `AC-DASH-02` | JWT middleware, functional guard, rotas `/dashboard` e `/profile` | M3–M4 | `BE-AUTH-001`, `BE-PASS-004`, `FE-GUARD-001`, `FE-WIRE-001`, `E2E-002` | M5 comprovado: Bearer, guard, wiring e endpoints protegidos passaram nas suítes; acesso anônimo real ao dashboard redirecionou ao login. |
| `FR-AUTH-02` | `SEC-AUTH-01`, `AC-PROF-01` | Leitura de `sub`; `getCurrentProfile`, `updateCurrentProfile`, `changeCurrentPassword` | M3–M4 | `BE-AUTH-002`, `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | M4 concluído: GET e os dois PUTs identificam exclusivamente pelo `sub`; overposting, IDs em query/header e isolamento entre usuários foram rejeitados/comprovados. |
| `FR-DASH-01` | `AC-DASH-01`, `AC-DASH-04` | `getCurrentProfile`, `ProfileService`, `/dashboard` | M3 | `BE-PROF-001`, `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M3 concluído: dashboard protegido busca a API e mostra o nome; loading/erro e isolamento entre sessões foram exercitados na suíte, enquanto a UI real comprovou o fluxo feliz. |
| `FR-DASH-02` | `AC-DASH-03` | Link `/dashboard` → `/profile` | M3 | `FE-DASH-001`, `E2E-001` | M5 comprovado: a jornada real navega do dashboard ao perfil, salva e retorna ao dashboard. |
| `FR-PROF-01` | `AC-PROF-01` | `ProfileResponse` com `id/name/email`, `getCurrentProfile`, `/profile` | M3–M4 | `BE-PROF-001/002`, `FE-PROF-001` | M4 concluído: a tela carrega o perfil autenticado e o backend mantém o DTO mínimo, sem senha/hash. |
| `FR-PROF-02` | `AC-PROF-02`, `AC-PROF-03`, `AC-PROF-04` | `UpdateProfileRequest`, `updateCurrentProfile`, formulário de dados | M4 | `BE-PROF-003/004/005/006`, `FE-PROF-001/002` | M4 concluído: validações equivalentes, borda positiva de email 320, normalização, unicidade/race, isolamento, payload mínimo e operações inválidas sem mutação foram aprovados. |
| `FR-PROF-03` | `AC-PROF-05` | `ProfileResponse`, signals de loading/sucesso/erro | M4 | `BE-PROF-003`, `FE-PROF-002`, `E2E-001` | M5 comprovado: submissão/feedback/bloqueio passaram e a jornada real confirmou o novo nome após nova consulta do dashboard. |
| `FR-PASS-01` | `AC-PASS-01` | `ChangePasswordRequest`, `changeCurrentPassword`, formulário separado | M4 | `FE-PASS-001`, `SPEC-OAS-004` | M4 concluído: formulário e endpoint separados, campos obrigatórios, wiring DOM e limite inclusivo 128 foram aprovados. |
| `FR-PASS-02` | `AC-PASS-02`, `AC-PASS-03` | PasswordHasher e `400 ValidationProblemDetails` | M4 | `BE-PASS-001/002`, `FE-PASS-001/002` | M4 concluído: senha atual inválida, confirmação divergente e novas senhas inválidas preservam integralmente usuário/hash; troca válida invalida a senha antiga e aceita a nova. |
| `FR-PASS-03` | `AC-PASS-04` | `200 MessageResponse`, limpeza de `sessionStorage` condicionada ao token iniciador | M4 | `BE-PASS-003`, `FE-PASS-002`, `E2E-003` | M5 comprovado: sucesso encerra a sessão; a jornada real rejeita a senha antiga e aceita a nova, enquanto o teste focado preserva uma sessão posterior. |
| `FR-UI-01` | `UI-STATE-01`, `AC-LOGIN-03`, `AC-DASH-04` | Signals e estados em cadastro, login, dashboard e perfil/senha | M2–M5 | `FE-REG-001/002`, `FE-LOGIN-001`, `FE-DASH-001`, `FE-PROF-001/002`, `FE-PASS-002`, `E2E-001/002/003` | M5 concluído: 57 testes e UI real comprovam feedback, loading, foco, prevenção de repetição, mensagens acessíveis, teclado e layout responsivo nas quatro telas. |
| `FR-ERR-01` | `API-ERROR-01` | Schemas/responses ProblemDetails; Nginx converte `413` e `502/504` → `503` | M1–M5 | `BE-ERR-001/002`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `CI-001` | M5 concluído na fatia: integração/contrato/smoke comprovam `400/401/404/405/409/413/415/500/503`; o workflow repete os gates sem criar quarta jornada E2E. |

## Requisitos não funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `NFR-TECH-01` | `TECH-BACKEND-01` | .NET 10, Controllers, EF Core SQLite 10, JWT e lock NuGet; ADR-0001 | M1–M5 | `TECH-BACKEND-001`, build e suíte `BE-*` | M5 revalidado: restore locked, build com 0 warnings/erros e 101 integrações sem skips passaram no target Docker fixado. |
| `NFR-TECH-02` | `TECH-FRONTEND-01` | Angular standalone/strict, Material, Reactive Forms, signals e lock npm | M1–M5 | `TECH-FRONTEND-001`, build e suíte `FE-*` | M5 revalidado: lint, 57 testes sem skips, build sem warnings e acabamento Material/responsivo/acessível passaram no target Docker fixado. |
| `NFR-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001`, `OPS-COMPOSE-001` | Parcial M4: smoke isolado recriou a API e confirmou persistência de perfil/senha no SQLite; revalidação final de M6 permanece pendente. |
| `NFR-OPS-01` | `OPS-DOCKER-01` | `compose.yaml`, Nginx com `413/503 ProblemDetails`, API e volume; `/health` e `/swagger` | M1, M6 | `OPS-COMPOSE-001`, `OPS-ORIGIN-001` | Parcial M4 concluída: config, origem, PUTs, atomicidade, persistência, `413/415`, `503`, logs e cleanup foram aprovados em projeto/volume isolados; validação final permanece M6. |
| `NFR-OPS-02` | `OPS-DOCKER-02` | Origem única, builds multi-stage, perfis de teste e tags completas; ADR-0004 | M1, M5, M6 | `OPS-COMPOSE-001`, `OPS-TAGS-001`, `CI-001`, `DOC-RUN-001` | M5 concluído na fatia: runtime multi-stage, tags completas, somente `web:8080`, quatro profiles sem SDK local e workflow CI foram comprovados; walkthrough limpo final permanece M6. |
| `NFR-CONFIG-01` | `SEC-SECRET-01`, `OPS-DOCKER-01` | Base e `.env.example` opcional em M1; chave validada em M3; auditoria em M5 | M1, M3, M5 | `BE-CONFIG-001`, `OPS-SECRET-001`, revisão de configuração | M5 concluído: chave externa validada, falha fechada, fallback somente Development, Compose sem `.env` e auditoria acumulada sem segredo versionado/logado. |
| `NFR-SEC-01` | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` | ADR-0003, DTOs, logging, allowlist do interceptor, wiring de produção e `sessionStorage` | M2–M5 | `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PASS-004`, `BE-DTO-001`, `FE-INT-001/002`, `FE-PASS-002`, `FE-WIRE-001`, `OPS-SECRET-001` | M5 concluído: identidade por `sub`, DTOs/sessão/logs seguros e artefatos E2E sem senha/JWT foram comprovados; auditoria independente não deixou achado aberto. |
| `NFR-TEST-01` | `TEST-FLOW-01` | Estratégia com integração principal, frontend, E2E, Compose e CI | M1–M6 | Gates de `04-test-strategy.md`, `E2E-*`, `CI-001` | M5 concluído na fatia: 101 integrações, 57 testes frontend, três jornadas Playwright, contrato e smoke passaram; workflow equivalente foi validado estaticamente, com execução hospedada condicionada ao futuro push. |
| `NFR-DOC-01` | `DOC-RUN-01` | README raiz futuro; índice SDD atual | M6 | `DOC-RUN-001` | Pendente de implementação/README. |
| `NFR-SDD-01` | `DOC-SDD-01`, `DOC-TRACE-01` | `00`–`06`, OpenAPI, plano, testes e ADRs | M1–M6 | `SPEC-TRACE-001`, revisão do índice | Parcial: documentação, contrato e evidências M1–M5 foram atualizados/revalidados; fechamento integral permanece obrigatório em M6. |
| `NFR-SDD-02` | `DOC-SDD-01` | ADR-0001 a ADR-0004 | Design e contínuo | Revisão de decisões relevantes | Atendido nesta etapa; revisão contínua. |
| `NFR-TRACE-01` | `DOC-TRACE-01` | Este documento e extensões `x-*` do OpenAPI | M1–M6 | `SPEC-TRACE-001` | Parcial: evidências M1–M5 e extensões de contrato estão mapeadas/aprovadas; revalidação final permanece M6. |
| `NFR-AI-01` | `AI-SDD-01`, `AI-EXPLAIN-01` | `ai-usage.md`, registros resumidos e walkthrough humano | Todos | Revisão de diff/decisões e `DOC-EXPLAIN-001` | Parcial: uso de IA, auditorias e validação de M5 estão resumidos sem transcrição; walkthrough humano permanece M6. |
| `NFR-DELIVERY-01` | `DEL-REPO-01` | Publicação explícita pelo responsável | Entrega | Verificação manual da URL pública | Pendente da entrega; nenhum push nesta etapa. |

## Premissas aprovadas

| Requisito/premissa | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `PREM-ARCH-01` | `DOC-SDD-01` | Monólito modular; ADR-0001 | M1 | Inspeção da solution e build | M1 comprovado: uma API modular direta e build aprovado. |
| `PREM-ARCH-02` | `TECH-BACKEND-01` | Um `UserProfile.Api` e um `tests/backend/UserProfile.Api.IntegrationTests` | M1 | Inspeção da solution | M1 comprovado: exatamente um executável e um projeto de integração na solution. |
| `PREM-ARCH-03` | `DOC-TRACE-01` | Features `Auth`/`Profile`; sem patterns proibidos | M1–M6 | Revisão de diff por milestone | Parcial M5: `Auth`/`Profile` seguem diretas e M5 não acrescentou camada ou funcionalidade de negócio; revisão final permanece M6. |
| `PREM-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | EF Core SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Parcial M4 aprovada: EF/SQLite/migration/volume e persistência dos dados cadastrais e da senha após recriação foram revalidados; repetição final permanece M6. |
| `PREM-DATA-02` | `DOC-TRACE-01` | Decisão interna do ADR-0002: entidade/migration `User`, timestamps e índice `UX_Users_NormalizedEmail` | M1–M4 | `BE-DB-001`, `BE-REG-001`, `BE-PROF-003`, `BE-PASS-003` | M4 concluído na fatia: schema/índice e relógio controlado comprovam preservação de `CreatedAtUtc` e avanço de `UpdatedAtUtc` somente em atualizações válidas. |
| `PREM-FE-01` | `UI-STATE-01` | Angular standalone/strict, Reactive Forms, Material | M1–M5 | Build, suíte `FE-*` e `E2E-*` | M5 concluído na fatia: quatro telas Material, formulários tipados, signals, acessibilidade/responsividade, lint/build, 57 testes e três jornadas passaram. |
| `PREM-LANG-01` | `DOC-SDD-01` | Código/IDs em inglês; documentação em português | Todos | Revisão de diff | Design concluído; verificação contínua. |
| `PREM-EMAIL-01` | `AC-REG-05`, `AC-PROF-04` | Para emails ASCII aceitos: `Trim().ToUpperInvariant()` nos três fluxos | M2–M4 | `BE-REG-003/004`, `BE-LOGIN-003`, `BE-PROF-005` | M4 concluído: cadastro, login e edição reutilizam a mesma regra; colisões normalizadas amigáveis e concorrentes foram aprovadas. |
| `PREM-INPUT-01` | `AC-REG-02`–`04`, `API-ERROR-01` | Refinamentos internos: nome/email `200/320` após `Trim`, email ASCII, senha/confirmação `128` sem aparar, JSON camelCase case-sensitive e corpo de 1 MiB com `413` | M2–M4 | `BE-REG-001/002/003`, `FE-REG-001`, `BE-LOGIN-002`, `BE-PROF-004`, `BE-PASS-001/002`, `SPEC-OAS-004/005`, `OPS-COMPOSE-001` | Decisão interna, não requisito original. Os usos de cadastro, login, edição e senha foram aprovados em backend, frontend, contrato e smoke até M4. |
| `PREM-REG-01` | `AC-REG-01` | `201` sem token; `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002` | M2 concluído: `201` mínimo, ausência de sessão/token e navegação foram aprovados. |
| `PREM-PASS-01` | `AC-PASS-01`–`04` | Endpoint/form separado e limpeza da sessão | M4–M5 | `BE-PASS-*`, `FE-PASS-*`, `E2E-003` | M5 comprovado: operação/form separados e reautenticação real rejeita senha antiga e aceita a nova após encerrar a sessão. |
| `PREM-PROF-01` | `AC-PROF-02` | PUT de perfil separado do PUT de senha | M4 | `SPEC-OAS-002`, `BE-PROF-*`, `BE-PASS-*` | M4 concluído: operações, DTOs, formulários e payloads permanecem estritamente separados. |
| `PREM-AUTH-01` | `SEC-AUTH-01` | `sub` validado; nenhum `userId` em requests; `ProfileResponse` pode devolver o ID imutável | M3–M4 | `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | M4 concluído: GET e ambos os PUTs são resolvidos pelo `sub`; IDs arbitrários/overposting não selecionam nem alteram outro usuário. |
| `PREM-AUTH-02` | `AC-DASH-01` | Dashboard usa `getCurrentProfile` com estado por ativação | M3–M5 | `FE-DASH-001`, `FE-WIRE-001`, `E2E-001` | M5 comprovado: dashboard usa `GET /api/profile`, isola respostas e a jornada real mostra o nome cadastrado e depois o atualizado. |
| `PREM-AUTH-03` | `SEC-SESSION-01` | JWT de 15 minutos em `sessionStorage`, sem refresh | M3 | `BE-LOGIN-001`, `FE-LOGIN-002`, `FE-INT-002` | M3 concluído: duração, claims, armazenamento, logout e limpeza em `401` somente para a mesma sessão foram aprovados; não há refresh. |
| `PREM-ERR-01` | `API-ERROR-01` | ProblemDetails na API e nas falhas de transporte do proxy, inclusive `413/415` | M1–M5 | `BE-ERR-*`, `BE-HEALTH-001`, `SPEC-OAS-005`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `CI-001` | M5 concluído na fatia: integração, contrato e proxy comprovam a matriz de erros; workflow versionado repete esses gates. |
| `PREM-SEED-01` | `DOC-RUN-01` | Dados criados pelo cadastro e por factories de teste | M2, M5, M6 | `E2E-001`, `DOC-RUN-001` | Parcial M5: cadastro/factories/E2E criam dados independentes sem seed; instrução final do README permanece M6. |
| `PREM-OPS-01` | `OPS-DOCKER-01` | Compose inicia sem criar ou copiar `.env` | M1–M6 | `OPS-COMPOSE-001`, `OPS-SECRET-001` | M1–M5 aprovados sem `.env`, inclusive profiles, E2E e smoke/log; repetição em checkout limpo permanece M6. |

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
| `E2E-001` | Playwright: cadastro → login → dashboard → perfil → nome/email atualizados → nova consulta do dashboard → logout | Aprovado em viewport de 360 px, inclusive ausência de overflow com nome no limite de 200 caracteres. |
| `E2E-002` | Playwright: dashboard anônimo → login; skip link por teclado; credenciais inválidas genéricas | Aprovado sem preparar estado pela API e sem retry/sleep. |
| `E2E-003` | Playwright: cadastro/login próprios → troca de senha → sessão encerrada → senha antiga falha → nova autentica | Aprovado com dados/contexto/volume independentes e cleanup isolado. |
| `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-TAGS-001`, `OPS-SECRET-001` | `docker compose config`, quatro profiles opt-in, `scripts/validate-m1-compose.sh` e inspeção de logs/artefatos | Aprovados: SDKs dispensados no host, tags completas, origem única, health/Swagger, persistência, `413/415/503`, logs seguros e recursos efêmeros removidos. Uma prova com trace forçado encontrou 0 senha/JWT nos três traces, cópias do report e JUnit. |
| `CI-001` | `.github/workflows/ci.yml`, `actionlint:1.7.12` e execução local dos profiles/scripts do job | Workflow válido: backend, frontend, contrato, imagens, smoke e E2E; traps dos projetos isolados persistem diagnóstico sanitizado antes do teardown, a etapa final agrega os artefatos e sempre faz cleanup. A execução hospedada exige o push explicitamente fora do escopo. |
| `FR-UI-01`, `PREM-FE-01` | Specs DOM, `E2E-*` e inspeção real desktop/360 px | Shell/telas Material consistentes, landmarks/headings/labels, `aria-live`, foco de erro, skip link, loading e ações responsivas aprovados sem layout excessivo. |

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
- M6 permanece pendente para README raiz, reprodução em checkout limpo, revalidação final integral, walkthrough humano e publicação explícita. Nenhum push foi realizado em M5.
