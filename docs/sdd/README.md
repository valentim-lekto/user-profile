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
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Funcionalidades M1–M4, gate M5 e `UI-RESP-01` implementados e validados, sem ampliar o escopo de negócio. |
| [`02-technical-design.md`](02-technical-design.md) | Definir arquitetura, modelo de dados, segurança, frontend, operação e versões fixadas. | M1–M6, revisão posterior, mutation testing, refinamento visual e correção responsiva — inclusive dashboard sem conteúdo redundante — validados localmente. |
| [`03-api-contract.yaml`](03-api-contract.yaml) | Definir requests, responses, schemas, autenticação e erros da API. | Seis operações normativas e 53 referências locais aprovadas; Swagger runtime também validado. |
| [`04-test-strategy.md`](04-test-strategy.md) | Definir integração, frontend, E2E, Docker e gates. | 111 integrações backend, 68 testes frontend, três jornadas E2E, `FE-DASH-001`/`FE-VISUAL-001` — inclusive landscape curto, foco/ordem, nome-limite, dashboard sem cards redundantes e perfil sem `id` técnico — e baseline Stryker limpa de 97,41% aprovados localmente. |
| [`05-execution-plan.md`](05-execution-plan.md) | Organizar a implementação incremental em M1–M6 e atividades de qualidade posteriores. | M1–M6, mutation testing, refinamento visual e correção responsiva local concluídos; publicação, CI hospedada e confirmação humana permanecem externas. |
| [`06-traceability.md`](06-traceability.md) | Relacionar requisito, critério, design, milestone, teste e estado. | Mutation testing, refinamento visual e `UI-RESP-01` estão Verified localmente; execução hospedada continua Pending. |
| [`07-validation-report.md`](07-validation-report.md) | Registrar a auditoria final independente. | Concluído com adendos pós-M6 de revisão, mutação e validação visual real, incluindo perfil sem ID técnico, dashboard sem cards redundantes e correções responsivas. |
| [`review-log.md`](review-log.md) | Registrar revisão independente, achados, decisões, comandos e riscos. | Revisões de design e M1–M6, inclusive revisões pós-M6 completa e responsiva, preservadas. |
| [`ai-usage.md`](ai-usage.md) | Definir e registrar de forma resumida o uso responsável de IA. | Uso de IA, revisão completa, correções, mutation testing e refinamentos visuais/responsivos resumidos; explicação humana não foi presumida pela IA. |

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
| Código, dependências, Dockerfiles e Compose | M1–M5 implementados; M6 restringiu o bind ao loopback; revisões posteriores corrigiram a reproteção no `exp`, adicionaram mutação e modernizaram/simplificaram/corrigiram somente a apresentação Angular, sem mudar contrato ou regra de negócio. |
| Testes automatizados e evidências observadas | 111 integrações backend, 68 testes frontend, três jornadas E2E — com as quatro telas em 320 px e autenticação completa em landscape curto —, inspeção visual desktop, `320×568`, `360×800` e `667×375`, baseline Stryker limpa de 97,41%, OpenAPI, actionlint, restart/persistência e smoke aprovados somente com Docker. |
| README de execução e validação na raiz | Concluído e confrontado com os comandos observados em M6. |
| Atualização de estados da matriz e do plano | Concluída para M6, revisões, mutation testing, refinamento visual e correção responsiva local; CI hospedada, publicação e confirmação da explicação humana permanecem Pending. |
| ADRs adicionais | Criar somente se surgir nova decisão relevante. |
