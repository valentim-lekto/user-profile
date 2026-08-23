# Artefatos SDD

Este diretório reúne os artefatos versionados do processo Spec-Driven Development. Consulte este índice e os documentos aplicáveis antes de alterar comportamento.

## Ordem de leitura

1. [`00-challenge.md`](00-challenge.md) — fonte estruturada do enunciado e das premissas recebidas.
2. [`01-requirements.md`](01-requirements.md) — requisitos normalizados e critérios de aceite estáveis.
3. Artefatos de design, contratos, dados, plano e testes aplicáveis à mudança.
4. [`ai-usage.md`](ai-usage.md) — regras de uso e registro de IA ao longo do trabalho.

## Estado atual

| Artefato | Finalidade | Estado |
|---|---|---|
| [`00-challenge.md`](00-challenge.md) | Preservar o enunciado, com requisitos originais e decisões aprovadas claramente separados. | Concluído nesta etapa. |
| [`01-requirements.md`](01-requirements.md) | Definir escopo, atores, casos de uso, requisitos, critérios, premissas, exclusões e Definition of Done. | Concluído nesta etapa. |
| [`ai-usage.md`](ai-usage.md) | Definir o uso responsável e verificável de IA. | Concluído nesta etapa. |

## Governança

- [`AGENTS.md`](../../AGENTS.md) contém as regras de execução e qualidade.
- [`PLANS.md`](../../PLANS.md) define somente o formato dos planos executáveis.
- IDs de requisitos, critérios e premissas publicados não devem ser reutilizados com outro significado.
- Planos, implementação e testes devem referenciar diretamente os critérios atendidos.

## Artefatos ainda pendentes

| Artefato | Estado |
|---|---|
| Design técnico | Pendente para a etapa de design. |
| Contratos de API | Pendente para a etapa de design. |
| Modelo de dados | Pendente para a etapa de design. |
| Plano de implementação executável | Pendente; `PLANS.md` contém apenas seu formato. |
| Estratégia de testes | Pendente para a etapa de planejamento. |
| README de execução e validação na raiz | Pendente para a etapa de implementação. |
| ADRs | Criar quando surgir uma decisão relevante que ainda não esteja registrada. |
