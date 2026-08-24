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

**Estado:** pendente

Entregas:

- criar solution, `UserProfile.Api` com Controllers e o único projeto de integração;
- criar Angular standalone/strict com routing, Reactive Forms e Angular Material;
- fixar SDK, pacotes, lockfile e todas as tags Docker aprovadas;
- implementar estrutura mínima por funcionalidades, `User`, `DbContext`, configuração, migration inicial com índice único e aplicação de migrations no startup;
- disponibilizar `/health` com checagem SQLite;
- criar Dockerfiles multi-stage, Nginx same-origin, volume e `compose.yaml` sem dependência de `.env`;
- criar testes mínimos de startup, migration, health, build e smoke do Compose;
- manter o OpenAPI versionado e validar sua sintaxe/estrutura em CI.

Gates observáveis:

- backend e frontend compilam;
- testes de M1 passam;
- `docker compose up --build --wait` disponibiliza `http://localhost:8080` e `/health` saudável;
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

- implementar emissão/validação JWT, chave externa e fallback aleatório de desenvolvimento;
- implementar `POST /api/auth/login` com erro genérico e sem refresh token;
- implementar sessão em `sessionStorage`, functional interceptor e functional route guard;
- implementar `GET /api/profile` resolvendo exclusivamente o claim `sub`;
- implementar login e dashboard com boas-vindas, navegação e estados de UI;
- implementar `BE-LOGIN-*`, `BE-AUTH-*`, `BE-PROF-001/002`, `FE-LOGIN-*`, `FE-GUARD-*`, `FE-INT-*` e `FE-DASH-*`, sempre verificando ProblemDetails e os DTOs da fatia.

Gates observáveis:

- login válido cria token de 15 minutos com claims mínimas e navega ao dashboard;
- login inválido retorna a mesma mensagem para email ou senha incorretos;
- dashboard não abre sem JWT válido e mostra o nome obtido da API;
- nenhuma operação recebe `userId` do cliente.

### M4 — Edição de perfil e senha

**Estado:** pendente

Entregas:

- implementar `PUT /api/profile` com validação, unicidade e `UpdatedAtUtc`;
- implementar `PUT /api/profile/password` com senha atual, nova senha e confirmação;
- implementar tela de perfil com formulários separados para dados e senha;
- encerrar a sessão do frontend após troca de senha bem-sucedida;
- implementar `BE-PROF-003/004/005`, `BE-PASS-*`, `FE-PROF-*` e `FE-PASS-*`, sempre verificando ProblemDetails e os DTOs da fatia.

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
- registrar evidências finais e limitações; a publicação do repositório permanece ação explícita do responsável.

Gates observáveis:

- `OPS-COMPOSE-*`, `OPS-ORIGIN-001`, `OPS-PERSIST-001`, `OPS-TAGS-001`, `OPS-SECRET-001` e `DOC-RUN-001` são reexecutados e passam;
- documentação reproduz exatamente o ambiente observado;
- todos os critérios possuem evidência e estado final correto;
- build, testes, E2E e revisão do diff estão aprovados.

## Progresso

- `2026-08-24` — `design concluído` — artefatos de design e planejamento criados; M1–M6 permanecem pendentes e nenhum código foi implementado.

Ao iniciar um milestone, alterar somente seu estado para `em andamento`. Ao concluir, registrar data, comandos, evidências, desvios e hash do commit antes de iniciar o próximo.

## Comandos

Comandos planejados para o scaffold de M1, executados somente na etapa de implementação:

```sh
dotnet new sln -n UserProfile
dotnet new webapi -n UserProfile.Api -f net10.0 --use-controllers
dotnet new xunit -n UserProfile.Api.IntegrationTests -f net10.0
npx @angular/cli@22.1.3 new user-profile-web --standalone --strict --routing --style=scss --skip-git --package-manager=npm
```

Comandos recorrentes após a criação dos projetos:

```sh
dotnet restore UserProfile.sln
dotnet build UserProfile.sln --no-restore
dotnet test UserProfile.sln --no-build
npm ci --prefix src/frontend/user-profile-web
npm run build --prefix src/frontend/user-profile-web
npm run test:ci --prefix src/frontend/user-profile-web
docker compose config --quiet
docker compose up --build --wait
curl --fail http://localhost:8080/health
docker compose down
```

Os nomes de scripts npm devem ser confirmados no scaffold e então congelados. Mudança de comando exige atualização deste plano e do README antes do código dependente.

## Validação observável

- A URL única carrega a SPA e as chamadas `/api` permanecem na mesma origem.
- `/health` só fica saudável após migrations e acesso ao SQLite.
- Cadastro cria dados sem autenticar; login cria sessão curta; dashboard consulta a API.
- Rotas e endpoints protegidos rejeitam ausência, adulteração ou expiração do token.
- Perfil do usuário A nunca consulta ou altera o usuário B.
- Atualizações persistem após recriar serviços mantendo o volume.
- Respostas e logs nunca expõem campos sensíveis.
- Cada resultado é ligado a teste e critério em `06-traceability.md`.

## Riscos

- **Drift OpenAPI/implementação** — validar contrato em CI e revisar status/schemas em cada milestone.
- **Concorrência SQLite/migrations** — manter uma instância; falhar startup em migration; documentar o limite.
- **Token em `sessionStorage`** — evitar HTML inseguro e dependências desnecessárias; expiração curta e limpeza em `401`.
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
- `2026-08-24` — Fixados `200` para login/updates, `201` para cadastro, `400` para validação/senha atual, `401` para autenticação, `409` para email e `503` para health.
- `2026-08-24` — Definida origem única em `http://localhost:8080`, com API interna e Nginx encaminhando `/api` e `/health`.

## Resultado final

Plano aprovado e pronto para iniciar M1 em uma etapa posterior. Nesta etapa, todos os milestones permanecem pendentes; foram produzidos somente documentos de design e planejamento, sem código, dependências ou infraestrutura executável.
