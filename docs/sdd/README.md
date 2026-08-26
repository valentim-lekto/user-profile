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
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Contrato M3 implementado: login `401` genérico e leitura de perfil com ID imutável pelo `sub`. |
| [`02-technical-design.md`](02-technical-design.md) | Definir arquitetura, modelo de dados, segurança, frontend, operação e versões fixadas. | M1–M3 validados; JWT/configuração, sessão e same-origin observados. |
| [`03-api-contract.yaml`](03-api-contract.yaml) | Definir requests, responses, schemas, autenticação e erros da API. | Contrato normativo M3 validado para seis operações e 53 referências locais. |
| [`04-test-strategy.md`](04-test-strategy.md) | Definir integração, frontend, E2E, Docker e gates. | Gates M1–M3 aprovados; E2E completo e CI continuam em M5. |
| [`05-execution-plan.md`](05-execution-plan.md) | Organizar a implementação incremental em M1–M6. | M1, M2 e M3 concluídos; M4–M6 pendentes. |
| [`06-traceability.md`](06-traceability.md) | Relacionar requisito, critério, design, milestone, teste e estado. | Evidências M1–M3 registradas: 69 integrações backend, 45 testes frontend, Compose e UI real. |
| [`review-log.md`](review-log.md) | Registrar revisão independente, achados, decisões, comandos e riscos. | Revisões independentes de design, M1, M2 e M3 registradas. |
| [`ai-usage.md`](ai-usage.md) | Definir e registrar de forma resumida o uso responsável de IA. | Uso de IA, auditorias e validação até M3 resumidos. |

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
| Código, dependências, Dockerfiles e Compose | Walking skeleton M1, cadastro M2 e login/autorização/dashboard M3 concluídos; M4–M6 pendentes. |
| Testes automatizados e evidências observadas | 69 integrações backend e 45 testes frontend aprovados, além de OpenAPI normativo/runtime, smoke Compose e UI real; E2E completo e CI permanecem pendentes. |
| README de execução e validação na raiz | Planejado para M6. |
| Atualização de estados da matriz e do plano | Obrigatória em cada milestone. |
| ADRs adicionais | Criar somente se surgir nova decisão relevante. |
