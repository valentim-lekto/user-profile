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
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Funcionalidades M1–M4 e gate de qualidade M5 implementados e validados, sem ampliar o escopo de negócio. |
| [`02-technical-design.md`](02-technical-design.md) | Definir arquitetura, modelo de dados, segurança, frontend, operação e versões fixadas. | M1–M6 validados, inclusive bind de loopback, perfis Compose, npm estrito, Playwright, Actions por SHA e política segura de artefatos. |
| [`03-api-contract.yaml`](03-api-contract.yaml) | Definir requests, responses, schemas, autenticação e erros da API. | Seis operações normativas e 53 referências locais aprovadas; Swagger runtime também validado. |
| [`04-test-strategy.md`](04-test-strategy.md) | Definir integração, frontend, E2E, Docker e gates. | 101 integrações backend, 57 testes frontend, três jornadas E2E reforçadas e smoke corretivo final aprovados. |
| [`05-execution-plan.md`](05-execution-plan.md) | Organizar a implementação incremental em M1–M6. | M1–M6 concluídos quanto ao escopo técnico; publicação e confirmação humana permanecem externas. |
| [`06-traceability.md`](06-traceability.md) | Relacionar requisito, critério, design, milestone, teste e estado. | Gates técnicos/operacionais/documentais de M6 reexecutados e marcados somente com evidência real. |
| [`07-validation-report.md`](07-validation-report.md) | Registrar a auditoria final independente. | Concluído: 0 Alto/Médio aberto, comandos Docker, resultados, riscos e pendências externas registrados. |
| [`review-log.md`](review-log.md) | Registrar revisão independente, achados, decisões, comandos e riscos. | Revisões de design e M1–M5 preservadas; a auditoria final está consolidada no relatório 07. |
| [`ai-usage.md`](ai-usage.md) | Definir e registrar de forma resumida o uso responsável de IA. | Uso de IA e auditorias até M6 resumidos; explicação humana não foi presumida pela IA. |

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

## Estado de entrega e pendências externas

| Item | Estado |
|---|---|
| Código, dependências, Dockerfiles e Compose | M1–M5 implementados; M6 restringiu o bind publicado ao loopback e o tornou gate operacional, sem criar funcionalidade de negócio. |
| Testes automatizados e evidências observadas | 101 integrações backend, 57 testes frontend, três jornadas E2E, OpenAPI, actionlint, build sem cache, restart/persistência e smoke aprovados somente com Docker. |
| README de execução e validação na raiz | Concluído e confrontado com os comandos observados em M6. |
| Atualização de estados da matriz e do plano | Concluída para M6; publicação e confirmação da explicação humana permanecem Pending. |
| ADRs adicionais | Criar somente se surgir nova decisão relevante. |
