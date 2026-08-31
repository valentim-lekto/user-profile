# Artefatos SDD

Este diretório reúne os artefatos versionados do processo Spec-Driven Development. Consulte este índice e os documentos aplicáveis antes de alterar comportamento.

## Ordem de leitura

1. [`00-challenge.md`](00-challenge.md) — fonte estruturada do enunciado e das premissas recebidas.
2. [`01-requirements.md`](01-requirements.md) — requisitos normalizados e critérios de aceite estáveis.
3. [`02-technical-design.md`](02-technical-design.md) — arquitetura, dados, segurança, frontend, Docker e versões.
4. [`03-api-contract.yaml`](03-api-contract.yaml) — contrato HTTP normativo em OpenAPI.
5. [`04-test-strategy.md`](04-test-strategy.md) — níveis, cenários e gates de teste.
6. [`05-execution-plan.md`](05-execution-plan.md) — plano executável M1–M6.
7. [`06-traceability.md`](06-traceability.md) — ligação entre requisitos, design, plano e evidências.
8. [`07-validation-report.md`](07-validation-report.md) — auditoria final, comandos e evidências reais.
9. [`adr/`](adr/) — decisões arquiteturais relevantes e suas consequências.
10. [`review-log.md`](review-log.md) — revisões independentes, achados, decisões e validações.
11. [`ai-usage.md`](ai-usage.md) — regras e registros resumidos do uso de IA.

## Estado atual

| Artefato | Finalidade | Estado |
|---|---|---|
| [`00-challenge.md`](00-challenge.md) | Preservar o enunciado, com requisitos originais e decisões aprovadas claramente separados. | Concluído nesta etapa. |
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Funcionalidades M1–M4, gate M5 e critérios pós-M6, inclusive rate limiting e feedback visual sem sobreposição, implementados e validados localmente. |
| [`02-technical-design.md`](02-technical-design.md) | Definir arquitetura, modelo de dados, segurança, frontend, operação e versões fixadas. | M1–M6 e atividades pós-M6 validadas; o hardening local dos endpoints públicos está registrado no ADR-0005 e o layout de mensagens usa altura dinâmica. |
| [`03-api-contract.yaml`](03-api-contract.yaml) | Definir requests, responses, schemas, autenticação e erros da API. | Seis operações e 56 referências locais; `429 RateLimitProblem` existe somente em cadastro/login e fixa headers/campos coerentes no contrato e Swagger runtime. |
| [`04-test-strategy.md`](04-test-strategy.md) | Definir integração, frontend, E2E, Docker e gates. | 121 integrações, 71 testes frontend, 3 E2E, Stryker e `SPEC-OAS-006`/`OPS-RATE-001/002` aprovados localmente; os oráculos do limiter e da geometria das mensagens foram fortalecidos após revisão. |
| [`05-execution-plan.md`](05-execution-plan.md) | Organizar a implementação incremental em M1–M6 e atividades de qualidade posteriores. | M1–M6 e atividades posteriores, inclusive rate limiting, seus oráculos e a correção visual das mensagens, concluídos localmente. |
| [`06-traceability.md`](06-traceability.md) | Relacionar requisito, critério, design, milestone, teste e estado. | 19 requisitos funcionais, 15 não funcionais, 18 premissas e 46 critérios ligados a design, implementação e evidência. |
| [`07-validation-report.md`](07-validation-report.md) | Registrar a auditoria final independente. | Concluído com adendos pós-M6, inclusive rate limiting e a prova real de mensagens sem sobreposição. |
| [`review-log.md`](review-log.md) | Registrar revisão independente, achados, decisões, comandos e riscos. | Revisões de design e M1–M6, inclusive revisões pós-M6 completa, responsiva, visual, de banco, queries e seus oráculos, preservadas. |
| [`ai-usage.md`](ai-usage.md) | Definir e registrar de forma resumida o uso responsável de IA. | Uso de IA, revisões/correções, mutation testing, refinamentos visuais e rate limiting resumidos; explicação humana não foi presumida pela IA. |

## Governança

- [`AGENTS.md`](../../AGENTS.md) contém as regras de execução e qualidade.
- [`PLANS.md`](../../PLANS.md) define somente o formato dos planos executáveis.
- IDs de requisitos, critérios e premissas publicados não devem ser reutilizados com outro significado.
- Planos, implementação e testes devem referenciar diretamente os critérios atendidos.

## ADRs aceitos

- [`ADR-0001`](adr/0001-modular-monolith.md) — monólito modular.
- [`ADR-0002`](adr/0002-sqlite-persistence.md) — SQLite, volume e migrations.
- [`ADR-0003`](adr/0003-jwt-authentication.md) — senha, JWT e sessão.
- [`ADR-0004`](adr/0004-nginx-same-origin.md) — Nginx e origem única.
- [`ADR-0005`](adr/0005-nginx-auth-rate-limiting.md) — rate limiting local de cadastro e login no Nginx.

Novas decisões relevantes devem criar ou substituir ADR; registrar apenas uma premissa em tabela não substitui essa análise.

## Estado de entrega e pendências externas

| Item | Estado |
|---|---|
| Código, dependências, Dockerfiles e Compose | M1–M6 e correções pós-revisão implementados; rate limiting local protege cadastro/login sem serviço, migration ou funcionalidade de negócio nova. |
| Testes automatizados e evidências observadas | 121 integrações backend, 71 testes frontend, três jornadas E2E, Stryker 97,50%, OpenAPI com 56 referências, restart/persistência, geometria responsiva e smoke concorrente aprovados com Docker. |
| README de execução e validação na raiz | Concluído e confrontado com os comandos observados em M6. |
| Atualização de estados da matriz e do plano | Concluída para M6 e atividades pós-M6, inclusive rate limiting; CI hospedada, publicação e confirmação da explicação humana permanecem Pending. |
| ADRs adicionais | Criar somente se surgir nova decisão relevante. |
