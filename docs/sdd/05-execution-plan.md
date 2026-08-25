# Plano de execução — User Profile

## Objetivo

Entregar a aplicação especificada em incrementos verticais verificáveis, executável por uma única origem com `docker compose up`, mantendo rastreabilidade entre requisitos, contrato, implementação e testes.

Somente um milestone pode estar em andamento. O próximo começa após o anterior cumprir seus gates, ter o diff revisado e registrar o resultado nesta página.

## Critérios de aceite relacionados

- `AC-REG-01`–`AC-REG-06` — cadastro e validações.
- `AC-LOGIN-01`–`AC-LOGIN-03` — autenticação e sessão.
- `AC-DASH-01`–`AC-DASH-04` — dashboard protegido.
- `AC-PROF-01`–`AC-PROF-05` — consulta e edição de perfil.
- `AC-PASS-01`–`AC-PASS-04` — alteração de senha e encerramento da sessão.
- `UI-STATE-01`, `API-ERROR-01` — estados de UI e erros HTTP.
- `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`, `SEC-LOG-01` — segurança.
- `TECH-BACKEND-01`, `TECH-FRONTEND-01` — tecnologias obrigatórias e builds reproduzíveis.
- `OPS-DOCKER-01`–`OPS-DOCKER-03` — execução e persistência.
- `DOC-RUN-01`, `DOC-SDD-01`, `DOC-TRACE-01` — documentação e rastreabilidade.
- `TEST-FLOW-01`, `AI-SDD-01`, `AI-EXPLAIN-01`, `DEL-REPO-01` — qualidade e entrega.

## Contexto

- A especificação, o design, o OpenAPI, a estratégia de testes, a matriz e quatro ADRs estão definidos antes do scaffold.
- Não existe código de aplicação no início deste plano.
- Arquitetura: um executável `UserProfile.Api`, um projeto `UserProfile.Api.IntegrationTests`, Angular standalone e SQLite em volume.
- Versões e tags exatas estão em [`02-technical-design.md`](02-technical-design.md).
- O contrato normativo está em [`03-api-contract.yaml`](03-api-contract.yaml).
- O avaliador final deve precisar somente de Docker e Docker Compose.

## Milestones

### M1 — Walking skeleton e Docker

**Estado:** implementação concluída em 2026-08-25; revalidação independente com volume Docker padrão bloqueada por falta de espaço na VM

Entregas:

- criar solution, `UserProfile.Api` com Controllers e o único projeto de integração;
- criar Angular standalone/strict com routing, Reactive Forms e Angular Material;
- fixar SDK com `rollForward: disable`, pacotes, bootstrap único dos `packages.lock.json` via `--use-lock-file`, `package-lock.json` e todas as tags Docker aprovadas; após versionar os locks, todo restore usa `--locked-mode`;
- implementar estrutura mínima por funcionalidades, `User` com os sete campos definidos internamente no ADR-0002, `DbContext`, configuração, migration inicial com timestamps/índice único e aplicação de migrations no startup;
- disponibilizar `/health` com checagem SQLite e Swagger/OpenAPI no backend;
- criar Dockerfiles multi-stage, Nginx same-origin encaminhando `/api`, `/swagger` e `/health`, com conversão explícita de `502/504` do upstream para `503 ProblemDetails`, volume e `compose.yaml` sem dependência de `.env`; a única probe do Compose roda no `web` com `wget -q -O /dev/null http://127.0.0.1:8080/health`, e `web` depende de `api` como `service_started`;
- criar `.env.example` opcional, sem valor utilizável, sem tornar sua cópia pré-requisito do Compose;
- criar testes mínimos de startup, migration, health, build e smoke do Compose;
- criar validação automatizada local da sintaxe/estrutura do OpenAPI; a integração desse check ao CI ocorre em M5.

Gates observáveis:

- backend e frontend compilam;
- testes de M1 passam;
- `docker compose up --build --wait` disponibiliza `http://localhost:8080`, `/health` e `/swagger` somente depois que a probe do `web` atravessa Nginx, API e SQLite por `/health`;
- com a API parada após startup, `/health` pelo Nginx retorna `503 application/problem+json` em vez de HTML, e a inspeção da configuração confirma o mesmo mapeamento para timeout `504`;
- nenhum `latest`, segredo ou porta pública adicional existe.

### M2 — Cadastro

**Estado:** pendente

Entregas:

- implementar normalização, `PasswordHasher<User>`, rejeição de propriedades JSON não mapeadas e `POST /api/auth/register` conforme OpenAPI;
- implementar tela de cadastro, validações reativas e estados loading/sucesso/erro;
- redirecionar ao login com mensagem de sucesso, sem criar sessão;
- implementar `BE-REG-*`, `FE-REG-*` e a prova global `BE-ERR-001/002` da estratégia de testes.

Gates observáveis:

- cadastro válido retorna `201`, persiste hash e nunca retorna token;
- invalidações retornam `400` e duplicidade normalizada retorna `409`;
- todos os testes M1+M2 passam e o diff referencia `AC-REG-*`.

### M3 — Login e dashboard

**Estado:** pendente

Entregas:

- implementar emissão/validação JWT, chave externa Base64 de ao menos 32 bytes, falha fechada para configuração inválida e fallback aleatório somente quando ausente em `Development`;
- completar em `.env.example` os nomes de configuração JWT introduzidos neste milestone, mantendo somente placeholders não utilizáveis;
- implementar `POST /api/auth/login` com `400 ValidationProblemDetails` genérico para credenciais inválidas e sem refresh token;
- implementar sessão em `sessionStorage`, functional interceptor com allowlist apenas para URLs relativas protegidas e functional route guard;
- implementar `GET /api/profile` resolvendo exclusivamente o claim `sub`;
- implementar login e dashboard com boas-vindas, navegação e estados de UI;
- implementar `BE-LOGIN-*`, `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PROF-001/002`, `TECH-BACKEND-001`, `FE-LOGIN-*`, `FE-GUARD-*`, `FE-INT-*` e `FE-DASH-*`, sempre verificando ProblemDetails, challenge Bearer obrigatório nos recursos protegidos e os DTOs da fatia.

Gates observáveis:

- login válido cria token de 15 minutos com claims mínimas e navega ao dashboard;
- login inválido retorna a mesma mensagem para email ou senha incorretos;
- dashboard não abre sem JWT válido e mostra o nome obtido da API;
- chave ausente/inválida fora de `Development` impede startup e o token nunca é enviado a destino público, absoluto ou externo;
- `.env.example` contém somente nomes e placeholders não utilizáveis, e o Compose continua iniciando sem copiá-lo;
- nenhuma operação recebe `userId` do cliente.

### M4 — Edição de perfil e senha

**Estado:** pendente

Entregas:

- implementar `PUT /api/profile` com validação e unicidade;
- implementar `PUT /api/profile/password` com senha atual, nova senha e confirmação;
- implementar tela de perfil com formulários separados para dados e senha;
- encerrar a sessão do frontend após troca de senha bem-sucedida;
- implementar `BE-PROF-003/004/005/006`, `BE-PASS-*`, `FE-PROF-*` e `FE-PASS-*`, incluindo autorização por `sub` dos dois endpoints novos e sempre verificando ProblemDetails e os DTOs da fatia.

Gates observáveis:

- nome/email e senha mudam por operações separadas;
- erros usam ProblemDetails; email duplicado usa `409`;
- senha atual incorreta não altera o hash; nova senha válida passa a autenticar;
- sucesso de senha remove o JWT do `sessionStorage`.

### M5 — Testes E2E, CI e acabamento

**Estado:** pendente

Entregas:

- fixar Playwright e implementar somente `E2E-001`–`E2E-003`;
- configurar CI para restore/build/test backend, install/build/test frontend, contrato, E2E e Compose smoke;
- auditar a cobertura acumulada de DTOs e ProblemDetails e concluir verificações de logs, segredo e tags;
- revisar acessibilidade básica dos formulários, feedback visual e submissões duplicadas;
- revisar dependências, imagens, configuração não sensível e ausência de escopo extra.

Gates observáveis:

- suíte completa passa sem skips;
- `E2E-001/002` atravessam Nginx, Angular, API e SQLite reais; `E2E-003` interrompe a API real pelo Compose e valida a falha observada no proxy;
- CI falha em drift de contrato, build, teste ou tag proibida;
- logs e artefatos não contêm senha, hash, token ou chave.

### M6 — Validação final e documentação

**Estado:** pendente

Entregas:

- criar/atualizar README raiz com comandos, URL, porta, health e procedimento de cadastro;
- validar checkout limpo somente com Docker e Docker Compose;
- validar recriação dos serviços preservando o volume SQLite;
- atualizar estado de todos os itens em `06-traceability.md`, plano e documentos SDD;
- revisar OpenAPI contra comportamento real, diff completo e decisões ADR;
- executar e registrar o walkthrough manual `DOC-EXPLAIN-001` sem transcrever conversas;
- registrar evidências finais e limitações; a publicação do repositório permanece ação explícita do responsável.

Gates observáveis:

- `TECH-*`, `OPS-COMPOSE-*`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-TAGS-001`, `OPS-SECRET-001`, `DOC-RUN-001` e `DOC-EXPLAIN-001` são reexecutados e passam;
- documentação reproduz exatamente o ambiente observado;
- todos os critérios possuem evidência e estado final correto;
- build, testes, E2E e revisão do diff estão aprovados.

## Progresso

- `2026-08-24` — `design concluído` — artefatos de design e planejamento criados; M1–M6 permanecem pendentes e nenhum código foi implementado.
- `2026-08-24` — `revisão independente concluída` — commit `b184432` auditado; 0 High, 15 Medium e 6 Low corrigidos; contrato, IDs, links, segredos e diff revalidados em [`review-log.md`](review-log.md); M1–M6 permanecem pendentes.
- `2026-08-24` — `M1 iniciado` — estrutura de testes alinhada a `tests/backend`, proxy Swagger e `.env.example` antecipado por instrução explícita; implementação e evidências ainda pendentes.
- `2026-08-25` — `M1 concluído` — walking skeleton backend/frontend, migration SQLite, testes, imagens multi-stage e origem única validados após auditoria final; M2–M6 permanecem pendentes e nenhum endpoint de negócio foi criado.
- `2026-08-25` — `revisão independente de M1 concluída` — commit `8db5592` auditado; 1 High, 12 Medium e 11 Low confirmados. O High e 11 Medium foram corrigidos; 1 Medium operacional está bloqueado pela VM Docker sem espaço; 8 Low triviais foram corrigidos e 3 Low adiados com justificativa em [`review-log.md`](review-log.md).

## Evidências de M1

| Gate | Execução observada | Resultado |
|---|---|---|
| Backend | SDK `10.0.400`: `dotnet restore UserProfile.sln --locked-mode`, `dotnet build UserProfile.sln --no-restore` e `dotnet test UserProfile.sln --no-restore --no-build --verbosity normal` | A revisão independente constatou que o comando original retornava sucesso sem descobrir testes. Depois de marcar o projeto de integração como projeto de teste, o restore locked passou, o build terminou com 0 warnings/0 erros e o VSTest descobriu e aprovou 6/6 integrações: schema SQLite exato, health `200/503` com limite de duração, contrato OpenAPI runtime de `/health`, falha de startup e `404 ProblemDetails`. |
| Frontend | Node `24.19.0`: `npm ci`, `npm run lint`, `npm test` e `npm run build` | Lint aprovado; 2/2 testes aprovados; bundle de produção com 265,02 kB bruto. |
| Dependências frontend | `npm audit --package-lock-only --audit-level=low` | 0 vulnerabilidades após atualizar Vitest de `4.0.8` para `4.1.11` por `GHSA-5xrq-8626-4rwp`. |
| Contrato | `ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml` e mutação negativa do `operationId` em cópia temporária | `SPEC-OAS-001`–`005` aprovados para 6 operações e 42 referências locais; a cópia inválida foi rejeitada como esperado. |
| Compose | `docker compose config --quiet`; tentativas padrão; `env COMPOSE_PROJECT_NAME=user-profile-m1-review-verified COMPOSE_FILE=compose.yaml:/private/tmp/user-profile-m1-review-verified.override.yaml M1_REVIEW_DATA_DIR=/private/tmp/user-profile-m1-review-verified-data.fbw7w5 scripts/validate-m1-compose.sh` sem `.env` | Configuração base aprovada. Tags, imagens, usuários, portas, SPA, health, Swagger, `404`, conversão `502/504` → `503` e teardown foram aprovados pelo script final em projeto isolado; somente o backing do volume nomeado foi desviado para o host. A repetição com volume Docker padrão continua bloqueada pela VM cheia. |
| Origem única | `curl` em `/`, rota SPA, `/health`, `/swagger/index.html`, `/swagger/v1/swagger.json` e `/api/not-implemented` | SPA/fallback, health e Swagger responderam pela porta 8080; rota API inexistente preservou `404 application/problem+json`; API sem binding de host. |
| Falha do upstream | `docker compose stop api` e `curl http://localhost:8080/health` | Nginx retornou `503 application/problem+json` com corpo ProblemDetails; `nginx -T` confirmou mapeamento explícito de `502` e `504`. |
| Cleanup | `docker compose down` sem `-v` | Contêineres/rede deste projeto removidos e volume `user-profile-sdd-challenge_user-profile-data` preservado. |

O primeiro startup do Compose encontrou `SQLite Error 13` porque a VM Docker compartilhada estava sem espaço. Foram removidos somente três volumes temporários criados durante esta validação (`user-profile-m1-node-modules`, `user-profile-m1-node-modules-clean` e `user-profile-m1-nuget`). Na repetição final após corrigir os timestamps, o disco voltou a ficar cheio: o volume vazio/stale deste próprio projeto foi recriado, uma imagem backend anterior e dois registros exatos do cache de `dotnet publish UserProfile.Api` foram removidos. Nenhum recurso de outro projeto foi apagado. Com 150 MB livres, o Compose reconstruiu, ambos os serviços ficaram saudáveis, os smokes passaram e o teardown final preservou o novo volume correto. O hash do commit local é informado no handoff da etapa, pois um commit não pode registrar o próprio hash em seu conteúdo.

Na revisão independente posterior, a VM voltou a ficar sem espaço e volumes Docker normais falharam antes da migration. `docker compose config` continuou aprovado e o smoke final completo passou em projeto isolado, usando o mesmo volume nomeado com armazenamento temporariamente apoiado no host. Essa adaptação e o cleanup pontual estão detalhados em [`review-log.md`](review-log.md); o volume principal não foi alterado e a execução padrão deve ser repetida após manutenção do disco da VM.

Ao iniciar um milestone, alterar somente seu estado para `em andamento`. Ao concluir, registrar data, comandos, evidências, desvios e hash do commit antes de iniciar o próximo.

## Comandos

Comandos planejados para o scaffold de M1, executados somente na etapa de implementação:

```sh
dotnet new sln -n UserProfile --format sln
dotnet new webapi -n UserProfile.Api -o src/backend/UserProfile.Api -f net10.0 --use-controllers
dotnet new xunit -n UserProfile.Api.IntegrationTests -o tests/backend/UserProfile.Api.IntegrationTests -f net10.0
dotnet sln UserProfile.sln add src/backend/UserProfile.Api/UserProfile.Api.csproj tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj
dotnet add tests/backend/UserProfile.Api.IntegrationTests/UserProfile.Api.IntegrationTests.csproj reference src/backend/UserProfile.Api/UserProfile.Api.csproj
npx @angular/cli@22.1.3 new user-profile-web --directory src/frontend/user-profile-web --standalone --strict --routing --style=scss --skip-git --package-manager=npm
```

Depois de adicionar todos os `PackageReference` previstos para M1, executar uma única vez o bootstrap dos locks, revisar os arquivos gerados e versioná-los:

```sh
dotnet restore UserProfile.sln --use-lock-file
```

Comandos recorrentes posteriores:

```sh
dotnet restore UserProfile.sln --locked-mode
dotnet build UserProfile.sln --no-restore
dotnet test UserProfile.sln --no-restore --no-build --verbosity normal
npm ci --prefix src/frontend/user-profile-web
npm run lint --prefix src/frontend/user-profile-web
npm run build --prefix src/frontend/user-profile-web
npm test --prefix src/frontend/user-profile-web
ruby scripts/validate-openapi.rb docs/sdd/03-api-contract.yaml
scripts/validate-m1-compose.sh
```

Os nomes de scripts npm devem ser confirmados no scaffold e então congelados. Mudança de comando exige atualização deste plano e do README antes do código dependente.

## Validação observável

- A URL única carrega a SPA e as chamadas `/api` permanecem na mesma origem.
- `/health` só fica saudável após migrations e acesso ao SQLite.
- Cadastro cria dados sem autenticar; login cria sessão curta; dashboard consulta a API.
- Rotas e endpoints protegidos rejeitam ausência, adulteração ou expiração do token.
- Toda resposta `401` de recurso protegido inclui challenge Bearer obrigatório; login inválido usa `400` genérico; indisponibilidade do upstream chega ao browser como `503 ProblemDetails`.
- Perfil do usuário A nunca consulta ou altera o usuário B.
- Atualizações persistem após recriar serviços mantendo o volume.
- Respostas e logs nunca expõem campos sensíveis.
- Cada resultado é ligado a teste e critério em `06-traceability.md`.

## Riscos

- **Drift OpenAPI/implementação** — validar contrato em CI e revisar status/schemas em cada milestone.
- **Concorrência SQLite/migrations** — manter uma instância; falhar startup em migration; documentar o limite.
- **Token em `sessionStorage`** — evitar HTML inseguro e dependências desnecessárias; expiração curta e limpeza em `401`.
- **Enumeração no cadastro** — o `409` de email duplicado é observável para cumprir o feedback explícito de erro; não expor outros dados e manter login genérico. Controles de abuso de produção permanecem fora da demonstração.
- **Token antigo após troca de senha** — aceitar validade até `exp`; não ampliar para revogação fora do escopo.
- **Chave sem `.env`** — gerar somente em `Development`; exigir configuração externa nos demais ambientes.
- **Tags envelhecidas** — reconfirmar as tags fixadas em M1; qualquer upgrade deve ser explícito e testado.
- **E2E instável** — limitar a três jornadas, esperar estados observáveis e não usar sleeps arbitrários.
- **Escopo crescente** — rejeitar patterns, endpoints e funcionalidades não ligados à matriz.

## Descobertas

- .NET 10/EF Core 10 são LTS e as versões `10.0.11` estão suportadas até novembro de 2028.
- Angular `22.1.3` é estável e aceita Node `^24.15.0`; Node `24.19.0` está em LTS.
- O “projeto de integração” foi esclarecido como o único projeto de testes HTTP do backend.
- Com SQLite, “persistência no Compose” é o arquivo em volume, não um serviço de banco.
- O requisito de Compose sem `.env` e a proibição de segredo versionado exigem chave efêmera apenas em `Development`.
- O logout após senha não revoga JWT no servidor; ampliar esse comportamento seria novo escopo.

## Decision log

- `2026-08-24` — Fixadas versões estáveis e tags completas após consulta às fontes oficiais; previews e aliases flutuantes foram excluídos.
- `2026-08-24` — Escolhido fluxo direto Controllers → EF Core/componentes concretos para preservar o monólito proporcional.
- `2026-08-24` — Fixada duração JWT em 15 minutos e tolerância de relógio em 30 segundos, sem refresh/revogação.
- `2026-08-24` — Definida normalização `Trim().ToUpperInvariant()` para cadastro, login e edição; email aparado preserva caixa para exibição.
- `2026-08-24` — Fixados `200` para login/updates, `201` para cadastro, `400` para validação, senha atual e credenciais de login inválidas, `401` com challenge Bearer para recursos protegidos, `409` para email e `503` para health/proxy.
- `2026-08-24` — Definida origem única em `http://localhost:8080`, com API interna e Nginx encaminhando `/api`, `/swagger` e `/health`.
- `2026-08-25` — M1 manteve `CreatedAtUtc`/`UpdatedAtUtc` como decisão interna agora formalizada no ADR-0002; a revisão independente restaurou o histórico que antes recomendara sua remoção e programou as provas de ciclo de vida em M2/M4.
- `2026-08-25` — A revisão independente descobriu que o comando backend original não executava testes; `IsTestProject`, contrato/runtime OpenAPI, schema real, timeout de health, ignores Docker, smoke Compose e rastreabilidade foram corrigidos antes do novo commit.
- `2026-08-25` — M1 manteve apenas o shell Angular e `/health`; a complexidade retida limita-se a migrations, SQLite real, ProblemDetails e proxy exigidos pelos critérios.

## Resultado final

M1 permanece implementado e suas correções passaram nas suítes backend/frontend, contrato e runtime isolado. A revalidação exata do volume Docker padrão está bloqueada pela falta de espaço da VM e não é declarada concluída nesta revisão. M2–M6 permanecem pendentes; cadastro, login, JWT, dashboard, edição de perfil/senha, E2E e CI não foram implementados.
