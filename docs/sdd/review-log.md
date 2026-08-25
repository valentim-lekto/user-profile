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

#### `REV-DESIGN-016` — Low — remoção de timestamps contrariava a arquitetura aprovada

- **Localização:** `02-technical-design.md:115-120`, `04-test-strategy.md:47,58`, `05-execution-plan.md:98`.
- **Evidência:** a arquitetura fornecida explicitamente exige `CreatedAtUtc`/`UpdatedAtUtc`; não os expor nos DTOs não autoriza removê-los do modelo persistido.
- **Impacto:** design, schema e implementação divergiriam de uma decisão aprovada.
- **Correção mínima:** manter os campos internos, sem ampliar o contrato HTTP, e provar suas colunas na migration inicial.
- **Critérios:** `PREM-DATA-02`, `DOC-TRACE-01`.
- **Decisão:** registro corrigido na auditoria final de M1; entidade, migration, teste, plano e matriz preservam os timestamps aprovados.

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

## 2026-08-25 — Auditoria final de M1

- **Escopo:** código, testes, Docker e atualizações SDD do walking skeleton, sem revisar funcionalidades de M2–M6 como implementadas.
- **Achado bloqueante:** `User` e a migration omitiam `CreatedAtUtc`/`UpdatedAtUtc`, e o design havia removido esses campos contrariando a arquitetura explicitamente aprovada.
- **Correção:** premissa e design restaurados antes do código; entidade, configuração, migration, snapshot e teste SQLite atualizados. O teste também exige ausência de mudanças pendentes entre modelo e snapshot.
- **Resultado:** nenhum outro bloqueador M1; não há endpoint de negócio, segredo/tag flutuante ou porta pública adicional. Restore/build/test, frontend, OpenAPI, Compose, smokes same-origin e teardown foram aprovados antes do commit.
