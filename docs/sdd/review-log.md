# Registro de revisões independentes

## 2026-08-24 — Design técnico e planejamento

- **Etapa revisada:** baseline de design, contrato, estratégia de testes, plano M1–M6, rastreabilidade e ADRs.
- **Commit revisado:** `b184432ff85f65cc0bbad19a97765d08a711a0da` (`be2019e` como base).
- **Natureza da etapa:** somente documentação; não existiam código, dependências, Dockerfiles ou testes executáveis da aplicação.
- **Arquivos examinados:** `AGENTS.md`, `PLANS.md`, todos os arquivos em `docs/sdd/`, o histórico recente e o diff integral do commit.
- **Critérios examinados:** `AC-REG-*`, `AC-LOGIN-*`, `AC-DASH-*`, `AC-PROF-*`, `AC-PASS-*`, `UI-STATE-01`, `API-ERROR-01`, `SEC-*`, `OPS-*`, `DOC-*`, `TEST-FLOW-01`, `AI-*`, `DEL-REPO-01` e requisitos/premissas associados.

### Achados confirmados e decisões

As localizações abaixo referem-se ao commit revisado, antes das correções.

Foram confirmados 0 achados High, 15 Medium e 6 Low.

#### `REV-DESIGN-001` — Medium — falha do upstream fora do contrato

- **Localização:** `02-technical-design.md:34,168-174,220-228`, `03-api-contract.yaml` nas respostas das operações e `04-test-strategy.md:95-97`.
- **Evidência:** `E2E-003` parava a API, mas o Nginx não tinha mapeamento; seu comportamento padrão produziria `502` HTML enquanto `API-ERROR-01` exige ProblemDetails.
- **Impacto:** o fluxo explicitamente testado poderia divergir do OpenAPI e quebrar o tratamento uniforme do frontend.
- **Correção mínima:** mapear somente `502/504` de transporte para `503 application/problem+json`, declarar `503` nas operações, provar `502` em runtime e inspecionar explicitamente o mapeamento de ambos os códigos; distinguir o `503` do health/transportes do `500` inesperado dentro de operações.
- **Critérios:** `API-ERROR-01`, `AC-DASH-04`, `OPS-DOCKER-01`, `OPS-DOCKER-02`.
- **Decisão:** corrigido no design, ADR-0004, OpenAPI, estratégia, plano e matriz.

#### `REV-DESIGN-002` — Medium — gate M3 dependia de PUT criado em M4

- **Localização:** `04-test-strategy.md:57,129`, `05-execution-plan.md:81-83,98-102`.
- **Evidência:** `BE-PROF-002` misturava consulta e alteração, mas era obrigatório em M3, quando somente `GET /api/profile` existe.
- **Impacto:** M3 só poderia fechar antecipando M4, ignorando o teste ou reduzindo sua cobertura.
- **Correção mínima:** manter GET/isolamento em `BE-PROF-002` e mover PUT/overposting para novo teste de M4.
- **Critérios:** `SEC-AUTH-01`, `AC-PROF-01`, `AC-PROF-02`, `DOC-TRACE-01`.
- **Decisão:** corrigido com `BE-PROF-006` e gates/matriz atualizados.

#### `REV-DESIGN-003` — Medium — senha sem prova específica de autorização

- **Localização:** `04-test-strategy.md:54-63,130`, `06-traceability.md:25,70`.
- **Evidência:** os testes genéricos de autenticação terminavam em M3; os `BE-PASS-*` de M4 não provavam Bearer, isolamento entre dois usuários, `sub` ou rejeição de `userId`.
- **Impacto:** endpoint anônimo ou IDOR na troca de senha poderia passar pela suíte planejada.
- **Correção mínima:** adicionar teste HTTP do endpoint de senha para Bearer e dois usuários resolvidos exclusivamente por `sub`.
- **Critérios:** `FR-AUTH-01`, `FR-AUTH-02`, `AC-DASH-02`, `SEC-AUTH-01`.
- **Decisão:** corrigido com `BE-PASS-004` e rastreabilidade explícita em M4.

#### `REV-DESIGN-004` — Medium — testes negativos não provavam ausência de mutação

- **Localização:** `04-test-strategy.md:48,62`.
- **Evidência:** cadastro inválido e nova senha inválida verificavam apenas `400`; uma implementação poderia persistir/trocar dados antes de responder erro.
- **Impacto:** critérios negativos poderiam passar sem proteger dados.
- **Correção mínima:** afirmar ausência de usuário e preservação do hash/senha, cobrindo também campos obrigatórios ausentes.
- **Critérios:** `AC-REG-02`–`AC-REG-04`, `AC-PASS-03`, `API-ERROR-01`.
- **Decisão:** corrigido em `BE-REG-002` e `BE-PASS-002`.

#### `REV-DESIGN-005` — Medium — `.env.example` sem entrega nem gate

- **Localização:** `01-requirements.md:76`, `02-technical-design.md:203`, `05-execution-plan.md:78-83,134-141`, `06-traceability.md:46`.
- **Evidência:** o design prometia o arquivo, mas nenhum milestone o criava e nenhum check verificava seu conteúdo.
- **Impacto:** M6 poderia declarar configuração/documentação concluída omitindo um artefato aplicável.
- **Correção mínima:** criar o exemplo opcional em M3 e auditá-lo sem exigir sua cópia para iniciar o Compose.
- **Critérios:** `NFR-CONFIG-01`, `SEC-SECRET-01`, `DOC-RUN-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido no plano, estratégia e matriz.

#### `REV-DESIGN-006` — Medium — gate de ProblemDetails em M1 sem prova runtime

- **Localização:** `04-test-strategy.md:65,68,127-128`, `05-execution-plan.md:43-46,64`.
- **Evidência:** M1 exigia pipeline de ProblemDetails, mas `BE-ERR-*` começava em M2 e o health test não afirmava media type/schema do `503`.
- **Impacto:** M1 poderia fechar com o formato de erro quebrado.
- **Correção mínima:** fazer `BE-HEALTH-001` provar `503 application/problem+json` e schema.
- **Critérios:** `FR-ERR-01`, `API-ERROR-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido no catálogo e no gate M1.

#### `REV-DESIGN-007` — Medium — conclusão sem validação observada versionada

- **Localização:** `05-execution-plan.md:152,158-193,226`, `ai-usage.md:24,38-39` e Definition of Done.
- **Evidência:** o plano marcava design concluído, mas registrava somente comandos futuros e expectativas da aplicação.
- **Impacto:** não havia evidência auditável de parse OpenAPI, IDs, links, diff ou matriz.
- **Correção mínima:** registrar esta revisão, comandos e resultados documentais e distinguir checks não aplicáveis por ausência de código.
- **Critérios:** `DOC-SDD-01`, `DOC-TRACE-01`, `AI-SDD-01`.
- **Decisão:** corrigido neste registro, no progresso do plano e no registro resumido de IA.

#### `REV-DESIGN-008` — Medium — tecnologias ligadas a critérios que não as comprovavam

- **Localização:** `01-requirements.md:71-72`, `06-traceability.md:41-42`.
- **Evidência:** ASP.NET Core, EF Core, JWT e Angular eram ligados a critérios sobre fluxos/UI/rastreabilidade, não sobre a stack.
- **Impacto:** a matriz poderia parecer completa sem provar tecnologias obrigatórias.
- **Correção mínima:** criar critérios de stack testáveis e checks de pacotes, locks e build.
- **Critérios:** `NFR-TECH-01`, `NFR-TECH-02`, `DOC-TRACE-01`.
- **Decisão:** corrigido com `TECH-BACKEND-01`, `TECH-FRONTEND-01` e testes `TECH-*`.

#### `REV-DESIGN-009` — Medium — configuração crítica da chave JWT sem gate

- **Localização:** `02-technical-design.md:185,197-203`, `04-test-strategy.md:51,120`.
- **Evidência:** a chave deveria ter ao menos 256 bits e ser obrigatória fora de Development, mas nenhum teste cobria ausência, formato ou tamanho.
- **Impacto:** startup com chave fraca ou fallback indevido poderia passar pelos gates.
- **Correção mínima:** definir formato Base64 de ao menos 32 bytes, falhar fechado e testar ambientes/configurações.
- **Critérios:** `SEC-AUTH-01`, `SEC-SECRET-01`, `NFR-CONFIG-01`.
- **Decisão:** corrigido no design, ADR-0003, `BE-CONFIG-001`, plano e matriz.

#### `REV-DESIGN-010` — Medium — interceptor sem allowlist verificável

- **Localização:** `02-technical-design.md:100`, `04-test-strategy.md:79`.
- **Evidência:** “chamadas protegidas” e “não login/cadastro” permitiam implementação por blacklist que anexasse Bearer a URL absoluta ou externa.
- **Impacto:** token poderia ser enviado a destino indevido.
- **Correção mínima:** allowlist das requisições relativas de perfil e teste negativo para health, URL absoluta e origem externa.
- **Critérios:** `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`.
- **Decisão:** corrigido no design, `FE-INT-001` e plano M3.

#### `REV-DESIGN-011` — Medium — semântica e challenge das respostas de autenticação

- **Localização:** `03-api-contract.yaml:535-562`, `02-technical-design.md:171-172`.
- **Evidência:** os componentes 401 tinham corpo, mas não declaravam `WWW-Authenticate`, obrigatório para 401 pelo HTTP; o login público, porém, não aceita Bearer e portanto não possui challenge Bearer aplicável.
- **Impacto:** manter `401` no login exigiria um challenge enganoso, enquanto os recursos protegidos poderiam produzir resposta HTTP incompleta.
- **Correção mínima:** usar `400 ValidationProblemDetails` genérico para credenciais inválidas; reservar `401` aos recursos protegidos e declarar seu `WWW-Authenticate: Bearer` como obrigatório no contrato.
- **Critérios:** `API-ERROR-01`, `AC-LOGIN-02`, `AC-DASH-02`.
- **Decisão:** corrigido no OpenAPI, design, `BE-LOGIN-002`, `BE-AUTH-001`, frontend, plano, matriz e `SPEC-OAS-005`.

#### `REV-DESIGN-012` — Medium — M1 exigia validação “em CI” antes de existir CI

- **Localização:** `05-execution-plan.md:46,117-119`.
- **Evidência:** validação do OpenAPI em CI era entrega M1, mas a criação do CI ocorre somente em M5.
- **Impacto:** gate impossível ou antecipação de milestone.
- **Correção mínima:** criar check automatizado local em M1 e conectá-lo ao CI em M5.
- **Critérios:** `DOC-SDD-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido no plano M1.

#### `REV-DESIGN-013` — Low — exemplos compartilhados de ProblemDetails incoerentes

- **Localização:** `03-api-contract.yaml:513-603`.
- **Evidência:** respostas reutilizadas fixavam `instance` de outra rota e combinavam `about:blank` explícito ou implícito com títulos específicos em vez das frases padrão do status.
- **Impacto:** documentação gerada mostraria ocorrência errada ou semântica ambígua.
- **Correção mínima:** remover `instance`/`type` dos exemplos reutilizados e usar as frases HTTP padrão em `title` enquanto o tipo continuar `about:blank` implícito.
- **Critérios:** `API-ERROR-01`.
- **Decisão:** corrigido no OpenAPI.

#### `REV-DESIGN-014` — Low — premissa Compose acumulava decisões alheias

- **Localização:** `04-test-strategy.md:119`, `06-traceability.md:75`.
- **Evidência:** `PREM-OPS-01` diz apenas “sem `.env`”, mas era usada também para tags e chave efêmera.
- **Impacto:** enfraquecia o significado estável do ID.
- **Correção mínima:** ligar tags à execução reproduzível/design e manter a premissa somente para ausência de `.env` obrigatório.
- **Critérios:** `DOC-TRACE-01`.
- **Decisão:** corrigido na estratégia e matriz.

#### `REV-DESIGN-015` — Low — `AI-EXPLAIN-01` não era observável

- **Localização:** `01-requirements.md:150`, `05-execution-plan.md:134-148`, `06-traceability.md:53`.
- **Evidência:** “consegue explicar” não tinha escopo, procedimento ou registro mínimo.
- **Impacto:** o critério final não poderia ser fechado de forma auditável.
- **Correção mínima:** walkthrough manual curto, cobrindo decisões e uma cadeia rastreada, com resultado resumido.
- **Critérios:** `AI-EXPLAIN-01`.
- **Decisão:** corrigido com `DOC-EXPLAIN-001` e gate M6.

#### `REV-DESIGN-016` — Low — timestamps sem requisito ou consumidor

- **Localização:** `02-technical-design.md:115-120`, `04-test-strategy.md:47,58`, `05-execution-plan.md:98`.
- **Evidência:** `CreatedAtUtc`/`UpdatedAtUtc` não eram retornados nem ligados a critério, mas adicionavam colunas, relógio e asserts.
- **Impacto:** estado e manutenção fora do escopo funcional.
- **Correção mínima:** remover os campos e suas provas planejadas.
- **Critérios:** `PREM-ARCH-03`, `DOC-TRACE-01`.
- **Decisão:** corrigido no modelo, testes, plano e matriz; nenhuma complexidade substituta foi adicionada. A decisão foi supersedida em M1 por `REV-M1-010`, que registra sua nova proveniência como decisão interna do ADR-0002 sem reescrever este histórico.

#### `REV-DESIGN-017` — Low — lock .NET não estava operacionalmente fechado

- **Localização:** `02-technical-design.md:38,43`, `05-execution-plan.md:41,160-180`.
- **Evidência:** havia promessa de SDK/pacotes/lockfiles exatos, mas sem `packages.lock.json`, locked mode ou `rollForward: disable` explícitos.
- **Impacto:** restore/CI poderia aceitar grafo ou SDK diferente sem mudança deliberada.
- **Correção mínima:** gerar uma vez os locks com `--use-lock-file`, revisá-los/versioná-los, usar `--locked-mode` nos restores posteriores e impedir roll-forward do SDK.
- **Critérios:** `TECH-BACKEND-01`, `OPS-DOCKER-02`.
- **Decisão:** corrigido no design, estratégia e plano.

#### `REV-DESIGN-018` — Low — duplo submit não era provado em todos os formulários

- **Localização:** `02-technical-design.md:216`, `04-test-strategy.md:76,83`.
- **Evidência:** o design prometia bloqueio para toda operação, mas login e edição de perfil verificavam apenas loading.
- **Impacto:** requests concorrentes e feedback em corrida poderiam escapar.
- **Correção mínima:** afirmar bloqueio de nova submissão nos testes focados existentes.
- **Critérios:** `UI-STATE-01`, `AC-LOGIN-03`, `AC-PROF-05`.
- **Decisão:** corrigido em `FE-LOGIN-001` e `FE-PROF-002`.

#### `REV-DESIGN-019` — Medium — check de tags fora do gate e da matriz

- **Localização:** `04-test-strategy.md:124,133`, `05-execution-plan.md:40-55`, `06-traceability.md:45`.
- **Evidência:** M1 entregava Dockerfiles e proibia `latest`, mas omitia `OPS-TAGS-001`; depois de separar `PREM-OPS-01`, nenhuma linha da matriz referenciava esse teste nominal.
- **Impacto:** M1 poderia fechar com tag flutuante e a cadeia `OPS-DOCKER-02` → teste ficava incompleta.
- **Correção mínima:** incluir `OPS-TAGS-001` em M1, repeti-lo em M5/M6 e ligá-lo a `NFR-OPS-02`.
- **Critérios:** `OPS-DOCKER-02`, `DOC-TRACE-01`.
- **Decisão:** corrigido nos gates e na matriz.

#### `REV-DESIGN-020` — Medium — health check do Compose sem executor definido

- **Localização:** `02-technical-design.md:224-230`, `04-test-strategy.md:121-133`, `05-execution-plan.md:40-55`.
- **Evidência:** o design exigia health, `depends_on` e `docker compose up --wait`, mas não definia qual contêiner executaria a probe; a imagem runtime ASP.NET fixada não promete `curl`/`wget`.
- **Impacto:** M1 poderia ficar sempre unhealthy após instalar a probe no contêiner errado ou fazer `--wait` provar somente processo em execução.
- **Correção mínima:** manter uma única probe no `web`, usando o BusyBox `wget` da imagem Alpine contra seu `/health`; depender da API como `service_started`, de forma que a probe atravesse Nginx, API e SQLite.
- **Critérios:** `OPS-DOCKER-01`, `OPS-DOCKER-02`, `API-ERROR-01`.
- **Decisão:** corrigido no design, estratégia e plano M1 sem adicionar dependência à imagem da API.

#### `REV-DESIGN-021` — Medium — scaffold não produzia a solution e os locks planejados

- **Localização:** `05-execution-plan.md:164-177`, `02-technical-design.md:62-84`.
- **Evidência:** no .NET 10, `dotnet new sln` cria `.slnx` por padrão, enquanto os comandos seguintes esperavam `UserProfile.sln`; os projetos também eram criados na raiz e não eram adicionados à solution antes do restore.
- **Impacto:** a sequência de M1 falharia pelo nome do arquivo ou restauraria uma solution vazia, sem gerar os locks dos projetos e divergindo da estrutura aprovada.
- **Correção mínima:** exigir `--format sln`, criar projetos com `-o` nos caminhos do design, adicioná-los à solution, ligar o teste à API e só então gerar os locks.
- **Critérios:** `TECH-BACKEND-01`, `PREM-ARCH-01`, `PREM-ARCH-02`, `DOC-TRACE-01`.
- **Decisão:** corrigido no bloco executável de scaffold de M1, incluindo o diretório explícito do workspace Angular.

### Candidatos rejeitados ou tratados como risco

- **`409` de cadastro permite descobrir email existente:** não foi classificado como defeito porque rejeição e feedback de erro são comportamentos explícitos de `AC-REG-05`/`AC-REG-06`. O risco foi documentado no design e no plano; alterar o oráculo exigiria nova decisão de produto.
- **Health/migrations no startup:** mantidos porque a entrega é de instância única e os limites estão registrados no ADR-0002; não foi encontrada alternativa menor que preserve os gates de Compose e SQLite real.
- **Tags sem digest:** tags completas atendem ao critério aprovado; digest seria hardening adicional, não correção desta etapa.

### Comandos e resultados

| Comando/check | Resultado |
|---|---|
| `pwd` | Raiz confirmada em `/Users/thgOyo/Desktop/Dev/user-profile-sdd-challenge`. |
| `git status --short --branch` | `main`, worktree inicialmente limpo. |
| `git log --oneline -5` | Dois commits; `b184432` identificado como última etapa. |
| `git show` / `git diff b184432^ b184432` | Diff integral examinado: 11 arquivos documentais, 1.515 inserções e 12 remoções. |
| Leitura integral de `AGENTS.md`, `PLANS.md` e `docs/sdd/*` | Concluída; não havia código/teste de aplicação relacionado. |
| Parse YAML com Ruby/Psych | OpenAPI `3.0.3`, seis operações e seis `operationId` únicos. |
| Redocly CLI `2.1.5` em cache temporário | Contrato semanticamente válido antes das correções; revalidação final registrada abaixo. |
| Checagem de IDs e matriz | Baseline original confirmada com 19 FR, 14 NFR, 16 premissas e 38 critérios únicos/presentes. |
| Pesquisa de segredos e tags | Nenhum segredo/token real; somente placeholders e referências documentais. |
| Verificação em fontes oficiais | Versões/tags fixadas confirmadas como existentes e compatíveis em 2026-08-24. |
| `git diff --check b184432^ b184432` | Aprovado para o commit revisado. |

### Validação das correções

| Comando/check | Resultado final |
|---|---|
| Redocly CLI `2.1.5` | OpenAPI válido, sem erro; dois avisos consultivos mantidos: licença não declarada e `/health` sem resposta `4XX`. Não se inventou licença nem erro de cliente para silenciar regras genéricas. |
| `oas-validate` | `1 passing`, `0 failing`, `0 warnings`. |
| Parse e invariantes OpenAPI com Ruby/Psych | OpenAPI `3.0.3`; seis operações/IDs únicos; 46 referências `x-*` conhecidas; `503` nas cinco operações de negócio; login inválido em `400`; challenge Bearer obrigatório nos três recursos protegidos; títulos Problem Details coerentes. |
| Checagem de IDs e matriz | 19 FR, 14 NFR, 16 premissas, 40 critérios e 55 testes planejados; IDs de definição únicos e rastreabilidade completa. |
| Links Markdown | 15 arquivos examinados; todos os links locais resolvem. |
| Pesquisa de padrões de segredo | Nenhuma chave, credencial, token ou private key real encontrada. |
| Ajuda do .NET CLI `10.0.105` | Confirmou `--format sln`, default `.slnx`, `-o`, `--use-controllers`, `dotnet sln add` e `dotnet add ... reference` usados pelo scaffold planejado. |
| `git diff --check` e revisão integral do diff | Aprovados; três leituras cruzadas finais de contrato, rastreabilidade e operação não deixaram achado aberto. |
| Build/testes funcionais/E2E/Compose | Não aplicáveis à etapa documental: o repositório ainda não contém aplicação, dependências ou infraestrutura executável; permanecem gates obrigatórios de M1–M6. |

### Riscos restantes

- A enumeração por email duplicado no cadastro permanece como consequência consciente dos critérios de feedback existentes.
- Nenhum comportamento da aplicação pode ser executado nesta etapa porque o repositório continua sem código; builds, testes funcionais e Docker permanecem gates de M1–M6.
- Versões e tags exatas envelhecem e devem ser reconfirmadas em M1, como já exige o design.

## 2026-08-25 — Revisão independente de M1

- **Etapa revisada:** M1 — walking skeleton executável.
- **Commit revisado:** `8db5592f2cb925004edb7cceb368995bcb2f2de4` (`b7de2fc716e2c4054f1d781faa4dda3fc58a66a3` como base).
- **Escopo:** diff integral de 65 arquivos; SDD/ADRs; solution, API, EF Core/migration, integrações, Angular, locks, Dockerfiles, Nginx, Compose, scripts e histórico recente. Funcionalidades M2–M6 não foram tratadas como implementadas.
- **Critérios examinados:** gates M1 `TECH-*`, `BE-DB-001`, `BE-HEALTH-001`, `BE-OAS-001`, `SPEC-OAS-001`–`005`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001`, `OPS-SECRET-001`, `API-ERROR-01`, `SEC-SECRET-01`, `DOC-SDD-01`, `DOC-TRACE-01`, `TEST-FLOW-01` e premissas relacionadas.

As localizações de `REV-M1-001`–`019` referem-se ao commit revisado, antes das correções; `REV-M1-020`–`024` foram encontrados na segunda revisão do diff corretivo. No total, foram confirmados 1 achado High, 12 Medium e 11 Low.

### Achados confirmados e decisões

#### `REV-M1-001` — High — suíte backend retornava sucesso sem executar testes

- **Localização:** `tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj:1-22`.
- **Evidência:** o projeto não declarava `IsTestProject`; no SDK exato, `dotnet test UserProfile.sln --no-restore --no-build --verbosity normal` saía com código zero após apenas o target VSTest da solution, sem assembly, descoberta, contagem ou resultado de teste.
- **Impacto:** os 6/6 testes registrados como evidência de M1 nunca haviam sido executados, invalidando o gate e a Definition of Done.
- **Correção mínima:** marcar explicitamente o projeto, repetir build/test e exigir no SDD evidência de descoberta/contagem.
- **Critérios:** `TECH-BACKEND-001`, `NFR-TEST-01`, gates M1 e Definition of Done.
- **Decisão:** corrigido com `IsTestProject=true`; o mesmo comando passou a descobrir um assembly e aprovar 6/6 testes.

#### `REV-M1-002` — Medium — validador OpenAPI não implementava os checks declarados

- **Localização:** `scripts/validate-openapi.rb:9-39`, `04-test-strategy.md:106-110,133`.
- **Evidência:** o script original verificava versão, paths/IDs, security e referências, mas não request/response schemas, `additionalProperties`, `userId`, regras de campos, status, ProblemDetails, challenge Bearer, login `400`, `503` nem campos sensíveis.
- **Impacto:** regressões de segurança e contrato passavam embora `SPEC-OAS-003`–`005` estivessem marcados como aprovados.
- **Correção mínima:** validar diretamente todas as invariantes nomeadas e provar o caminho negativo com contrato mutado.
- **Critérios:** `SPEC-OAS-001`–`005`, `SEC-AUTH-01`, `API-ERROR-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido no script; a mutação temporária de `getHealth` foi rejeitada. Um linter externo adicional foi rejeitado por não acrescentar uma invariante concreta e introduzir dependência nova nesta etapa.

#### `REV-M1-003` — Medium — OpenAPI runtime de health divergia do contrato normativo

- **Localização:** `HealthController.cs:10-13`, `HealthResponse.cs:3`, `HealthTests.cs:143-156`, `03-api-contract.yaml:280-312,456-466`.
- **Evidência:** o Swagger gerado usava tag `Health`, omitia `operationId`, publicava `status` como string livre e trazia info diferente; o teste verificava apenas `/health` e o campo obrigatório.
- **Impacto:** documentação/clientes runtime divergiam do contrato enquanto M1 aparecia concluído.
- **Correção mínima:** declarar metadata/info/enum normativos e ampliar o teste do documento exposto.
- **Critérios:** `BE-OAS-001`, `SPEC-OAS-002`, `SPEC-OAS-004`, `DOC-TRACE-01`.
- **Decisão:** corrigido com nome/tag explícitos, `HealthState` serializado como string, info Swagger e asserts de info, respostas e enum.

#### `REV-M1-004` — Medium — teste do schema não comprovava os sete campos declarados

- **Localização:** `HealthTests.cs:35-75`, `06-traceability.md:65`.
- **Evidência:** o teste consultava apenas timestamps e índice; `HasPendingModelChanges` compara modelo e snapshot, não o schema realmente aplicado.
- **Impacto:** migration sem campos essenciais ainda poderia passar enquanto `PREM-DATA-02` constava como comprovado.
- **Correção mínima:** consultar `pragma_table_info('Users')` e exigir conjunto exato, tipos, nulabilidade e chave primária.
- **Critérios:** `BE-DB-001`, `PREM-DATA-02`, `DOC-TRACE-01`.
- **Decisão:** corrigido com verificação das sete colunas reais e manutenção da prova do índice único.

#### `REV-M1-005` — Medium — timeout de health não representava o runtime do Compose

- **Localização:** `compose.yaml:12`, `DatabaseHealthCheck.cs:17-28`, `ApiFactory.cs:27-28`.
- **Evidência:** o Compose omitia `Default Timeout`, cujo padrão é 30 segundos; o teste forçava um segundo. A resposta podia exceder os limites do Nginx apesar do teste passar, conforme a [documentação do Microsoft.Data.Sqlite](https://learn.microsoft.com/en-us/dotnet/standard/data/sqlite/connection-strings).
- **Impacto:** locks SQLite poderiam ocupar requests além do limite operacional e a prova não reproduzia a configuração entregue.
- **Correção mínima:** limitar somente o comando do health e manter o teste com connection default de 30 segundos, afirmando duração observável.
- **Critérios:** `BE-HEALTH-001`, `OPS-DOCKER-01`, `API-ERROR-01`.
- **Decisão:** corrigido com `CommandTimeout=1`; lock real produziu `503 ProblemDetails` em aproximadamente 1,5 segundo e abaixo de cinco segundos.

#### `REV-M1-006` — Medium — contexto Docker podia incluir segredos locais ignorados pelo Git

- **Localização:** `.dockerignore:4-6`, `.gitignore:34-43`, Dockerfile backend `COPY` do diretório da API.
- **Evidência:** `appsettings.*.local.json`, `secrets.json`, `*.pfx`, `*.p12` e `*.key` eram ignorados pelo Git, mas não pelo contexto Docker.
- **Impacto:** credenciais locais poderiam entrar em cache/camada e tornar builds dependentes da máquina.
- **Correção mínima:** replicar os padrões sensíveis nos `.dockerignore`, sem alterar a estrutura dos Dockerfiles.
- **Critérios:** `SEC-SECRET-01`, `NFR-CONFIG-01`, `OPS-DOCKER-02`.
- **Decisão:** corrigido nos contextos raiz/backend e frontend; nenhuma credencial real foi encontrada.

#### `REV-M1-007` — Medium — gates Compose/Nginx não tinham smoke versionado

- **Localização:** `04-test-strategy.md:121-125`, `05-execution-plan.md:47,172-174`, diretório `scripts/`.
- **Evidência:** origem única, porta interna, tags, `404` e `502/504` → `503` apareciam somente em narrativa de comandos manuais.
- **Impacto:** regressões operacionais não quebrariam uma verificação repetível, contrariando o gate M1.
- **Correção mínima:** um script único com config/build/wait, smokes HTTP, inspeções e teardown sem `-v`.
- **Critérios:** `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001`, `API-ERROR-01`.
- **Decisão:** corrigido com `scripts/validate-m1-compose.sh`; o script final passou integralmente.

#### `REV-M1-008` — Medium — registro anterior da auditoria M1 era incompleto

- **Localização:** `review-log.md:248-253`.
- **Evidência:** quatro bullets omitiam commit/base, arquivos/critérios, IDs/severidades, evidências, decisões individualizadas, comandos/resultados e riscos.
- **Impacto:** a aprovação não era reproduzível nem auditável a partir do artefato obrigatório.
- **Correção mínima:** substituir a seção por este registro completo.
- **Critérios:** `DOC-SDD-01`, `DOC-TRACE-01`, `AI-SDD-01`, Definition of Done.
- **Decisão:** corrigido nesta seção.

#### `REV-M1-009` — Medium — histórico da revisão de design foi reescrito

- **Localização:** `review-log.md:13,152-159`, comparado ao mesmo arquivo em `b7de2fc`.
- **Evidência:** `REV-DESIGN-016` originalmente recomendava remover timestamps sem consumidor; M1 reutilizou o ID para afirmar o oposto e dizer que a arquitetura os exigia.
- **Impacto:** o audit trail deixou de registrar a decisão realmente tomada e ocultou sua posterior reversão.
- **Correção mínima:** restaurar o texto original e registrar a supersessão em novo achado M1.
- **Critérios:** `AI-SDD-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido; `REV-DESIGN-016` foi restaurado e aponta para `REV-M1-010` como decisão posterior.

#### `REV-M1-010` — Medium — timestamps eram tratados como premissa aprovada sem proveniência nem teste de ciclo de vida

- **Localização:** `00-challenge.md:67-92`, `01-requirements.md:155-165`, `02-technical-design.md:119-127`, `04-test-strategy.md:47,59,65,70`, `06-traceability.md:65`.
- **Evidência:** o challenge não exige timestamps; eles surgiram no design de `b184432`, foram removidos em `b7de2fc` e retornaram em M1 como arquitetura “aprovada”. Os testes planejados não afirmavam inicialização, preservação ou avanço.
- **Impacto:** escopo interno era apresentado como requisito externo e implementações futuras poderiam persistir valores incorretos com gates verdes.
- **Correção mínima:** reclassificar como decisão interna, formalizá-la no ADR-0002 e planejar asserts de ciclo de vida em M2/M4.
- **Critérios:** `PREM-DATA-02`, `BE-REG-001`, `BE-PROF-003`, `BE-PASS-003`, `DOC-TRACE-01`.
- **Decisão:** corrigido no requisito, design, ADR, estratégia e matriz; a parcela M1 agora é explicitamente parcial.

#### `REV-M1-011` — Medium — `TEST-FLOW-01` recebia progresso sem cobrir fluxo funcional

- **Localização:** `01-requirements.md:150`, `06-traceability.md:49,85`.
- **Evidência:** os testes M1 cobriam infraestrutura/shell, enquanto o critério exige cadastro, login, proteção, dashboard, perfil e senha.
- **Impacto:** a matriz sugeria evidência funcional que só poderá existir em M2–M4.
- **Correção mínima:** manter `TEST-FLOW-01` pendente e separar os gates infra comprovados.
- **Critérios:** `NFR-TEST-01`, `TEST-FLOW-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido na matriz.

#### `REV-M1-012` — Medium — testes concluídos não estavam ligados a arquivos/métodos/comandos

- **Localização:** `06-traceability.md:40-85`, testes backend/frontend e scripts.
- **Evidência:** a matriz mantinha somente “Teste planejado”; nenhum vínculo permitia conferir qual dos 6+2 testes provava cada gate M1.
- **Impacto:** contagens não ofereciam rastreabilidade executável entre critério, implementação e evidência.
- **Correção mínima:** mapear cada ID M1 para método/arquivo/comando estável sem poluir produção com comentários.
- **Critérios:** `DOC-TRACE-01`, `NFR-TRACE-01`.
- **Decisão:** corrigido pela tabela “Evidência executável de M1” em `06-traceability.md`.

#### `REV-M1-013` — Low — design indicava duas localizações para E2E

- **Localização:** `02-technical-design.md:89-95`, `tests/e2e/README.md`.
- **Evidência:** a árvore/checkout usam `tests/e2e`, mas o texto dizia que as jornadas ficariam no workspace Angular.
- **Impacto:** M5 poderia duplicar configuração/dependências Playwright.
- **Correção mínima:** escolher `tests/e2e` e alinhar o texto.
- **Critérios:** `PREM-ARCH-03`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial e documental.

#### `REV-M1-014` — Low — “restart saudável” não tinha evidência

- **Localização:** `06-traceability.md:44`, `05-execution-plan.md:172-177`.
- **Evidência:** havia `up`/`down`, mas nenhum ciclo de recriação no mesmo volume registrado.
- **Impacto:** `OPS-DOCKER-03` ficava superestimado.
- **Correção mínima:** remover a alegação e manter persistência/recriação pendente.
- **Critérios:** `OPS-DOCKER-03`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial; o volume é apenas configurado/preservado em M1.

#### `REV-M1-015` — Low — allowlist de scripts npm não era fechada

- **Localização:** `package.json:14-18`, lockfile e Dockerfile frontend.
- **Evidência:** `strict-allow-scripts` não está habilitado e um install script transitivo permanece fora da lista.
- **Impacto:** a configuração aparenta bloquear scripts não revisados, mas o npm apenas avisa.
- **Correção mínima:** decidir cada script e validar `strict-allow-scripts` nas plataformas suportadas.
- **Critérios:** `TECH-FRONTEND-01`, `NFR-SEC-01`.
- **Decisão:** adiado para M5; ativar enforcement sem testar dependências opcionais por plataforma não é mudança trivial de M1.

#### `REV-M1-016` — Low — fallback SPA responde HTML para asset inexistente

- **Localização:** `src/frontend/user-profile-web/nginx.conf:38-40`, ADR-0004.
- **Evidência:** `try_files $uri $uri/ /index.html` também transforma `/arquivo-inexistente.js` em `200 text/html`.
- **Impacto:** assets/chunks ausentes geram erro de MIME em vez de `404` operacional.
- **Correção mínima:** separar paths com extensão/ativos e adicionar smoke específico.
- **Critérios:** `OPS-ORIGIN-001`, ADR-0004.
- **Decisão:** adiado para M5; altera roteamento de deploy e requer teste de assets, portanto não é correção trivial do skeleton.

#### `REV-M1-017` — Low — collector de cobertura ainda não tem consumidor

- **Localização:** csproj de integração e lock NuGet.
- **Evidência:** `coverlet.collector` não é usado por comando/gate atual.
- **Impacto:** pequena superfície de dependência sem benefício imediato.
- **Correção mínima:** remover ou usá-lo quando M5 definir cobertura/CI.
- **Critérios:** `TECH-BACKEND-01`, `PREM-ARCH-03`.
- **Decisão:** adiado para M5; remover agora exige regenerar locks e anteciparia a decisão de cobertura.

#### `REV-M1-018` — Low — assert OpenAPI dependia da ordem das responses

- **Localização:** `HealthTests.cs:179-181` após a correção Medium do teste.
- **Evidência:** propriedades JSON/OpenAPI não têm ordem semântica, mas o assert comparava a enumeração diretamente.
- **Impacto:** versão futura do gerador poderia causar falso negativo sem drift de contrato.
- **Correção mínima:** ordenar as chaves antes da comparação.
- **Critérios:** `BE-OAS-001`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial; suíte completa repetida.

#### `REV-M1-019` — Low — falha de migration era identificada por texto incidental

- **Localização:** `StartupTests.cs:25-31`.
- **Evidência:** o teste buscava a substring inglesa `already exists` em uma exceção genérica.
- **Impacto:** mensagem/wrapping do provider poderia causar falso negativo ou aceitar a falha errada.
- **Correção mínima:** afirmar `SqliteException` na causa raiz e seu código SQLite.
- **Critérios:** `BE-HEALTH-001`, `OPS-DOCKER-01`.
- **Decisão:** corrigido por ser trivial; o teste agora exige `SqliteErrorCode=1`.

#### `REV-M1-020` — Medium — gate Compose exato não pôde ser revalidado após as correções

- **Localização:** `04-test-strategy.md`, `05-execution-plan.md`, `06-traceability.md`, `README.md` e ambiente Docker da revisão.
- **Evidência:** tanto o volume padrão preexistente quanto um volume Docker padrão novo falharam antes da migration com `SQLite Error 10/13` (`disk I/O error`/`database or disk is full`). O script final passou quando o mesmo volume nomeado foi apoiado em diretório temporário do host, mas isso não é a configuração exata exigida por `OPS-COMPOSE-001`.
- **Impacto:** marcar a revalidação independente como integralmente concluída criaria evidência superior ao que o ambiente permitiu observar.
- **Correção mínima:** liberar espaço na VM Docker e repetir `scripts/validate-m1-compose.sh` sem `COMPOSE_FILE`/override.
- **Critérios:** `OPS-COMPOSE-001`, `OPS-DOCKER-01`, `DOC-TRACE-01`.
- **Decisão:** bloqueado por estado externo; plano, estratégia, matriz, índice e riscos foram qualificados como parciais. Nenhum volume ou recurso de outro projeto foi removido para forçar o gate.

#### `REV-M1-021` — Low — tabela executável omitia a sexta integração backend

- **Localização:** tabela “Evidência executável de M1” em `06-traceability.md`.
- **Evidência:** `BE-HEALTH-001` listava os dois métodos de health, mas não `StartupFailsWhenInitialMigrationCannotApply`.
- **Impacto:** a cadeia da falha de migration não era conferível embora a suíte declarasse seis testes.
- **Correção mínima:** incluir o método de `StartupTests.cs` na mesma linha.
- **Critérios:** `BE-HEALTH-001`, `OPS-DOCKER-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial.

#### `REV-M1-022` — Low — parcela M1 de `OPS-SECRET-001` não tinha comando/escopo

- **Localização:** `06-traceability.md`, `review-log.md` e `04-test-strategy.md`.
- **Evidência:** havia alegação de pesquisa de segredos, mas a tabela executável não distinguia padrões/repositório/contexto da auditoria de logs futura.
- **Impacto:** a evidência de ausência de segredo não era reproduzível pelo SDD.
- **Correção mínima:** registrar comando, famílias de padrões, inspeção dos Docker ignores e o limite M5 para logs funcionais.
- **Critérios:** `OPS-SECRET-001`, `SEC-SECRET-01`, `SEC-LOG-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser documental e trivial.

#### `REV-M1-023` — Low — ADR-0002 não referenciava a premissa de timestamps

- **Localização:** `adr/0002-sqlite-persistence.md`, seção Rastreabilidade.
- **Evidência:** o ADR formalizava o ciclo de vida, mas omitia `PREM-DATA-02`.
- **Impacto:** a decisão indicada como fonte pela matriz não era autocontida.
- **Correção mínima:** adicionar a premissa à seção de rastreabilidade.
- **Critérios:** `PREM-DATA-02`, `DOC-SDD-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial.

#### `REV-M1-024` — Low — premissa de topologia continuava ligada a `TEST-FLOW-01`

- **Localização:** linha `PREM-ARCH-02` de `06-traceability.md`.
- **Evidência:** um executável/um projeto de integração era ligado ao critério de fluxos funcionais, apesar de esses fluxos permanecerem pendentes.
- **Impacto:** mantinha uma associação semântica falsa após a correção de `REV-M1-011`.
- **Correção mínima:** ligar a topologia a `TECH-BACKEND-01` e reservar `TEST-FLOW-01` aos fluxos.
- **Critérios:** `TECH-BACKEND-01`, `TEST-FLOW-01`, `DOC-TRACE-01`.
- **Decisão:** corrigido por ser trivial.

Consolidação: o High e 11 Medium foram corrigidos; `REV-M1-020` é o único Medium bloqueado, por estado externo explícito. Oito Low triviais foram corrigidos e `REV-M1-015`–`017` permanecem adiados.

### Candidatos rejeitados e riscos aceitos

- Nenhuma falha JWT/`sub`, senha ou autorização foi classificada: esses fluxos não existem em M1 e permanecem obrigatórios em M3–M4.
- Health transitivo, migrations no startup, Nginx same-origin e builds multi-stage foram mantidos porque atendem gates explícitos; não foi adicionada abstração nova.
- `409` por email duplicado continua decisão consciente dos critérios de cadastro, não defeito introduzido pelo skeleton.

### Comandos e resultados

| Comando/check | Resultado |
|---|---|
| `pwd`, `git status --short --branch`, `git log --oneline -5` | Raiz correta; `main` inicialmente limpo; M1/commit/base identificados. |
| `git diff b7de2fc 8db5592`, leitura integral de SDD/ADRs/código/testes/locks/configuração | Diff integral de 65 arquivos examinado por eixos backend, frontend/operação e rastreabilidade. |
| `git diff --check b7de2fc 8db5592` | Aprovado no commit revisado. |
| Teste backend original no SDK `10.0.400` | Exit 0, mas nenhum assembly/teste descoberto; confirmou `REV-M1-001`. |
| `dotnet restore ... --locked-mode`, `dotnet build ... --no-restore`, `dotnet test ... --no-restore --no-build --verbosity normal` em `mcr.microsoft.com/dotnet/sdk:10.0.400-noble` | Restore dos dois projetos aprovado; build com 0 warnings/0 erros; 1 arquivo descoberto; 6/6 testes aprovados. |
| Frontend em cópia temporária com `node:24.19.0-bookworm-slim`: `npm ci`, lint, test e build | Node `24.19.0`/npm `11.17.0`; 0 vulnerabilidades; lint aprovado; 2/2 testes; bundle 265,02 kB bruto. |
| `npm audit --package-lock-only --audit-level=low` | 0 vulnerabilidades. |
| `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` | `SPEC-OAS-001`–`005`, 6 operações e 42 referências locais aprovados. |
| Contrato copiado para `/private/tmp` com `operationId` mutado | Rejeitado com `Unexpected operationId for GET /health`, como esperado. |
| `sh -n scripts/validate-m1-compose.sh`, `docker compose config --quiet` | Sintaxe e Compose base aprovados. |
| `scripts/validate-m1-compose.sh` com Compose/volume padrão | O volume preexistente falhou com SQLite `disk I/O error`; projeto isolado com volume padrão novo falhou com `database or disk is full`. Gate exato bloqueado pelo ambiente. |
| `env COMPOSE_PROJECT_NAME=user-profile-m1-review-verified COMPOSE_FILE=compose.yaml:/private/tmp/user-profile-m1-review-verified.override.yaml M1_REVIEW_DATA_DIR=/private/tmp/user-profile-m1-review-verified-data.fbw7w5 scripts/validate-m1-compose.sh` | Exit 0: script final aprovou tags/imagens, API interna/não-root, SPA/fallback, health, Swagger, `404`, Nginx `502/504` e `503` real; somente o backing do volume nomeado foi direcionado ao host e o teardown o preservou. |
| Pesquisa de segredos, locks/URLs/integridade e `npm audit` | Nenhum segredo real, registry privado ou vulnerabilidade conhecida encontrado. |
| `git diff --check`, revisão integral do diff corretivo e links/IDs SDD | Aprovados na validação final registrada no commit de revisão. |

### Limitações ambientais e cleanup

- A VM Docker compartilhada ficou sem espaço: tanto o volume normal preexistente quanto um volume normal novo falharam com SQLite `disk I/O error`/`database or disk is full`. O `compose.yaml` base foi validado separadamente e o smoke final usou o mesmo volume nomeado em projeto isolado, com dados apoiados em diretório temporário do host; toda a superfície runtime passou.
- Foram removidos somente volumes/bancos dos projetos temporários `user-profile-m1-review*`, uma imagem dangling sem tag/contêiner deste repositório e um cache reclaimable antigo de `dotnet publish` identificado por ID. O volume principal e recursos de outros projetos não foram alterados.
- A cópia frontend, overrides e dados temporários foram removidos após os testes.

### Riscos restantes

- `REV-M1-015`–`017` permanecem Low e adiados para M5 com justificativa; não bloqueiam a implementação M1.
- A execução com volume Docker padrão deve ser repetida após liberar espaço na VM; o erro observado é ambiental, mas o volume principal local pode exigir manutenção pelo responsável.
- Ciclo de vida de timestamps será provado nos fluxos de M2/M4; persistência após recriação permanece pendente até haver usuário em M2/M6.
- Cadastro, autenticação/JWT, dashboard, perfil, senha, E2E, CI e README final continuam corretamente pendentes em M2–M6.
