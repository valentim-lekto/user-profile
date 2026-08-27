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

### 2026-08-25 — Implementação e validação de M1

- **Objetivo:** entregar somente o walking skeleton executável, persistência SQLite, shell Angular e origem única Docker previstos em M1, sem antecipar cadastro, login, JWT ou perfil.
- **Entradas:** `AGENTS.md`, documentos `00`–`06`, contrato OpenAPI, ADRs e instruções explícitas da etapa.
- **Apoio da IA:** scaffold proporcional, implementação direta de configuração/health/migration, derivação dos testes mínimos, configuração do shell/Material/Nginx, auditorias independentes de escopo e execução dos gates observáveis.
- **Decisões influenciadas:** manter somente `Program`/Controllers/EF Core concretos; health consultar a tabela real de migrations; testar indisponibilidade com lock exclusivo; usar um único healthcheck no `web`; preservar erros da API e converter apenas `502/504` do Nginx; tornar `strict`/`strictTemplates` explícitos; restringir o ignore de `/data/` para não ocultar `Data/` em macOS.
- **Segurança de dependências:** o build revelou `GHSA-5xrq-8626-4rwp` em Vitest `4.0.8`; foi aplicado o patch `4.1.11`, o lock foi regenerado com Node `24.19.0`/npm `11.17.0` e `npm audit` terminou com 0 vulnerabilidades.
- **Artefatos:** solution e projetos .NET, entidade/DbContext/migration, testes de integração, workspace Angular, Dockerfiles, Nginx, `compose.yaml`, `.env.example`, validador OpenAPI e atualizações SDD.
- **Validação:** restore locked e build backend sem warnings; 6/6 integrações, incluindo schema Swagger obrigatório e ausência de drift EF; lint, 2/2 testes e build Angular; OpenAPI válido; Compose saudável sem `.env`; smokes de SPA/health/Swagger; `404` preservado; upstream parado convertido em `503 ProblemDetails`; API sem porta pública; cleanup sem remover o volume.
- **Tratamento do ambiente:** a VM Docker compartilhada ficou sem espaço em dois startups. Foram removidos somente caches/uma imagem produzidos nesta execução e volumes vazios deste projeto; recursos e dados de outros projetos não foram alterados. O teardown final preservou o volume recriado com o schema correto.
- **Limitação:** M2–M6 continuam pendentes. O shell não contém telas funcionais, a API expõe apenas `/health` e não há autenticação, jornadas E2E ou CI nesta etapa.
- **Auditoria final original:** a auditoria de M1 reintroduziu `CreatedAtUtc`/`UpdatedAtUtc` e ampliou a prova do schema; a revisão independente posterior preservou os campos, mas corrigiu a alegação de que vinham do challenge, formalizando-os como decisão interna do ADR-0002.

### 2026-08-25 — Revisão independente de M1

- **Objetivo:** auditar diretamente `8db5592` contra o challenge, SDD, código, testes e runtime, corrigir todos os High/Medium solucionáveis, explicitar bloqueios externos e registrar evidência reproduzível antes do commit local.
- **Entradas:** raiz/status/histórico, diff completo `b7de2fc..8db5592`, todos os SDD/ADRs, backend/frontend, locks, Docker/Compose/Nginx e resultados executáveis.
- **Apoio da IA:** três leituras independentes de backend, frontend/operação e SDD/rastreabilidade; consolidação por evidência e aplicação de correções mínimas.
- **Resultado:** 1 High, 12 Medium e 11 Low confirmados; o High e 11 Medium foram corrigidos, 1 Medium operacional foi explicitamente bloqueado pelo espaço da VM, oito Low triviais foram corrigidos e três Low adiados para M5 com justificativa em `review-log.md`.
- **Correções influenciadas:** descoberta real dos testes .NET; asserts do schema e OpenAPI runtime; timeout localizado do health; validador normativo/negativo; smoke Compose; contextos Docker sem arquivos sensíveis; restauração do histórico e rastreabilidade executável.
- **Validação:** SDK/Node exatos; restore locked; build sem warnings; 6/6 integrações e 2/2 frontend; lint/build; 0 vulnerabilidades npm; contrato positivo/negativo; smoke same-origin/`503`; revisão do diff e checks documentais.
- **Limitação ambiental:** a VM Docker estava sem espaço; o Compose base foi validado e o smoke final rodou em projeto isolado com o volume nomeado apoiado em diretório temporário do host. A revalidação exata do volume padrão permanece bloqueada; artefatos temporários foram removidos e o volume principal não foi alterado.
- **Simplicidade:** correções diretas em metadata, asserts e dois scripts; migrations, health transitivo, multi-stage e Nginx foram mantidos somente por corresponderem a gates explícitos de M1.

### 2026-08-25 — Implementação e validação original de M2

- **Objetivo:** entregar somente o cadastro vertical Angular → API → SQLite, com validações, hash, unicidade, feedback acessível e evidência real, sem antecipar login, JWT, guard, interceptor ou perfil.
- **Entradas:** `AGENTS.md`, requisitos, design, OpenAPI, estratégia de testes, plano, matriz e instruções explícitas de M2.
- **Apoio da IA:** propostas independentes de backend/frontend, auditoria preventiva de riscos, implementação paralela das duas fatias, revisão integrada, derivação dos casos negativos/concorrentes e execução dos gates.
- **Decisões influenciadas:** limites defensivos `200/320/128`; política de email comum e explícita nas duas camadas; JSON normativo `passwordConfirmation`; `Trim` somente em nome/email; fluxo direto `AuthController` → EF Core/`PasswordHasher<User>`; precheck amigável com índice único autoritativo; barreira `SaveChangesInterceptor` somente nos testes; um service Angular com signals e placeholder mínimo de login.
- **Correção orientada por teste:** a primeira suíte frontend mostrou que um erro remoto era aplicado ao controle, mas não aparecia no `mat-error` porque o campo permanecia intocado. O controle passou a ser marcado como tocado; a suíte completa então aprovou 12/12 testes.
- **Auditoria final original:** uma leitura independente encontrou divergência potencial entre os validadores de email e metadata incompleta das senhas no Swagger runtime. As duas camadas passaram a compartilhar a mesma política explícita, com regressão no limite aceito de 320 caracteres; um schema filter restrito a `RegisterRequest` e asserts runtime passaram a comprovar `format: password` e `writeOnly: true`.
- **Validação original:** antes da revisão independente descrita abaixo, o OpenAPI normativo foi aprovado; restore/build .NET registrou 29/29 integrações; Node/npm registrou lint, 12/12 testes e build; Compose padrão registrou smokes `200/201/400/409`, persistência e inspeção da UI. Esses resultados são baseline histórica e não aprovam as correções posteriores.
- **Ambiente:** como o host expunha outro patch do SDK e o `node_modules` ficou incompleto após uma execução interrompida, restores/builds/testes foram repetidos nas imagens já fixadas pelo design. Somente dependências derivadas e caches temporárias desta execução foram substituídas/removidas; nenhum lock, versão ou recurso de outro projeto foi alterado.
- **Simplicidade e escopo:** não foram criados repository, service de domínio, transação serializável ou hook de produção. A complexidade mantida limita-se ao catch específico da corrida SQLite e à barreira determinística exclusiva da infraestrutura de teste. M3–M6 continuam pendentes.

### 2026-08-25 — Revisão independente e correções de M2

- **Objetivo:** auditar o commit `c02b67f` contra seu pai, o challenge, todos os artefatos SDD, o diff, a implementação e as evidências, e corrigir divergências documentais/comportamentais sem antecipar M3.
- **Entradas:** `AGENTS.md`, histórico/diff, documentos `00`–`06`, OpenAPI, ADRs, backend/frontend, testes e artefatos Docker/Nginx.
- **Apoio da IA:** leituras independentes por eixos de especificação/rastreabilidade, backend/contrato e frontend/operação; consolidação por evidência e proposta das correções mínimas. Achados, decisões e comandos detalhados pertencem a [`review-log.md`](review-log.md), sem duplicação neste registro.
- **Decisões influenciadas:** formalizar `PREM-INPUT-01` como refinamento interno — limites `200/320/128` e 1 MiB não vieram do challenge —; adotar email ASCII e semântica OpenAPI pós-`Trim`; rejeitar nomes JSON com caixa incorreta; declarar `413/415 ProblemDetails`; restringir logs para não registrar query/body/header; tornar `scripts/validate-m1-compose.sh` o smoke versionado acumulado M1+M2.
- **Rastreabilidade de segurança:** `BE-ERR-002` evidencia somente que a resposta de erro não expõe stack, SQL ou segredo; a prova de que logs omitem os marcadores sintéticos pertence a `OPS-SECRET-001`.
- **Validação:** imagem .NET `10.0.400` com restore locked, build sem warnings/erros e 36/36 integrações; imagem Node `24.19.0` com 0 vulnerabilidades, lint, 13/13 testes e build de 494,59 kB; OpenAPI normativo com seis operações/52 referências e runtime aprovado; smoke acumulado com `201/400/409/413/415`, persistência, ausência de marcadores nos logs e cleanup integral dos recursos efêmeros.
- **Resultado, simplicidade e limitação:** 1 High, 10 Medium e 5 Low foram corrigidos. A política ASCII substituiu a necessidade de case-fold Unicode; a complexidade mantida limita-se às extensões pós-`Trim`, aos mapeamentos `413/415` e ao smoke exigidos por contrato/operação. M3–M6 permanecem pendentes.

### 2026-08-26 — Implementação, auditorias e validação de M3

- **Objetivo:** entregar somente login, JWT Bearer, sessão, proteção de rotas/endpoints e dashboard, sem antecipar edição de perfil/senha, refresh token, E2E completo ou CI.
- **Entradas:** `AGENTS.md`, requisitos, design técnico, OpenAPI, estratégia de testes, plano, matriz, ADRs, implementação M1/M2 e instruções explícitas de M3.
- **Apoio da IA:** implementação e leituras independentes de backend, frontend e risco de segurança; revisão do contrato antes do código; derivação de cenários de autenticação/configuração; execução dos gates e inspeção da UI real.
- **Decisões influenciadas:** fluxo proporcional `Controllers` → EF Core/`JwtTokenIssuer`; chave Base64 validada com falha fechada fora de Development e fallback aleatório apenas em Development; token de 15 minutos e claims mínimas; resposta `401` byte-idêntica para credenciais não reconhecidas; busca exclusiva pelo `sub`; allowlist funcional para o interceptor e guard que devolve `UrlTree`.
- **Correções de auditoria:** o Swagger runtime passou a declarar challenge Bearer em respostas `401`; a validação cobriu a borda do clock skew e configuração inválida; a suíte frontend passou a provar erros locais/remotos, duplo submit, preservação de chave não relacionada e limpeza da sessão em `401`/logout.
- **Simplicidade (KISS):** foram mantidos `Controllers` → EF Core/`JwtTokenIssuer` e `AuthService` com signals, sem refresh, NgRx, repository, facade ou camada adicional. A complexidade ficou restrita à validação JWT/configuração, dummy hash para reduzir sinal de timing, allowlist do interceptor e lazy routes, pois são requisitos de segurança ou de orçamento da fatia.
- **Artefatos:** código e testes de `Auth`/`Profile`, configuração JWT/Compose, smoke acumulado M1+M2+M3, OpenAPI e os documentos de execução/rastreabilidade deste diretório.
- **Validação:** imagem .NET `10.0.400` com restore locked, build com warnings como erros e 69/69 integrações sem falhas/skips; imagem Node `24.19.0` com 494 pacotes, 0 vulnerabilidades, lint, 42/42 testes e build sem warnings (317,28 kB bruto/87,60 kB estimado); OpenAPI normativo com seis operações/53 referências; `docker compose config`; smoke isolado cobrindo origem única, registro/login, `401` equivalente/Bearer, perfil por `sub`, `413/415`, ausência dos marcadores e do primeiro JWT nos logs, recriação da API com novo token/persistência, `503` e cleanup; UI real em `http://localhost:8080` sem warnings/erros de console.
- **Ambiente e limitação:** o host não tinha os patches exatos de SDK/Node, portanto a validação usou as imagens fixadas, sem mudar locks. O Compose padrão foi encerrado sem apagar o volume. M4–M6 permanecem pendentes, inclusive edição de perfil/senha, jornadas E2E completas, CI, README raiz e validação final; nenhum push foi realizado.

### 2026-08-26 — Revisão independente e correções de M3

- **Objetivo:** auditar o commit `b1f2468` sem presumir correção, registrar achados e corrigir os riscos High/Medium e Low triviais antes de iniciar M4.
- **Entradas:** histórico/diff integral, SDD/ADRs, implementação/testes, Swagger runtime, smoke Compose e UI real.
- **Apoio da IA:** leituras independentes por backend/segurança, frontend e SDD/operação; reprodução de concorrência de sessão, inspeção estrutural do OpenAPI servido e re-revisão do patch corretivo. Achados e comandos completos estão em [`review-log.md`](review-log.md).
- **Decisões influenciadas:** isolar `ProfileService` por ativação do dashboard; condicionar a limpeza de `401` ao mesmo token; completar metadados do Swagger com filtro focado; provar o wiring real do Angular e procurar a senha sintética bem-sucedida nos logs.
- **Simplicidade (KISS):** provider local e igualdade do token substituíram store global, generation tracker e infraestrutura de cancelamento; não foram criadas novas camadas de aplicação.
- **Resultado:** 1 High, 5 Medium e 4 Low confirmados e corrigidos; 69/69 integrações backend, 45/45 testes frontend, 0 vulnerabilidades, lint/build, OpenAPI normativo/runtime, Compose/smoke e fluxo real no navegador aprovados. Nenhum push foi realizado e M4–M6 permanecem pendentes.

### 2026-08-26 — Implementação e validação de M4

- **Objetivo:** entregar somente visualização/edição dos dados cadastrais e alteração de senha, preservando os contratos e as decisões de autenticação de M3, sem antecipar E2E completo, CI ou a documentação final.
- **Entradas:** `AGENTS.md`, requisitos, design técnico, OpenAPI, estratégia de testes, plano, matriz, ADRs, implementação M1–M3 e instruções explícitas de M4.
- **Apoio da IA:** revisão da especificação antes do código, implementação e leituras independentes de backend/frontend/segurança, derivação dos cenários atômicos e de isolamento por `sub`, execução dos gates em imagens fixadas, smoke acumulado e inspeção da UI real.
- **Decisões influenciadas:** manter os dois PUTs separados; validar integralmente antes da mutação; reutilizar normalização, unicidade e `PasswordHasher<User>`; encerrar apenas a sessão corrente no frontend após sucesso; forçar nova consulta no dashboard em cada ativação.
- **Simplicidade (KISS):** o backend preserva o fluxo direto `Controller` → EF Core/`PasswordHasher<User>` e o frontend `component` → service/signals, sem repository, facade, NgRx ou camada adicional. A complexidade mantida restringe-se à atomicidade, à corrida do índice único, à segurança por `sub`/sessão e aos estados observáveis exigidos pela fatia.
- **Artefatos:** implementação e testes dos dois PUTs e dos dois formulários de perfil, atualização do smoke acumulado M1+M2+M3+M4, OpenAPI e documentos de execução/rastreabilidade deste diretório.
- **Validação:** imagem .NET `10.0.400` com restore locked, build sem warnings e 99/99 integrações sem falhas/skips; imagem Node `24.19.0` com `npm ci`, 0 vulnerabilidades, lint, 55/55 testes sem skips e build sem warnings (317,36 kB bruto/87,67 kB estimado); OpenAPI normativo com seis operações/53 referências e Swagger runtime aprovado; `docker compose config`; smoke completo dos PUTs, atomicidade, senha antiga/nova, persistência, logs seguros, `413/415/503` e cleanup efêmero.
- **UI e ambiente:** em `http://localhost:8080` foram observados login, dashboard, perfil, atualização de dados e o novo nome após retornar ao dashboard, além da validação acessível de confirmação divergente e console limpo. A submissão final da troca de senha não foi feita pelo navegador, mas foi coberta por integração, frontend e smoke automatizado. Como SDK/Node do host divergiam dos patches fixados, os gates usaram as imagens versionadas; a stack padrão foi encerrada sem `-v` e preservou o volume.
- **Limitação e entrega:** M5/M6, jornadas E2E completas, CI e documentação final continuam pendentes. Nenhum segredo real foi usado, nenhum push foi realizado e apenas M4 foi concluído nesta etapa.

### 2026-08-26 — Revisão independente e correções de M4

- **Objetivo:** auditar o commit `7803b3d` contra seu pai sem presumir correção, registrar os achados e encerrar todos os High/Medium e Low triviais antes de M5.
- **Entradas:** histórico/diff integral, todos os artefatos SDD/ADRs, implementação/testes backend e frontend, OpenAPI normativo/runtime e configuração/smoke Docker.
- **Apoio da IA:** leituras independentes por frontend, backend/segurança e SDD/operação; consolidação por evidência; criação dos testes vermelhos de concorrência, interação DOM, contrato runtime e bordas; reexecução dos gates completos. Os achados e comandos detalhados estão em [`review-log.md`](review-log.md).
- **Decisões influenciadas:** vincular o sucesso da senha ao token iniciador; desabilitar somente o `FormGroup` em curso; provar os formulários renderizados em vez de chamar apenas métodos do componente; tornar explícitos os bodies obrigatórios e os `$ref`/media types dos PUTs no Swagger runtime; completar as bordas 320/128.
- **Simplicidade (KISS):** a igualdade de token já existente substituiu store/generation tracker, e `disable`/`enable` em `try/finally` substituiu versionamento de formulário ou cancelamento global. No backend, dois atributos MVC e helpers de assert focados bastaram; nenhuma camada, dependência ou abstração de produção foi adicionada.
- **Validação:** imagem .NET `10.0.400` com restore locked, build sem warnings/erros e 101/101 integrações sem falhas/skips; imagem Node `24.19.0` com `npm ci`, 0 vulnerabilidades, lint, 56/56 testes sem skips e build de 317,36 kB bruto/87,58 kB estimado; OpenAPI normativo com seis operações/53 referências; configuração Compose e smoke acumulado aprovados.
- **Resultado e limitação:** 0 High, 4 Medium e 1 Low foram corrigidos, sem achado aberto. A UI cadastral real permanece evidência da implementação original, enquanto esta revisão acrescentou DOM automatizado e smoke; E2E completo, CI e acabamento continuam em M5/M6. Nenhum push foi realizado.

### 2026-08-26 — Implementação, auditoria e validação de M5

- **Objetivo:** entregar somente qualidade acumulada, três jornadas E2E, execução Docker sem SDK local, CI e acabamento visual, sem endpoint, DTO ou regra de negócio nova.
- **Entradas:** `AGENTS.md`, estratégia de testes, plano, matriz, design técnico, contrato, implementação/testes M1–M4 e instruções explícitas de M5.
- **Apoio da IA:** auditorias independentes de cobertura backend, frontend/acessibilidade e segurança/CI; implementação dos targets/profiles Docker, Playwright e workflow; inspeção da interface real com navegador; execução e correção iterativa dos gates observáveis.
- **Decisões influenciadas:** manter exatamente três jornadas sem seed, retry ou preparação via API; usar projeto/volume/artefato por execução da suíte e contexto/dados por jornada; reter screenshot/trace/logs apenas em falha; executar o mesmo conjunto por profiles no host e na CI; ampliar o timeout de resposta do Nginx para 30 segundos após observar contenção real no primeiro hash, preservando connect timeout de 2 segundos e a prova de `503`.
- **Segurança:** a primeira abordagem evitava `fill`, mas ainda serializava a senha sintética no argumento de `locator.evaluate`. A reauditoria reproduziu o vazamento em trace; a versão final gera e mantém as senhas exclusivamente no contexto do navegador, passa somente chaves não sensíveis, limpa inputs no `finally` e teve traces forçados/report/JUnit varridos com 0 senha, JWT ou Bearer. O smoke persiste diagnóstico filtrado antes do teardown; cada nome de projeto recebe sufixo próprio e somente seus recursos são encerrados.
- **Simplicidade (KISS):** o backend não recebeu alteração; a cobertura existente foi auditada e reutilizada. No frontend, o acabamento ficou nos componentes/SCSS Material existentes. A complexidade nova limita-se aos quatro profiles, um script E2E direto e um workflow único; não houve Page Object, seed manager, retry, store global ou framework adicional.
- **Artefatos:** targets de teste nos Dockerfiles, profiles no `compose.yaml`, três specs Playwright e instruções E2E, workflow GitHub Actions, ajustes semânticos/responsivos das quatro telas, extensão mínima dos testes frontend e atualização do design/estratégia/plano/matriz/índice.
- **Validação:** profile backend com 101/101 integrações; profile frontend com lint, 57/57 testes e build; OpenAPI com seis operações/53 referências; Compose config; actionlint; smoke acumulado; três jornadas E2E reais; inspeção desktop/360 px, teclado, foco, `aria-live` e overflow; `git diff --check` e revisão de segurança/escopo. A execução hospedada da CI depende do futuro push e por isso foi validada estaticamente e pelos mesmos comandos locais.
- **Resultado e limitação:** M5 concluído sem nova funcionalidade de negócio e sem achado de segurança aberto. Uma repetição redundante do smoke, após mudar somente o trap de diagnóstico, encontrou contenção externa comprovada por thread-pool starvation e por um contêiner PostgreSQL de outro projeto próximo de 98% de CPU; o smoke completo anterior já estava verde e a falha controlada comprovou o novo artefato/teardown, sem tocar no projeto externo. M6 permanece responsável pelo README raiz, checkout limpo, walkthrough humano e revalidação/documentação finais. Nenhum segredo real foi usado e nenhum push foi realizado.

### 2026-08-26 — Revisão independente de M5

- **Objetivo:** auditar o commit `eaad3cd` contra seu pai, challenge, SDD/ADRs, código, testes, Docker/Compose e CI; corrigir todos os High/Medium e Low triviais sem antecipar M6.
- **Entradas:** worktree inicialmente limpa, diff integral de 41 arquivos, todos os documentos SDD, implementação frontend/backend, três jornadas, scripts, workflow e evidências executáveis reais.
- **Apoio da IA:** leituras independentes de frontend/acessibilidade, segurança/CI e rastreabilidade; consolidação por evidência; re-revisão do patch corretivo antes do fechamento.
- **Decisões influenciadas:** reforçar as jornadas existentes por labels/roles e reproteção, sem criar quarta jornada ou Page Object; comparar inventários literais; impor a allowlist nativa do npm; remover o collector sem consumidor; usar filtro shell compartilhado e um helper mínimo para precedência de exit status; fixar Actions por SHA.
- **Correções:** persistência de nome/email e sessão encerrada passaram a ter prova E2E; assets inexistentes com extensão usam `404`; todos os perfis/estágios/imagens são validados; diagnósticos e cleanup registram projetos e preservam somente saída filtrada; `REV-M1-015`–`017` foram encerrados; timeout e isolamento foram descritos com precisão.
- **Validação:** npm estrito rejeitou primeiro dois scripts não decididos e depois aprovou as negações explícitas; frontend 57/57, backend 101/101, contrato 6 operações/53 referências, `actionlint`, smoke final, E2E 3/3, probe do sanitizador, varredura de artefatos e inspeção de cleanup passaram.
- **Resultado:** 0 High, 8 Medium e 11 Low corrigidos, sem achado aberto; seis Low foram encontrados ao re-revisar o próprio patch corretivo e também foram encerrados. A contenção observada na baseline foi rejeitada como falha do repositório depois de execuções seriais verdes. M6 e a execução hospedada continuam fora desta revisão; nenhum push foi realizado.
