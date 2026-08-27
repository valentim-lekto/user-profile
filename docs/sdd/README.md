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
8. [`adr/`](adr/) — decisões arquiteturais relevantes e suas consequências.
9. [`review-log.md`](review-log.md) — revisões independentes, achados, decisões e validações.
10. [`ai-usage.md`](ai-usage.md) — regras e registros resumidos do uso de IA.

## Estado atual

| Artefato | Finalidade | Estado |
|---|---|---|
| [`00-challenge.md`](00-challenge.md) | Preservar o enunciado, com requisitos originais e decisões aprovadas claramente separados. | Concluído nesta etapa. |
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Funcionalidades M1–M4 e gate de qualidade M5 implementados e validados, sem ampliar o escopo de negócio. |
| [`02-technical-design.md`](02-technical-design.md) | Definir arquitetura, modelo de dados, segurança, frontend, operação e versões fixadas. | M1–M5 validados, inclusive perfis Compose, npm estrito, Playwright, Actions por SHA e política segura de artefatos. |
| [`03-api-contract.yaml`](03-api-contract.yaml) | Definir requests, responses, schemas, autenticação e erros da API. | Seis operações normativas e 53 referências locais aprovadas; Swagger runtime também validado. |
| [`04-test-strategy.md`](04-test-strategy.md) | Definir integração, frontend, E2E, Docker e gates. | 101 integrações backend, 57 testes frontend, três jornadas E2E reforçadas e smoke corretivo final aprovados. |
| [`05-execution-plan.md`](05-execution-plan.md) | Organizar a implementação incremental em M1–M6. | M1–M5 concluídos; M6 pendente. |
| [`06-traceability.md`](06-traceability.md) | Relacionar requisito, critério, design, milestone, teste e estado. | M5 concluído com evidências automatizadas, Compose, E2E e inspeção da UI real. |
| [`review-log.md`](review-log.md) | Registrar revisão independente, achados, decisões, comandos e riscos. | Revisões independentes de design e M1–M5 registradas; M5 encerrou também `REV-M1-015`–`017`. |
| [`ai-usage.md`](ai-usage.md) | Definir e registrar de forma resumida o uso responsável de IA. | Uso de IA, auditorias e revisão independente até M5 resumidos. |

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

Novas decisões relevantes devem criar ou substituir ADR; registrar apenas uma premissa em tabela não substitui essa análise.

## Pendências para as próximas etapas

| Item | Estado |
|---|---|
| Código, dependências, Dockerfiles e Compose | Walking skeleton M1, cadastro M2, login/autorização/dashboard M3, perfil/senha M4 e qualidade/E2E/CI M5 concluídos; somente M6 permanece pendente. |
| Testes automatizados e evidências observadas | 101 integrações backend, 57 testes frontend e três jornadas E2E aprovados sem skips, além de OpenAPI normativo/runtime, perfis Compose, npm estrito, smoke final e UI real. |
| README de execução e validação na raiz | Planejado para M6. |
| Atualização de estados da matriz e do plano | Obrigatória em cada milestone. |
| ADRs adicionais | Criar somente se surgir nova decisão relevante. |
