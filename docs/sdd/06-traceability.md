# 06 — Matriz de rastreabilidade

**Status:** baseline de design · **Data:** 2026-08-24

## Convenções

- **Design concluído:** requisito e critério estão ligados a uma decisão, contrato/tela, milestone e teste planejado; código e teste ainda não existem.
- **Parcial:** parte documental já existe, mas o critério depende de implementação ou entrega futura.
- **Pendente:** depende integralmente de milestone futuro.
- O estado deve ser atualizado no mesmo commit que altera implementação ou evidência.
- IDs de testes são definidos em [`04-test-strategy.md`](04-test-strategy.md); operações em [`03-api-contract.yaml`](03-api-contract.yaml).

## Requisitos funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `FR-REG-01` | `AC-REG-01`, `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | DTO `RegisterRequest`, `registerUser`, `/register` | M2 | `BE-REG-001/002`, `FE-REG-001` | Design concluído; código/teste pendentes. |
| `FR-REG-02` | `AC-REG-02`, `AC-REG-03`, `AC-REG-04` | Schemas OpenAPI e Reactive Form de cadastro | M2 | `BE-REG-002`, `FE-REG-001` | Design concluído; código/teste pendentes. |
| `FR-REG-03` | `AC-REG-05` | Normalização, índice `UX_Users_NormalizedEmail`, `registerUser` | M2 | `BE-REG-003/004` | Design concluído; código/teste pendentes. |
| `FR-REG-04` | `AC-REG-01`, `AC-REG-06` | `201 MessageResponse`, navegação `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002`, `E2E-001` | Design concluído; código/teste pendentes. |
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
| `FR-UI-01` | `UI-STATE-01`, `AC-LOGIN-03`, `AC-DASH-04` | Signals e estados em cadastro, login, dashboard e perfil/senha | M2–M4 | `FE-REG-002`, `FE-LOGIN-001`, `FE-DASH-001`, `FE-PROF-001/002`, `FE-PASS-002` | Design concluído; código/teste pendentes. |
| `FR-ERR-01` | `API-ERROR-01` | Schemas/responses ProblemDetails; middleware e conversão `502/504` → `503` no Nginx | M1–M5 | `BE-ERR-001/002`, `BE-HEALTH-001`, `SPEC-OAS-005`, `E2E-003`, `OPS-ORIGIN-001` | Design concluído; código/teste pendentes. |

## Requisitos não funcionais

| Requisito | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `NFR-TECH-01` | `TECH-BACKEND-01` | .NET 10, Controllers, EF Core SQLite 10, JWT e lock NuGet; ADR-0001 | M1–M3 | `TECH-BACKEND-001`, build e suíte `BE-*` | Design concluído; código/teste pendentes. |
| `NFR-TECH-02` | `TECH-FRONTEND-01` | Angular standalone/strict, Material, Reactive Forms, signals e lock npm | M1–M4 | `TECH-FRONTEND-001`, build e suíte `FE-*` | Design concluído; código/teste pendentes. |
| `NFR-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Design concluído; código/teste pendentes. |
| `NFR-OPS-01` | `OPS-DOCKER-01` | `compose.yaml`, Nginx com `503 ProblemDetails`, API e volume; `/health` | M1, M6 | `OPS-COMPOSE-001`, `OPS-ORIGIN-001` | Design concluído; código/teste pendentes. |
| `NFR-OPS-02` | `OPS-DOCKER-02` | Origem única, builds multi-stage e tags completas; ADR-0004 | M1, M5, M6 | `OPS-COMPOSE-001`, `OPS-TAGS-001`, `DOC-RUN-001` | Design concluído; código/teste pendentes. |
| `NFR-CONFIG-01` | `SEC-SECRET-01`, `OPS-DOCKER-01` | Base em M1; chave validada e `.env.example` opcional em M3; auditoria em M5 | M1, M3, M5 | `BE-CONFIG-001`, `OPS-SECRET-001`, revisão de configuração | Design concluído; código/teste pendentes. |
| `NFR-SEC-01` | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` | ADR-0003, DTOs, logging, allowlist do interceptor e `sessionStorage` | M2–M5 | `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PASS-004`, `BE-DTO-001`, `FE-INT-001`, `OPS-SECRET-001` | Design concluído; código/teste pendentes. |
| `NFR-TEST-01` | `TEST-FLOW-01` | Estratégia com integração principal, frontend, E2E e Compose | M1–M6 | Gates de `04-test-strategy.md` | Estratégia concluída; testes pendentes. |
| `NFR-DOC-01` | `DOC-RUN-01` | README raiz futuro; índice SDD atual | M6 | `DOC-RUN-001` | Pendente de implementação/README. |
| `NFR-SDD-01` | `DOC-SDD-01`, `DOC-TRACE-01` | `00`–`06`, OpenAPI, plano, testes e ADRs | M1–M6 | `SPEC-TRACE-001`, revisão do índice | Parcial: baseline criado; atualização contínua. |
| `NFR-SDD-02` | `DOC-SDD-01` | ADR-0001 a ADR-0004 | Design e contínuo | Revisão de decisões relevantes | Atendido nesta etapa; revisão contínua. |
| `NFR-TRACE-01` | `DOC-TRACE-01` | Este documento e extensões `x-*` do OpenAPI | M1–M6 | `SPEC-TRACE-001` | Parcial: planejamento completo; evidências pendentes. |
| `NFR-AI-01` | `AI-SDD-01`, `AI-EXPLAIN-01` | `ai-usage.md`, registros resumidos e walkthrough humano | Todos | Revisão de diff/decisões e `DOC-EXPLAIN-001` | Em andamento. |
| `NFR-DELIVERY-01` | `DEL-REPO-01` | Publicação explícita pelo responsável | Entrega | Verificação manual da URL pública | Pendente da entrega; nenhum push nesta etapa. |

## Premissas aprovadas

| Requisito/premissa | Critério de aceite | Design / endpoint / tela | Milestone | Teste planejado | Estado |
|---|---|---|---|---|---|
| `PREM-ARCH-01` | `DOC-SDD-01` | Monólito modular; ADR-0001 | M1 | Inspeção da solution e build | Design concluído; verificação pendente. |
| `PREM-ARCH-02` | `TEST-FLOW-01` | Um `UserProfile.Api` e um `UserProfile.Api.IntegrationTests` | M1 | Inspeção da solution | Design concluído; verificação pendente. |
| `PREM-ARCH-03` | `DOC-TRACE-01` | Features `Auth`/`Profile`; sem patterns proibidos | M1–M6 | Revisão de diff por milestone | Design concluído; verificação pendente. |
| `PREM-DATA-01` | `OPS-DOCKER-01`, `OPS-DOCKER-03` | EF Core SQLite, migrations e volume; ADR-0002 | M1, M6 | `BE-DB-001`, `OPS-PERSIST-001` | Design concluído; verificação pendente. |
| `PREM-FE-01` | `UI-STATE-01` | Angular standalone/strict, Reactive Forms, Material | M1–M4 | Build e suíte `FE-*` | Design concluído; verificação pendente. |
| `PREM-LANG-01` | `DOC-SDD-01` | Código/IDs em inglês; documentação em português | Todos | Revisão de diff | Design concluído; verificação contínua. |
| `PREM-EMAIL-01` | `AC-REG-05`, `AC-PROF-04` | `Trim().ToUpperInvariant()` nos três fluxos | M2–M4 | `BE-REG-003/004`, `BE-PROF-005` | Design concluído; código/teste pendentes. |
| `PREM-REG-01` | `AC-REG-01` | `201` sem token; `/register` → `/login` | M2 | `BE-REG-001`, `FE-REG-002` | Design concluído; código/teste pendentes. |
| `PREM-PASS-01` | `AC-PASS-01`–`04` | Endpoint/form separado e limpeza da sessão | M4 | `BE-PASS-*`, `FE-PASS-*`, `E2E-002` | Design concluído; código/teste pendentes. |
| `PREM-PROF-01` | `AC-PROF-02` | PUT de perfil separado do PUT de senha | M4 | `SPEC-OAS-002`, `BE-PROF-*`, `BE-PASS-*` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-01` | `SEC-AUTH-01` | `sub` validado; nenhum `userId` nos contratos | M3–M4 | `BE-PROF-002/006`, `BE-PASS-004`, `SPEC-OAS-003` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-02` | `AC-DASH-01` | Dashboard usa `getCurrentProfile` | M3 | `FE-DASH-001`, `E2E-001` | Design concluído; código/teste pendentes. |
| `PREM-AUTH-03` | `SEC-SESSION-01` | JWT de 15 minutos em `sessionStorage`, sem refresh | M3 | `BE-LOGIN-001`, `FE-LOGIN-002` | Design concluído; código/teste pendentes. |
| `PREM-ERR-01` | `API-ERROR-01` | ProblemDetails na API e nas falhas de transporte do proxy | M1–M5 | `BE-ERR-*`, `BE-HEALTH-001`, `SPEC-OAS-005`, `E2E-003`, `OPS-ORIGIN-001` | Design concluído; código/teste pendentes. |
| `PREM-SEED-01` | `DOC-RUN-01` | Dados criados pelo cadastro e por factories de teste | M2, M5, M6 | `E2E-001`, `DOC-RUN-001` | Design concluído; código/teste pendentes. |
| `PREM-OPS-01` | `OPS-DOCKER-01` | Compose inicia sem criar ou copiar `.env` | M1, M5–M6 | `OPS-COMPOSE-001`, `OPS-SECRET-001` | Design concluído; código/teste pendentes. |

## Cobertura desta baseline

- Todos os 19 requisitos funcionais estão ligados a operação/tela, milestone e teste planejado.
- Todos os 14 requisitos não funcionais possuem decisão/evidência planejada.
- Todas as 16 premissas identificadas possuem ponto de verificação.
- Todos os 40 critérios de aceite/qualidade de `01-requirements.md` aparecem nesta matriz.
- Nenhuma linha afirma implementação ou teste concluído; nesta etapa existem apenas design e planejamento.
