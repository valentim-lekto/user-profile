# Uso de IA

A IA será usada como apoio, nunca como autoridade final. O responsável deve revisar, compreender e conseguir explicar todas as decisões e todo código produzido com seu auxílio.

## Uso por fase

| Fase | Apoio da IA | Verificação obrigatória |
|---|---|---|
| Requisitos | Identificar ambiguidades, organizar requisitos e propor critérios de aceite. | Comparar com `00-challenge.md` e separar requisitos originais, premissas e fora de escopo. |
| Design | Analisar alternativas e apoiar o design técnico, contratos e modelo de dados. | Validar simplicidade, segurança e coerência; registrar decisões relevantes e suas consequências. |
| Implementação | Apoiar uma etapa planejada por vez com código e refatorações. | Revisar o diff, relacionar a critérios de aceite e compreender integralmente o resultado. |
| Testes | Derivar cenários positivos, negativos e estados a partir dos critérios de aceite. | Revisar e executar os testes sem removê-los, desativá-los ou enfraquecê-los. |
| Revisão | Comparar especificação, implementação, testes e documentação. | Executar build e testes, revisar o diff e verificar segurança, segredos e rastreabilidade. |

## Responsabilidade humana

- A IA não aprova mudanças nem substitui validação observável.
- Toda sugestão deve ser confrontada com a especificação e com o comportamento executado.
- O responsável decide o que aceitar, corrige erros e assume autoria pelas entregas.
- Código ou decisão que não possa ser explicado não está pronto para integração.

## Registro

Versionar somente sínteses úteis do uso de IA: objetivo, fase, documentos usados como entrada, decisões influenciadas, artefatos alterados, validações executadas e limitações relevantes.

Não armazenar conversas completas nem transcrições integrais de prompts e respostas. Nunca incluir segredos, senhas, tokens, credenciais reais ou dados pessoais desnecessários nesses registros.

## Registros resumidos

### 2026-08-24 — Design técnico e planejamento

- **Objetivo:** transformar requisitos e arquitetura aprovada em design, OpenAPI, estratégia de testes, plano executável, rastreabilidade e ADRs, sem implementar código.
- **Entradas:** `AGENTS.md`, `PLANS.md`, `00-challenge.md`, `01-requirements.md`, este documento e as decisões fornecidas para a etapa.
- **Apoio da IA:** auditoria de cobertura, comparação de alternativas simples, proposta de contratos/status, catálogo de testes, matriz de rastreabilidade e revisão de riscos.
- **Pesquisa:** versões e tags verificadas em documentação oficial do .NET/EF Core, Angular, Node.js e manifestos oficiais de imagens Docker.
- **Decisões influenciadas:** fluxo direto por funcionalidades sem camadas genéricas; seis operações HTTP; JWT de 15 minutos; normalização única de email; chave aleatória somente em Development; origem única por Nginx; integração backend como base da suíte.
- **Artefatos:** `02-technical-design.md`, `03-api-contract.yaml`, `04-test-strategy.md`, `05-execution-plan.md`, `06-traceability.md` e ADR-0001 a ADR-0004.
- **Verificação humana obrigatória:** comparar com requisitos, validar OpenAPI, revisar o diff, confirmar que tags são específicas e explicar consequências de SQLite, JWT/sessionStorage, migrations no startup e proxy same-origin.
- **Limitação:** nenhum comportamento foi executado nesta etapa; builds, testes da aplicação e Docker permanecem pendentes porque não há código.

### 2026-08-24 — Revisão independente do design

- **Objetivo:** verificar o commit `b184432` diretamente contra challenge, requisitos, Definition of Done, contrato, arquitetura, segurança, testes e rastreabilidade antes de iniciar M1.
- **Entradas:** repositório limpo, histórico/diff completo, todos os artefatos SDD e fontes oficiais das versões e das semânticas HTTP/Nginx.
- **Apoio da IA:** leituras independentes em paralelo de contrato/segurança, rastreabilidade/testes e arquitetura/operação; consolidação baseada em evidência direta.
- **Resultado:** nenhum High; 15 achados Medium e 6 Low confirmados e corrigidos; candidatos não confirmados e riscos aceitos registrados separadamente.
- **Decisões influenciadas:** contrato `503 ProblemDetails` no proxy, `400` genérico no login e challenge Bearer obrigatório somente em recursos protegidos, health check Compose pelo `web`, scaffold/locks executáveis, allowlist do interceptor, validação fechada da chave, separação dos gates M3/M4, provas negativas sem mutação, critérios de stack e remoção de timestamps sem consumidor.
- **Artefatos:** requisitos, design, OpenAPI, estratégia, plano, matriz, ADR-0003/0004, índice SDD e `review-log.md`.
- **Validação:** parse/lint OpenAPI, checagem de IDs/links/rastreabilidade, pesquisa de segredos, revisão do diff e `git diff --check`; resultados detalhados em `review-log.md`.
- **Limitação:** a revisão comprova a coerência documental; código, builds, testes funcionais, E2E e Compose ainda não existem e continuam pendentes nos milestones.
