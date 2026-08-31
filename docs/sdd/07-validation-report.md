# 07 — Relatório de validação final

**Data:** 2026-08-27

**Baseline auditada:** `72d8941a8f563aac1f3c69d1690583cceacb30fc`

**Correção operacional validada:** `3f6fbc4`

**Resultado:** aprovado localmente, sem achado Alto ou Médio aberto

## Escopo e método

A auditoria começou com worktree limpa e leitura integral de governança, todos os artefatos SDD/ADRs, implementação, testes, Compose, CI e histórico recente. Afirmações documentais foram confrontadas com código e execução real. Quatro revisores independentes, somente leitura, avaliaram segurança, testes/CI/E2E, Docker e coerência SDD antes de qualquer edição; segurança, entrega e SDD receberam uma segunda leitura após as correções.

Cinco achados Médios únicos foram corrigidos: ausência dos artefatos finais de M6, bind HTTP em todas as interfaces, CSP descrita como se estivesse ativa, convenção de estado incompatível com pendências externas e descrição imprecisa do reset. Achados baixos documentais simples também foram encerrados. Não houve correção de código da aplicação ou do contrato de negócio; a única mudança operacional restringiu o bind e adicionou seu assert ao smoke.

## Ambiente e isolamento

- `.env` ausente; nenhum SDK de aplicação foi usado no host;
- projeto Compose fixo `user-profile-sdd-challenge`; Docker Compose
  `v2.37.1-desktop.1`;
- antes do reset, nenhum container do desafio estava ativo; a rede residual e
  o volume nomeado do projeto existiam;
- outro projeto Compose em execução foi apenas inventariado e permaneceu intocado;
- a porta 8080 estava livre;
- o reset encontrou zero containers e removeu a rede residual e o volume
  exclusivamente deste desafio; E2E e smoke usaram nomes/volumes efêmeros
  próprios.

## Comandos e resultados resumidos

| Comando ou verificação | Resultado observado |
|---|---|
| `docker compose down --volumes --remove-orphans` | Sem containers ativos; removeu somente a rede residual e o volume `user-profile-sdd-challenge_user-profile-data`; banco inicial vazio confirmado na execução seguinte. |
| `docker compose config --quiet` e inventário renderizado | Exit 0 sem `.env`; somente `web` publicou `127.0.0.1:8080`; quatro profiles e seis imagens exatas. |
| `docker compose build --no-cache --progress plain` | Exit 0; API publish Release e Angular build de 317,42 kB concluídos; tags `user-profile-api:0.1.0` e `user-profile-web:0.1.0`. |
| `docker compose up --detach --wait --wait-timeout 300` | API ficou `Up`, web ficou `healthy` e a probe transitiva confirmou API/SQLite; migrations criaram o SQLite no volume nomeado. |
| HTTP em `/`, `/health`, `/swagger/index.html` e `/swagger/v1/swagger.json` | Todos `200` pela origem única; SPA, health JSON e Swagger/OpenAPI runtime presentes. |
| Navegador real em `/`, `/dashboard`, `/login` e `/register` | Redirecionamento anônimo, labels, skip link, mensagens por campo, viewport 390×844 e console sem warning/erro aprovados. |
| Fluxo HTTP sintético + `docker compose restart` + nova espera de health | Cadastro/login/PUT de perfil aprovados; token da chave efêmera anterior recebeu `401`; nome/email persistiram no SQLite após restart. |
| Troca de senha sintética | `200` com resposta mínima; senha antiga recebeu `401`, nova recebeu `200` e consultou o perfil. |
| `docker compose --profile contract-tests run --rm contract-tests` | OpenAPI aprovado: `SPEC-OAS-001..005`, 6 operações e 53 referências locais. |
| `docker compose --profile backend-tests run --rm --build backend-tests` | 101/101 integrações aprovadas, 0 falhas, 0 skips; duração do runner 6 s. |
| `docker compose --profile frontend-tests run --rm --build frontend-tests` | `npm ci`, lint, 57/57 testes em 9 arquivos e build aprovados; runner de testes 12,49 s. |
| `./scripts/e2e-playwright.sh` | 3/3 jornadas Chromium aprovadas em 7,3 s; projeto/volume efêmeros removidos. |
| `./scripts/validate-m1-compose.sh` | Exit 0 antes e depois da correção: bind runtime exatamente `127.0.0.1:8080`, origem única, cadastro/login/perfil/senha, autorização, persistência, `413/415/503`, logs, tags, sanitizador e cleanup aprovados. |
| `docker run --rm --volume "$PWD:/repo:ro" --workdir /repo rhysd/actionlint:1.7.12` | Exit 0; workflow sintaticamente válido. |
| `git diff --check`, `sh -n`, links Markdown e contagens da matriz | Aprovados; 16 arquivos Markdown sem link local quebrado e contagens `19/14/18/40` confirmadas. |
| Scans do Git atual/histórico para segredo, banco e chave | 0 padrão secreto de alta confiança e 0 caminho de banco/chave; o único JWT-shaped é o marcador sintético do sanitizador. |
| Inventário Docker após teardown | 0 container/rede do desafio ou de seus projetos efêmeros; somente o volume principal preservado; projeto externo permaneceu ativo. |
| `git diff --check` e `git status --short` após o commit documental | Diff aprovado; status sem saída, confirmando worktree limpa antes do amend final. |

Os gates automatizados Docker (build limpo, profiles, E2E, smoke e actionlint) levaram aproximadamente quatro minutos, desconsiderando tempo de aprovação da ferramenta e cache/download externo.

## Ocorrências do próprio auditor

Falhas iniciais do script manual não foram atribuídas ao produto:

- um loop zsh usou o nome reservado `path` e removeu `curl` do `PATH`; a checagem foi repetida com variável segura;
- payloads/asserções iniciais usaram nomes ou shapes diferentes do OpenAPI (`confirmPassword`, DTO no `201`, `tokenType/expiresIn` e `204` na senha);
- a API rejeitou ou respondeu exatamente conforme o contrato (`passwordConfirmation`, `MessageResponse`, `accessToken` único e `200 MessageResponse`).

As credenciais foram geradas em diretório temporário com permissão restrita, não foram impressas e foram removidas ao fim de cada tentativa. As contas sintéticas criadas permanecem somente no volume local preservado, nunca no Git.

## Auditoria funcional

| Área | Estado | Evidência |
|---|---|---|
| Cadastro e validações | Verified | Integração, 57 testes frontend, E2E-001, smoke e `201/400/409` reais. |
| Login e erro genérico | Verified | Respostas byte-idênticas na integração, E2E-002 e smoke; Bearer válido no fluxo manual. |
| Dashboard protegido e boas-vindas | Verified | Guard/integração, inspeção anônima no navegador e E2E-001/003. |
| Consulta e edição de perfil | Verified | Isolamento por `sub`, payload mínimo, fluxo manual com restart e E2E-001. |
| Troca de senha | Verified | Atomicidade na integração, fluxo manual e E2E-003; senha antiga/nova verificadas. |
| Logout e reproteção | Verified | Specs do `AuthService`, E2E-001 e E2E-003. |
| Loading, sucesso e erro | Verified | Specs DOM das quatro telas, bloqueio/duplo submit e três jornadas reais. |

## Auditoria de segurança

Verified por inspeção e testes:

- `PasswordHasher<User>` gera/verifica hash; integração confirma que o valor persistido não é texto puro;
- JWT HMAC-SHA256 valida issuer, audience, assinatura, algoritmo, `exp` e claims mínimas, com `sub` Guid imutável;
- nenhum endpoint recebe/confia em `userId`; JSON desconhecido é rejeitado e query/header arbitrários não selecionam usuário;
- DTOs omitem senha, hash, email normalizado e timestamps;
- normalização ASCII consistente e índice único `UX_Users_NormalizedEmail`, inclusive corrida SQLite;
- login inexistente/senha errada usa o mesmo `401 ProblemDetails` genérico;
- não há CORS permissivo; o interceptor só anexa Bearer a método/URL relativos permitidos;
- somente o web publica porta, vinculada a `127.0.0.1`; a API permanece na
  rede interna do Compose;
- chave real, banco, token e `.env` não estão versionados; logs e artefatos usam política de redução/sanitização;
- `sessionStorage`, token curto, ausência de refresh/revogação e chave efêmera estão documentados como trade-offs.

## Auditoria Docker e cleanup

Verified:

- build multi-stage sem cache, tags específicas e execução sem `.env`;
- somente Nginx publicado em `127.0.0.1:8080`; API sem porta de host e
  executando como usuário não-root;
- healthcheck atravessa Nginx, API e SQLite;
- volume nomeado preserva usuários após restart/recriação;
- suites backend/frontend/contrato/E2E executam somente em Docker;
- smoke/E2E removem exclusivamente projetos efêmeros próprios;
- ao final da execução, não restou container do desafio ou dos projetos efêmeros; o volume principal foi preservado e o outro projeto Compose continuou ativo.

## Revisões independentes e achados

| Eixo | Alto | Médio | Baixo | Disposição |
|---|---:|---:|---:|---|
| Segurança | 0 | 2 | 1 | Bind fora do loopback e afirmação de CSP corrigidos; ausência de rate limiting aceita apenas para demo local. |
| Testes/CI/E2E | 0 | 0 | 1 | `forbidOnly` não explícito no Playwright; sem `.only`/skip no HEAD, aceito como hardening futuro. |
| Docker/entrega | 0 | 0 | 3 | Pré-requisitos de shell e versão Compose documentados; Nginx sem `USER` explícito permanece baixo no container não privilegiado. |
| SDD | 0 | 3 | 4 | Artefatos M6, convenção de estado e relato do reset corrigidos; imprecisões baixas também encerradas. |

Os eixos compartilham alguns achados; o total único foi 0 Alto e 5 Médios.
Depois das correções e da reexecução relevante: **0 Alto e 0 Médio abertos**.

## Rastreabilidade e estados que não podem ser promovidos

- 19 requisitos funcionais, 14 não funcionais, 18 premissas e 40 critérios continuam mapeados em [`06-traceability.md`](06-traceability.md).
- `DOC-RUN-001` e os gates técnicos/operacionais de M6 estão Verified por este relatório e pelo README reproduzido.
- O roteiro `DOC-EXPLAIN-001` existe no README; a habilidade do responsável humano (`AI-EXPLAIN-01`) permanece **Pending human confirmation**.
- `DEL-REPO-01` permanece **Pending**: não há remote configurado e nenhum push foi autorizado.
- A estrutura do GitHub Actions foi validada localmente; a execução hospedada permanece não observada até a publicação.

## Limitações residuais aceitas

- HTTP/Swagger/Development são adequados somente à demonstração;
- o bind é local, mas não há CSP, rate limiting ou lockout; exposição externa
  exige TLS e hardening desses controles;
- SQLite, migration no startup e uma instância não constituem arquitetura de produção escalável;
- token em `sessionStorage` é acessível a JavaScript e não há revogação/refresh;
- chave JWT efêmera invalida sessões no restart quando nenhum segredo externo é fornecido;
- E2E usa somente Chromium e o Playwright ainda não bloqueia `.only` por configuração;
- Nginx usa o modelo de usuário padrão da imagem oficial; hardening adicional fica para ambiente de produção;
- CI hospedada e capacidade humana de explicar a solução dependem de ações externas à auditoria.

## Conclusão

A entrega está funcional, coerente com o OpenAPI/ADRs, reproduzível somente com Docker e pronta para associação a um repositório GitHub e publicação pelo responsável. A publicação em si não foi executada.

## Adendo — revisão independente posterior de M6

Esta seção preserva a auditoria acima como evidência da conclusão original de M6 e registra a revisão posterior exigida sobre seu snapshot documental.

- **Snapshot revisado:** `ee2933d5d880f9ea0a401a39fffa7fec43e5c0a0`.
- **Base do diff:** `3f6fbc4b006c3a0ebbe83cf3617c8a924a16e798`.
- **Worktree inicial:** limpa, na branch `main`.
- **Escopo:** diff integral de oito arquivos de M6 e leitura acumulada de governança, SDD/ADRs, backend, frontend, testes, Docker, scripts, CI, locks e cinco commits recentes.
- **Método:** revisões independentes de correção/segurança, stale, simplicidade e KISS antes de editar; testes vermelhos antes do código; re-revisão completa do patch.

### Achados e disposição

| ID | Severidade | Evidência e impacto | Disposição |
|---|---|---|---|
| `REV-M6-001` | Medium | `AC-DASH-02` estava marcado como Verified, mas o guard só atuava na ativação. Após `exp`, dashboard/perfil e dados pessoais já renderizados permaneciam visíveis; uma nova chamada era enviada sem Bearer e seu `401` escapava da navegação global. A API seguia segura, sem bypass server-side. | Critério/design/testes atualizados; `AuthService` agenda `exp` por token e reprotege rota ativa; interceptor cancela request protegida sem token e conduz ao login; sessão posterior e rota pública são preservadas. |
| `REV-M6-002` | Medium | Durante a re-revisão, as afirmações correntes ainda descreviam o baseline de 57 testes e ausência de mudança da aplicação, contrariando o patch e `DOC-TRACE-01`. | Evidência histórica M3–M6 preservada e rotulada; plano, matriz, relatório, índice, uso de IA e review log ganharam estado corrente separado com 64 testes. |
| `REV-M6-003` | Low | O novo timer era cancelado em logout/troca/expiração, mas sobrevivia à destruição do injector, deixando callback/closure satélite até `exp`. | `AuthService` implementa `OnDestroy` e cancela somente o timer, sem remover a sessão; teste dedicado aprovado. |
| `REV-M6-004` | Low | A classificação textual de rota ignorava query/fragment, mas não matrix params; `/dashboard;tab=resumo` e `/profile;section=password` perdiam a sessão em `exp` sem navegar, mantendo o DOM protegido. | A URL é normalizada também em `;`; dois testes vermelhos e depois verdes cobrem dashboard/perfil com matrix params, query e fragment. |

Consolidação desta revisão e de sua re-revisão: **0 High, 2 Medium e 2 Low confirmados; todos corrigidos. 0 achado aberto ou bloqueado.** As lentes stale e KISS não encontraram resíduo anterior ou complexidade desnecessária no snapshot; os três achados satélites acima surgiram e foram encerrados ao re-revisar o patch corretivo.

### Evidência executada

| Comando ou gate | Resultado observado |
|---|---|
| Checks estáticos iniciais | `git diff --check ee2933d^ ee2933d`, `docker compose config --quiet` e `bash -n scripts/*.sh` aprovados; status inicial limpo. |
| Profile `contract-tests` | OpenAPI aprovado: 6 operações e 53 referências locais. |
| Profile `backend-tests` | Restore/build Release e 101/101 integrações aprovados, sem falha ou skip. |
| Baseline do profile `frontend-tests` | Lint, 57/57 testes e build aprovados antes da correção. |
| Regressão focada antes do código | 3 falhas e 21 sucessos: timer/redirect ausentes e request anônima reproduzida. |
| Segunda regressão focada | 2 falhas/8 sucessos reproduziram matrix params impedindo navegação; após a correção, os 10 testes do `AuthService` passaram. |
| Testes de autenticação finais | 27/27 aprovados, incluindo `exp`, parâmetros de URL, races, rota pública, cancelamento HTTP e lifecycle. |
| Profile `frontend-tests` final | Lint, 64/64 testes em 9 arquivos e build de 318,32 kB bruto/87,94 kB estimado aprovados. |
| `e2e-playwright.sh` | 3/3 jornadas aprovadas em Chromium; recursos efêmeros removidos. |
| `validate-m1-compose.sh` | Origem, cadastro/login, perfil/senha, autorização, persistência, `413/415/503`, logs e cleanup aprovados. |
| `actionlint:1.7.12` | Workflow aprovado no container fixado. |

### Riscos e pendências preservados

- A API continua validando assinatura, issuer, audience, claims e `exp`; a decodificação/timer do browser serve somente à experiência. Timers podem ser processados no primeiro ciclo disponível após suspensão da aba ou do sistema.
- JWT em `sessionStorage`, ausência de refresh/revogação, HTTP local, SQLite de instância única, CSP/rate limiting/lockout e hardenings de produção continuam com as limitações já descritas acima.
- CI hospedada, `AI-EXPLAIN-01` e `DEL-REPO-01` permanecem Pending; nenhum push, remote ou confirmação humana foi fabricado.

**Resultado corrente:** aprovado localmente, com 0 High, 0 Medium e 0 Low abertos após a revisão pós-M6.

## Adendo — revisão completa e correções posteriores

Uma revisão completa posterior fixou o snapshot `a73d33bdfcabb2a512ebad32b013a3b7db9b89e6` e aplicou as lentes de correção/segurança, stale, simplicidade e KISS. O primeiro passe encontrou dois achados acionáveis; a re-revisão do patch encontrou ainda um resíduo satélite do problema de timeout. Todos foram corrigidos sem ampliar funcionalidade de negócio, e nenhum defeito de segurança foi confirmado:

| Achado | Evidência | Correção |
|---|---|---|
| `P2 / Correctness` — gate frontend instável | O comando Docker oficial aprovou lint, mas terminou com 57/64 testes por sete timeouts; a repetição na mesma imagem passou 64/64. | `vitest.config.ts` define timeout global de 30 segundos, carregado pelo builder Angular e copiado para o target Docker; não há retry nem aumento do timeout externo da CI. |
| `Low / Stale` — overrides locais anulavam parte da correção | A re-revisão encontrou dois argumentos `10_000` em `app.spec.ts`; o Vitest prioriza o timeout local sobre o global. | Os overrides obsoletos foram removidos para que todos os testes herdem os mesmos 30 segundos. A busca final não encontrou outro timeout local. |
| `Simplicity` — decoder de `ProblemDetails` triplicado | Cadastro, autenticação e perfil mantinham tipos e pipelines defensivos idênticos, introduzidos em milestones diferentes. | Um módulo `core/http/problem-details.ts` passou a ser a única fonte do tipo/parser. Não foi criado service base, wrapper HTTP ou nova dependência; o helper `isRecord` do JWT permaneceu local por ter responsabilidade distinta. |

### Evidência executada nesta correção

| Comando ou gate | Resultado observado |
|---|---|
| `docker compose --profile frontend-tests run --rm --build frontend-tests`, após remover os overrides | Runner declarou carregar `vitest.config.ts`; lint, 67/67 testes em 10 arquivos e build de 318,10 kB bruto/87,84 kB estimado passaram; runner de teste em 4,09 s. |
| Duas execuções simultâneas de `docker compose --profile frontend-tests run --rm frontend-tests` | Ambas aprovaram lint, 67/67 testes e build; runners de teste em 4,38 s e 5,65 s sob contenção. |
| Profile `backend-tests` | Restore/build Release e 101/101 integrações aprovados, sem falha ou skip. |
| Profile `contract-tests` | OpenAPI aprovado: 6 operações e 53 referências locais. |
| `docker compose config --quiet` | Configuração aprovada. |
| Re-revisão independente do diff final | Correção/segurança e stale encerraram com zero finding acionável; a lente de simplicidade já havia aprovado a forma direta, sem redução segura adicional. |

E2E e smoke funcional não foram repetidos nesta correção porque rotas, templates, API, proxy e contrato de negócio não mudaram; suas últimas evidências verdes permanecem registradas acima. CI hospedada, `AI-EXPLAIN-01` e `DEL-REPO-01` continuam Pending.

**Resultado corrente após a revisão completa:** os dois achados principais e o resíduo satélite foram corrigidos; 0 achado aberto ou bloqueado.

## Adendo — mutation testing crítico do backend após M6

`BE-MUT-001` e a infraestrutura local de `CI-MUT-001` foram implementados em 2026-08-27. A revisão DB de 2026-08-28 alterou lógica interna coberta pela allowlist e acrescentou duas regressões, por isso a baseline foi recalibrada; endpoint, status HTTP, schema/migration e frontend permaneceram iguais. Esta evidência é histórica e foi sucedida pelo adendo de queries/startup ao fim deste relatório. A execução hospedada continua Pending até o workflow ser publicado e observado.

### Implementação confrontada

- tool manifest raiz fixa `dotnet-stryker` `4.16.0`;
- `stryker-config.json` usa `Release`, `net10.0`, nível `standard`, análise `perTest`, dois workers, timeout adicional de 5 segundos, falha para suíte inicial vermelha e reporters progress/HTML/JSON;
- a allowlist contém exatamente os onze arquivos aprovados de autenticação, perfil, JWT, health e configuração EF; migrations, DTOs passivos, Swagger, entidade/DbContext e `Program.cs` não recebem mutante ativo;
- o target/profile `mutation-tests` não sobe a aplicação, não publica porta e grava em `${MUTATION_ARTIFACTS_DIR:-./artifacts/mutation}`;
- o runner valida o JSON depois do Stryker e reprova relatório ausente, `Timeout`, `NoCoverage`, `RuntimeError` ou quantidade de `CompileError` diferente dos três mutantes inválidos classificados;
- o workflow separado possui somente disparo manual e cron de segunda-feira às `06:00 UTC`, runner `ubuntu-24.04`, limite de 90 minutos, `contents: read`, projeto Compose exclusivo, Actions por SHA, cleanup sem `--volumes` e upload por 14 dias.

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release reutilizado sem warning/erro; 113/113 integrações passaram, 0 falha e 0 skip, em 6 s na reexecução final. |
| Baseline temporária com `thresholds.break = 0`, recalibrada após a correção DB | 473 mutantes foram criados e 198 executados; 193 killed, 5 survived, 108 ignored, 3 `CompileError`, 0 `NoCoverage`, 0 timeout e 0 erro de execução; `S = 97,47%` em `00:03:21`. |
| `docker compose --profile mutation-tests run --rm --build mutation-tests`, configuração final | Execução definitiva da imagem com o gate incorporado: 193 killed, 5 survived, 108 ignored, 3 `CompileError` classificados, 0 `NoCoverage`, timeout ou erro de execução, score `97,47%`; HTML/JSON gerados; gate do relatório aprovado; exit code 0 em `00:03:00`. |
| Prova negativa do gate | Um passe sob contenção produziu score `97,93%`, mas 12 timeouts; o Stryker retornou sucesso pelo score e o gate adicional encerrou o comando com exit code 1. Esse relatório foi rejeitado e substituído pela execução limpa. |
| `docker compose --profile mutation-tests config --quiet` | Configuração renderizada aprovada sem `.env`. |
| `docker run --rm --volume "$PWD:/repo:ro" --workdir /repo rhysd/actionlint:1.7.12` | Workflows aprovados sem diagnóstico. |
| `./scripts/validate-m1-compose.sh` | Inventários, builds, origem única, migrations, cadastro/login/perfil/senha, autorização, persistência, `413/415/503`, logs e cleanup isolado aprovados. |
| Auditoria do JSON/HTML | Contagens exatas; zero mutante ativo fora da allowlist; reporters presentes; nenhum padrão de JWT compacto, chave privada, valor Bearer ou arquivo SQLite. `artifacts/` continua ignorado e nenhum relatório/banco é rastreado pelo Git. |
| `docker compose --profile mutation-tests down --remove-orphans` | Removeu somente a rede residual do projeto de mutação. O volume `user-profile-sdd-challenge_user-profile-data` foi observado antes e depois; nenhum container/rede desse projeto permaneceu. |

O host compartilhava o daemon com containers de outros projetos em reinício e produziu passes intermediários com 25, 7 e 12 timeouts. Eles não foram promovidos. A repetição limpa com os mesmos dois workers e os mesmos 5 segundos comprova que não havia timeout intrínseco no alvo; nenhum container ou volume externo foi interrompido para obter o resultado.

### Baseline, ratchet e escopo

O score limpo daquela execução, `S = 193 / (193 + 5) = 97,47%`, resultou em `floor(S) = 97`; logo, `break = 97`, `low = 97` e `high = 97`. O valor temporário zero não permanece na configuração. Não há `ignore-mutations` nem `ignore-methods` global.

O JSON do Stryker preserva algumas fontes fora da allowlist como `Ignored` pelo mutate filter; a auditoria encontrou zero mutante killed, survived, timeout, `NoCoverage` ou `CompileError` fora dos onze alvos. Os quatro arquivos de request estão explicitamente selecionados, mas o nível `standard` do Stryker `4.16.0` não gerou mutante executável em seus atributos/auto-properties.

Os 108 ignored daquele relatório incluem 56 removidos pelo mutate filter, 51 pelo filtro de bloco já coberto e uma exclusão pontual. Os três `CompileError` são mutações C# inválidas: tornar a comparação de quantidade esperada impossível (`Count < 0`) e trocar `Count` por `Sum` sobre IDs de migration em `DatabaseHealthCheck`; e remover o initializer obrigatório usado por `JwtBearerConfiguration`.

### Classificação dos cinco survivors

| Arquivo / mutação | Classificação e justificativa |
|---|---|
| `AuthController.Register` — remover o precheck de email duplicado | Equivalente no contrato: o índice único continua como autoridade e o catch da violação SQLite devolve o mesmo `409`. O precheck permanece pela resposta amigável no caminho comum. |
| `AuthController.Login` — substituir o argumento `user` do hasher pelo usuário fictício | Equivalente com o `PasswordHasher<User>` fixado: a implementação não consulta o objeto `User`; hash e senha fornecidos continuam os mesmos. |
| `ProfileController.UpdateCurrent` — remover o precheck de conflito de email | Equivalente no contrato: o índice único/catch preserva o mesmo `409` e nenhuma atualização parcial é persistida. |
| `JwtBearerConfiguration.TryGetSingleClaim` — retornar true para `principal is null` | Equivalente no chamador: o parâmetro `out` inicial fica vazio e a conversão obrigatória de Guid/timestamp ainda rejeita o token. |
| `JwtBearerConfiguration.TryGetSingleClaim` — retornar true para claim ausente, duplicada ou vazia | Equivalente pelo mesmo parse obrigatório do valor vazio; o token continua rejeitado. |

A única exclusão pontual cobre a mutação de string na inicialização do parâmetro `out`: no retorno falso os chamadores ignoram o valor e, no retorno verdadeiro, ele é sempre sobrescrito pela claim. A justificativa fica junto ao código. Nenhuma regra de negócio foi alterada para matar mutante; survivors equivalentes continuam visíveis e compõem a baseline.

### Segurança, limites e estado

Os reporters carregam o código-fonte versionado dos testes e, portanto, exibem nomes de campos e fixtures sintéticas já públicas; não capturam valores runtime. A varredura final não encontrou JWT compacto, chave privada, valor Bearer longo, conteúdo/arquivo SQLite ou segredo real. Logs e respostas da aplicação continuam sem senha, hash, token ou chave.

`BE-MUT-001` está Verified localmente. `CI-MUT-001` está Verified quanto a configuração, execução Docker, actionlint, isolamento, cleanup e geração/upload previsto de artefatos; sua execução hospedada permanece Pending. Nenhum relatório foi versionado, e nenhum commit ou push foi produzido nesta atividade.

## Adendo — refinamento visual do frontend após M6

Em 2026-08-28, o shell e as telas de login, cadastro, dashboard e perfil receberam somente refinamento de apresentação. A referência pública da Lekto orientou ritmo, contraste e uso de cor; marca, textos, fontes, imagens e ativos não foram copiados. Um ajuste final retirou do dashboard os três cartões sem ação que apenas descreviam dados pessoais, senha e sessão. API, OpenAPI, banco, autenticação, payloads e regras de negócio permaneceram inalterados.

### Implementação confrontada

- tokens globais próprios, tipografia do sistema e componentes Material preservados;
- autenticação em composição dividida no desktop e uma coluna no mobile;
- dashboard com hero, CTA principal e resumo dos campos já devolvidos por `GET /api/profile`, sem os cartões descritivos redundantes;
- perfil sem exposição do identificador técnico e com os dois formulários existentes em cartões separados;
- labels, nomes de ações, hierarquia de headings, `aria-live`, `aria-busy`, foco, loading e bloqueio de submissão preservados.

### Evidência executada

| Comando ou gate | Resultado observado |
|---|---|
| Regressão frontend antes da implementação | Lint aprovado e falha única esperada: 66/67 testes passaram; `FE-PROF-001` encontrou o UUID ainda presente no DOM. |
| `docker compose --profile frontend-tests run --rm --build frontend-tests` | Após remover somente o bloco/estilos do identificador e o alias de template sem uso, lint aprovado; 67/67 testes em 10 arquivos; build de 327,52 kB bruto/90,27 kB estimado, sem warning de budget. |
| `./scripts/e2e-playwright.sh` | 3/3 jornadas oficiais aprovadas em 6,5 s; `E2E-001` confirmou nome/email carregados e ausência de “Identificador da conta”, com projeto/volume isolados e cleanup concluído. |
| Navegador real em `http://localhost:8080` | Login e cadastro foram inspecionados em 1280 px/360 px; após o ajuste final, o perfil autenticado foi revalidado em 1200 px/360 px com dois formulários, um `h1`, dois `h2`, nenhum label/UUID técnico, nenhum overflow e nenhum warning/error de console. |
| Capturas temporárias Playwright | Dashboard e composição responsiva do perfil foram confrontados durante o refinamento; a ausência final do identificador foi validada diretamente no DOM e no E2E, sem depender das capturas anteriores. Os artefatos continuam ignorados. |
| Regressão da simplificação do dashboard | Depois de atualizar requisitos/design/estratégia e acrescentar `FE-DASH-001`, o primeiro profile aprovou lint e 67/68 testes; a única falha encontrou os três textos antigos no DOM, como esperado. |
| `docker compose --profile frontend-tests run --rm --build frontend-tests` após a simplificação | Lint aprovado; 68/68 testes em 10 arquivos; build de 327,52 kB bruto/90,29 kB estimado. O teste novo preserva saudação, resumo, navegação e logout enquanto exige a ausência dos três textos. |
| `./scripts/e2e-playwright.sh` após a simplificação | 3/3 jornadas oficiais aprovadas em 5,0 s; `E2E-001` manteve navegação, edição, retorno ao dashboard, logout e ausência de overflow em 360 px; recursos efêmeros foram removidos. |
| Navegador real no dashboard publicado | O serviço `web` foi recriado sem alterar API/volume. Em 1280×720, o DOM apresentou um `h1`, um `h2`, saudação, resumo do perfil, ações de perfil/logout, nenhum dos três textos e nenhum overflow. A conta sintética foi criada pela UI, encerrada por logout e suas credenciais ficaram somente na sessão de validação. |

O teste temporário usado somente para as capturas autenticadas foi removido antes da suíte oficial; os artefatos permanecem no diretório ignorado `artifacts/`. Nenhum segredo real, password runtime, JWT ou banco foi adicionado ao Git. A aplicação principal permaneceu disponível em `http://localhost:8080` com seu volume preservado.

**Resultado:** `FE-PROF-001`, `FE-DASH-001`, `FE-VISUAL-001`, `AC-PROF-01`, `AC-DASH-03`, `FR-UI-01` e `PREM-FE-01` estão Verified localmente para o refinamento visual pós-M6. O `id` permanece no contrato/API, mas não no DOM; o dashboard preserva apenas conteúdo e ações úteis, e nenhuma funcionalidade nova foi introduzida.

## Adendo — correção responsiva após revisão

Em 2026-08-28, uma revisão somente leitura focada em responsividade encontrou dois P2 e um P3 no refinamento visual: login/cadastro inteiramente abaixo da primeira viewport em landscape curto; ordem visual inversa à sequência de foco nas ações móveis; e crescimento vertical desproporcional do dashboard com o nome válido de 200 caracteres. A especificação criou `UI-RESP-01` antes dos testes e do código.

### Correção aplicada

- em viewport de até `52rem × 36rem`, o painel editorial não essencial da autenticação sai do fluxo e o cartão funcional permanece prioritário;
- abaixo de `30rem`, as ações usam `column`, igualando ordem visual, DOM e Tab;
- o nome continua integral no DOM/API/perfil, mas o dashboard limita visualmente a saudação a três linhas e o resumo a duas;
- nenhuma rota, regra, payload, estado, dependência, TypeScript, backend, OpenAPI, banco ou Compose foi alterado.

### Evidência executada

| Comando ou gate | Resultado observado |
|---|---|
| Primeira execução de `./scripts/e2e-playwright.sh` com a regressão completa e CSS antigo | Exit 1 esperado: heading do cadastro recebeu viewport ratio `0` em `667×375`; E2E-002/003 passaram. |
| Execução após corrigir somente landscape | Exit 1 esperado: sequência Tab seguiu o DOM, mas o link secundário aparecia em `y=677` e o botão posterior acima, em `y=620`; E2E-002/003 passaram. |
| Execução após alinhar somente a ordem móvel | Exit 1 esperado: nome integral permaneceu no DOM, mas sem clamp/altura limitada; E2E-002/003 passaram. |
| Execução E2E final após re-revisão dos oráculos | 3/3 jornadas aprovadas em 5,0 s, sem retry ou jornada adicional; login, cadastro, dashboard e perfil foram percorridos em 320 px, e o formulário completo ficou alcançável em landscape curto. |
| `docker compose --profile frontend-tests run --rm --build frontend-tests` | Lint aprovado; 68/68 testes em 10 arquivos; build de 327,59 kB bruto/90,31 kB estimado. |
| Recriação isolada do serviço `web` e `curl --fail http://localhost:8080/health` | Nginx/frontend corrente publicado; API e volume preservados; health saudável. |
| Navegador real e asserts reforçados | Em `667×375`, login/cadastro apresentaram heading e primeiro campo dentro da viewport, painel editorial oculto e zero overflow; o E2E também rolou até o último campo e submit, exigindo ambos visíveis e habilitados. Em `320×568`, as quatro telas ficaram sem overflow, as ações seguiram a ordem DOM/Tab e, com nome de 200 caracteres realmente visível e sem recorte lateral pelo viewport ou ancestral, hero/resumo ficaram em três/duas linhas e `Ir para o perfil`/`Sair` terminaram na primeira viewport. |

As execuções E2E usaram projetos/volumes efêmeros e o cleanup do script os removeu. A inspeção publicada criou somente contas sintéticas no volume local da demonstração; logout encerrou as sessões, credenciais foram descartadas e nenhum segredo, token, relatório ou banco foi versionado.

**Resultado:** `UI-RESP-01`, `FE-VISUAL-001`, `FR-UI-01` e `PREM-FE-01` estão Verified localmente. Os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 42 critérios então correntes permaneciam rastreados; nenhuma funcionalidade de negócio nova foi criada.

## Adendo — revisão focada em banco e correções

Em 2026-08-28, uma revisão somente leitura focada em constraints, concorrência, migrations e limites operacionais encontrou dois P2 e dois P3. As correções foram precedidas por `AC-PASS-05` e pelos testes correspondentes; não criaram endpoint, status HTTP, migration, tabela, coluna ou funcionalidade de negócio.

### Achados e disposições

| Achado | Severidade | Correção aplicada | Evidência |
|---|---|---|---|
| Duas trocas simultâneas aceitavam a mesma senha atual e ambas retornavam `200`, com last-writer-wins. | P2 | `PUT /api/profile/password` passou a atualizar por compare-and-swap de `Id + PasswordHash` observado; zero linha afetada devolve o mesmo `400 ValidationProblemDetails` de senha atual incorreta. | `BE-PASS-005` sincroniza ambas as escritas: exatamente um `200`, um `400`, somente a senha vencedora autentica e os demais dados permanecem íntegros. |
| A connection string do Compose sobrescrevia o timeout defensivo do appsettings e deixava o SQLite em 30 s, igual ao timeout do proxy. | P2 | O Compose fixa `Default Timeout=5`; o health usa timeout próprio de 1 s e o Nginx permanece em 30 s. | Configuração renderizada e assert do smoke confirmaram a margem `1 < 5 < 30`. |
| O health considerava somente a quantidade de migrations aplicadas. Um histórico diferente com a mesma contagem poderia ser declarado saudável. | P3 | O check lê `MigrationId` e exige igualdade exata, ordinal, com o conjunto conhecido pelo assembly. | `BE-HEALTH-001` inseriu IDs inesperados na mesma quantidade e passou a receber `Unhealthy`. |
| A corrida de email duplicado verificava os status e a cardinalidade, mas não o corpo da resposta concorrente. | P3 | O teste reutiliza o assert completo de conflito no response `409` real da corrida. | `BE-REG-004` comprova `application/problem+json`, `status`, `title`, `detail` e `instance`. |

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| Regressões focadas antes do código | 1/3 passou e 2/3 falharam como esperado: health retornou `Healthy` para IDs divergentes e as duas trocas retornaram `200`. |
| Regressões focadas após o código | 3/3 passaram: corrida de cadastro, histórico divergente e CAS de senha. |
| `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release com 0 warnings/erros; 113/113 integrações, 0 falha e 0 skip, em 6 s na reexecução final. |
| Profile `contract-tests` | OpenAPI aprovado com seis operações e 53 referências locais. |
| `docker compose --profile mutation-tests config --quiet` e configuração renderizada | Aprovados sem `.env`; connection string contém `Default Timeout=5`. |
| `./scripts/validate-m1-compose.sh` | Inventário, builds, origem única, migrations, health, cadastro/login/perfil/senha, autorização, persistência, `413/415/503`, logs e cleanup isolado aprovados. |
| Baseline Stryker temporária | 473 criados, 198 executados, 193 killed, 5 survived equivalentes, 108 ignored, 3 `CompileError` classificados, 0 timeout/`NoCoverage`/erro de execução; 97,47% em `00:03:21`. |
| Profile `mutation-tests` final | Mesmas contagens e score, ratchet 97/97/97, relatórios HTML/JSON, gate aprovado e exit code zero em `00:03:00`. |
| Stack padrão e probes HTTP | `api` e `web` healthy; `/`, `/health` e `/swagger/index.html` responderam `200` em `http://localhost:8080`, com o volume existente preservado. |

### Segurança, escopo e limites

- a senha continua processada apenas pelo `PasswordHasher<User>`; o CAS compara hashes parametrizados pelo EF Core e não inclui senha/hash em logs ou responses;
- a identidade continua vindo somente de `sub`; DTOs e contratos não ganharam `userId` nem campos de entidade;
- o índice único `UX_Users_NormalizedEmail` permanece a constraint autoritativa e nenhuma migration foi necessária;
- a aplicação, OpenAPI e frontend mantêm o mesmo comportamento público; somente a descrição normativa de concorrência foi explicitada;
- o SQLite continua uma escolha proporcional para uma única instância de demonstração. O CAS resolve esta corrida de aplicação, mas não transforma o banco em solução para escrita concorrente de alta escala;
- a execução hospedada semanal/manual de mutation testing permanece Pending até o workflow ser publicado e observado.

**Resultado:** os quatro achados foram corrigidos e revalidados; não restou finding DB alto ou médio dentro do escopo. Ao fim daquela atividade, a baseline era 113 integrações e mutation score de 97,47%, sem timeout ou `NoCoverage`; a evidência corrente está no adendo seguinte.

## Adendo — revisão de queries e recuperação de startup

Em 2026-08-28, a revisão focada nas consultas e no caminho de inicialização encontrou um P2 e três P3. A especificação foi atualizada antes do código com `OPS-DOCKER-04` e os testes `BE-DB-002`, `BE-HEALTH-001` e `BE-PROF-005`. O patch não alterou endpoint, payload, status, schema, migration ou frontend.

### Achados e disposições

| Achado | Severidade | Correção aplicada | Evidência |
|---|---|---|---|
| Um `__EFMigrationsLock` órfão podia reter `MigrateAsync` indefinidamente depois de uma interrupção. | P2 | Sob a premissa documentada de uma única instância, o startup abre a conexão, remove somente a tabela técnica com command timeout de 5 segundos e aplica migrations dentro de deadline total de 15 segundos ligado ao encerramento da aplicação. | Duas factories sobre o mesmo SQLite comprovam recuperação dentro do limite, preservação do usuário e do histórico; schema conflitante continua encerrando o startup com erro. |
| O health chamava `CanConnectAsync` antes de `OpenConnectionAsync`, abrindo a conexão duas vezes. | P3 | A probe abre uma única conexão e reutiliza-a para ler os IDs exatos das migrations. | Interceptor EF sobre SQLite real observa exatamente uma abertura por execução e os cenários saudável/indisponível permanecem verdes. |
| A edição de perfil consultava conflito mesmo quando o email normalizado não mudava. | P3 | O `AnyAsync` só executa quando a nova chave canônica difere da persistida; o índice único e o tratamento da corrida permanecem autoritativos. | Teste direto do controller com SQLite real observa somente a carga obrigatória do usuário, sem segundo round-trip; atualização, duplicidade e colisão concorrente continuam verdes. |
| O teste de migration usava `LIKE '%_InitialCreate'`, em que `_` era wildcard e permitia falso-verde/scan. | P3 | O oráculo lê, ordena e compara o ID completo versionado. | Banco vazio exige exatamente `20260824182132_InitialCreate` e rejeita histórico diferente. |
| A primeira simplificação do precheck removeu a autoexclusão por `Id`; em uma corrida, a própria linha já atualizada podia ser confundida com outra conta e produzir falso `409`. | P2 | A query voltou a excluir o `Id` derivado do `sub`; a condição externa ainda evita toda a query quando a chave canônica não muda. | Dois contexts SQLite mantêm a entidade A rastreada, persistem B em paralelo e confirmam que a request para B retorna `200`, não `409`; conflito de outra conta continua verde. |
| A primeira regressão do lock provava recuperação rápida, mas não matava a remoção do deadline total nem a retirada do token em `MigrateAsync`. | P2 de evidência | Duas variantes do interceptor retêm separadamente a abertura da preparação e a abertura usada por `MigrateAsync`, respeitando somente o token real do startup; cada factory precisa falhar por `TimeoutException` antes da guarda externa. | As duas fases cancelam no deadline; sem `CancelAfter` ou sem o token da aplicação, a variante correspondente excederia a guarda de 20 segundos e falharia. |
| O observer da query reconhecia apenas SQL contendo `EXISTS`, permitindo falso-verde se a implementação trocasse o operador LINQ. | P3 de teste | O interceptor conta qualquer `CommandSource.LinqQuery` depois do arranjo. | A atualização canonicamente inalterada exige exatamente a única carga obrigatória do usuário; qualquer segundo round-trip LINQ reprova. |
| A rastreabilidade atribuía parte de `OPS-DOCKER-04` ao smoke, omitia os gates pós-M6 novos e relacionava o deadline de startup ao timeout de leitura do proxy. | P3 documental | `BE-DB-002` ficou como evidência direta do lock/deadline; a tabela pós-M6 inclui queries/startup; o limite de 15 segundos é descrito como operacional independente. | Matriz, estratégia e design foram reconciliados antes da validação final. |

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| Regressões focadas antes do código | 3/3 falharam como esperado: health abriu duas conexões, perfil fez um precheck redundante e o startup excedeu a guarda externa com lock órfão. |
| Regressões focadas após o código | Recuperação/deadline do lock, falha de schema conflitante, uma abertura no health, única query obrigatória no caso inalterado e corrida da própria conta passaram. |
| `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release aprovado; 119/119 integrações, 0 falha e 0 skip, em 30 segundos. |
| Profile `contract-tests` | OpenAPI aprovado com seis operações e 53 referências locais. |
| `docker compose --profile mutation-tests config --quiet` | Configuração renderizada aprovada sem `.env`. |
| `./scripts/validate-m1-compose.sh` | Na primeira tentativa a porta `127.0.0.1:8080` estava ocupada pela própria stack de demonstração; ela foi pausada sem remover o volume. A repetição passou inventário, origem única, migrations, health, fluxos, autorização, persistência, erros, logs e cleanup isolado; a stack principal foi restaurada. |
| `docker compose --profile mutation-tests run --rm --build mutation-tests` | Execução final em `00:08:23`: 484 mutantes descobertos, 198 executados, 193 killed, 5 survived equivalentes, 119 ignored, 3 `CompileError` classificados, 0 timeout, 0 `NoCoverage` e 0 erro de runtime; score 97,47%, ratchet 97/97/97, HTML/JSON, gate e exit code zero. |
| Probes da stack principal | `api`/`web` saudáveis; `/`, `/health` e `/swagger/index.html` responderam `200` em `http://localhost:8080`, preservando o volume. |

### Baseline, decisões e segurança

O score daquela execução era `S = 193 / (193 + 5) = 97,47%`; `floor(S)` permanecia 97 e a configuração versionada continuava `break/low/high = 97/97/97`. Os mesmos cinco survivors equivalentes seguiam visíveis. A única exclusão pontual era a atribuição inicial do parâmetro `out` em `JwtBearerConfiguration`; nenhuma exclusão foi adicionada para as queries.

Execuções intermediárias não foram promovidas: uma produziu 12 timeouts; outra expôs que a autoexclusão do próprio `Id` precisava de prova concorrente; e o primeiro passe após a regressão ainda produziu um timeout isolado. Uma tentativa de exclusão pontual foi descartada porque ocultava também um mutante não equivalente. A solução final preservou a condição necessária e matou seu mutante com comportamento observável, sem distorcer regra de negócio nem ampliar configuração de ignore.

Os relatórios gerados permaneceram sob `artifacts/`, ignorado pelo Git. A revisão não encontrou senha, hash, JWT, chave, banco ou relatório rastreado. Identidade e escrita de perfil continuam exclusivamente vinculadas ao `sub`; as queries permanecem parametrizadas pelo EF Core. A recuperação automática do lock é adequada somente à instância única desta demonstração e não deve ser transportada para execução multi-instância sem coordenação externa de migrations.

**Resultado daquela etapa:** os achados estavam corrigidos e Verified localmente. A baseline era 119 integrações e mutation score de 97,47%, sem timeout, `NoCoverage` ou erro de execução; 19 requisitos funcionais, 14 não funcionais, 18 premissas e 43 critérios permaneciam rastreados.

O resultado acima é a fotografia histórica da correção de queries. A evidência corrente, após o fechamento da revisão completa, está no adendo seguinte.

## Adendo — fechamento da revisão completa de queries/startup

Em 2026-08-28, três achados P3 da revisão completa foram implementados sem alterar API, schema, migration, frontend ou regra de negócio. O lifecycle de migrations deixou o bloco inline anterior a `app.Run`; os testes de query passaram a reutilizar a infraestrutura HTTP comum. A especificação, a estratégia e a matriz foram atualizadas antes da mudança comportamental.

### Achados e disposições

| Achado | Correção aplicada | Evidência real |
|---|---|---|
| O bloco de migration executava antes de `app.Run`, portanto antes de `ConsoleLifetime` registrar `SIGTERM`. | Um único `DatabaseMigrationStartupService` implementa `IHostedLifecycleService.StartingAsync`. O host registra os sinais antes do lifecycle e o servidor só abre o listener depois dele. | Subprocesso real bloqueado no SQLite recebeu `SIGTERM`, observou o token do host, não registrou prontidão e saiu abaixo de 10 segundos, preservando usuário/histórico e sem lock técnico residual. |
| A justificativa do Stryker chamava `Program.cs` de wiring sem lógica, apesar da rotina operacional crítica inline. | O design agora descreve uma allowlist selecionada, não uma classificação de trivialidade; o novo lifecycle permanece fora do gate de mutação, com responsabilidade e cobertura de integração/processo explícitas. | Somente a allowlist tem mutantes ativos no relatório; os demais arquivos aparecem apenas como metadados, e lifecycle/`Program.cs` têm zero ativos. Nenhum ignore global ou pontual novo foi adicionado. |
| Dois testes de query recriavam escopo/DI/SQLite manual apesar de `ApiFactory.WithInterceptor`. | Health e atualização canonicamente inalterada usam cliente HTTP real e a factory compartilhada; somente a corrida com estado rastreado deliberadamente obsoleto mantém dois `DbContext`. | Endpoint de health observa uma abertura; PUT de perfil observa uma única query LINQ e resposta/payload reais; corrida da própria conta permanece verde. |

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| Teste focado inicial do `SIGTERM` | Falhou porque o primeiro oráculo aguardava a mensagem genérica de shutdown, que só aparecia depois de a chamada nativa SQLite sair por command timeout. O oráculo foi corrigido para observar diretamente o callback seguro do token do host; timeout, teste ou produto não foram afrouxados. |
| Testes focados finais (`BE-DB-002/003`, health e perfil) | 7/7 aprovados em 31 segundos; o teste de `SIGTERM` isolado aprovou em 1 segundo. |
| `docker compose --profile backend-tests run --rm backend-tests` | Build Release e 120/120 integrações, 0 falha e 0 skip, em 31 segundos. |
| `docker compose --profile contract-tests run --rm contract-tests` | OpenAPI aprovado: `SPEC-OAS-001..005`, seis operações e 53 referências locais. |
| `docker compose --profile mutation-tests config --quiet` | Configuração renderizada aprovada sem `.env`. |
| `./scripts/validate-m1-compose.sh` | Origem única, migrations, health, cadastro/login/perfil/senha, autorização, persistência, erros, logs e cleanup isolado aprovados. |
| `docker compose --profile mutation-tests run --rm --build mutation-tests` | Exit 0 em `00:04:30`: 491 mutantes descobertos, 198 executados, 193 killed, 5 survived equivalentes, 106 ignored, 3 `CompileError` classificados, 0 timeout, 0 `NoCoverage` e 0 erro de execução; score 97,47%, ratchet 97/97/97 e HTML/JSON aprovados. |
| Probes da stack restaurada | `/`, `/health` e `/swagger/index.html` responderam `200` em `http://localhost:8080`; `api` e `web` permaneceram ativos, com o health transitivo da origem aprovado. |

### Baseline, limites e segurança

A variação de 484 para 491 mutantes descobertos reflete o novo arquivo operacional; a queda de 119 para 106 ignored reflete a remoção do bloco de `Program.cs`, já fora da allowlist. Os 198 mutantes executados e a classificação observável não mudaram: `193 / (193 + 5) = 97,47%`. O ratchet permanece 97/97/97.

O token de cancelamento interrompe operações assíncronas cooperativas, mas não consegue preemptar imediatamente toda chamada síncrona nativa do SQLite. Por isso o command timeout de 5 segundos continua sendo a segunda barreira e o processo encerra bem antes do deadline total de 15 segundos após a liberação do lock no teste. Essa limitação é aceitável somente na instância única de demonstração.

Relatórios ficaram sob `artifacts/`, ignorado pelo Git. Logs e relatórios foram revistos sem senha, hash, JWT, chave ou banco versionado. Nenhum endpoint, payload, status, regra de autorização ou dado persistido foi alterado.

**Resultado daquela etapa:** os três achados P3 foram encerrados. A suíte tinha 120 integrações e a baseline Stryker era 97,47%, sem timeout, `NoCoverage` ou erro de execução. A evidência corrente está no adendo seguinte.

## Adendo — fortalecimento dos oráculos de startup e queries

Em 2026-08-30, a revisão completa encontrou duas lacunas P2 no teste de lifecycle e uma lacuna P3 no observer de queries. As três permitiam falsos-verdes sem indicar defeito no comportamento de produção corrente. A estratégia e a rastreabilidade foram atualizadas antes dos testes; API, schema, migration, lifecycle e frontend permaneceram inalterados.

### Achados e disposições

| Achado | Correção aplicada | Prova discriminante |
|---|---|---|
| A asserção de ausência de `"Now listening on"` não observava a mensagem porque `Microsoft.Hosting.Lifetime` estava filtrado em `Warning`. | O subprocesso de teste habilita `Information` somente para essa categoria e preserva a asserção após a saída. | Mover temporariamente o corpo de `StartingAsync` para `StartedAsync` abriu o listener e reprovou `Assert.DoesNotContain`; restaurado o lifecycle, o teste passou. |
| O teste de `SIGTERM` observava o token bruto do host e liberava o lock antes de comprovar o token consumido pela migration. | Um teste direto do lifecycle captura o token recebido pela abertura usada por `MigrateAsync`, cancela o token chamador e exige propagação imediata e `OperationCanceledException`. | Substituir temporariamente o CTS ligado por um CTS independente reprovou a asserção em menos de um segundo; o caminho correto passou sem aguardar o deadline de 15 segundos. |
| O observer interceptava apenas `ReaderExecutingAsync`, portanto um precheck síncrono redundante escapava. | `ReaderExecuting` e `ReaderExecutingAsync` chamam o mesmo contador de `CommandSource.LinqQuery`. | Com o observer anterior, o precheck síncrono descartável ficou verde; com o callback novo, falhou com duas queries observadas. O caminho correto permaneceu em uma. |

### Comandos e resultados observados

| Gate | Resultado |
|---|---|
| Testes e mutantes focados | Os três caminhos corretos passaram; `StartedAsync`, CTS independente e precheck síncrono redundante reprovaram pelos oráculos esperados. Todos os mutantes ficaram somente em diretórios temporários e foram removidos. |
| `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release com 0 warnings/erros; 121/121 integrações, 0 falha e 0 skip, em 32 segundos. |
| `docker compose --profile mutation-tests run --rm --build mutation-tests` | Exit 0 em `00:08:36`: 492 mutantes descobertos, 200 executados, 195 killed, 5 survived equivalentes, 105 ignored, 3 `CompileError` classificados, 0 timeout, 0 `NoCoverage` e 0 erro de runtime; score 97,50%, ratchet 97/97/97 e gate do JSON aprovado. |

### Limites e simplicidade

A lente KISS manteve as correções no nível responsável: um override de log somente no subprocesso, um teste direto reutilizando o interceptor existente e dois callbacks EF convergindo para um helper local. Não foram criados porta fixa, probe TCP, hook de produção, abstração de lifecycle ou mecanismo de deduplicação de comandos. O subprocesso real e o cenário com SQLite foram preservados porque protegem diretamente `OPS-DOCKER-04`.

Relatórios continuam sob `artifacts/`, ignorado pelo Git. Nenhuma senha, hash, JWT, chave ou banco foi versionado. Execução hospedada da CI, confirmação humana e publicação continuam externas e não foram promovidas a Verified.

**Resultado daquela rodada:** os três falsos-verdes foram encerrados. A evidência foi sucedida pelo fechamento adicional abaixo.

## Adendo — fechamento do shutdown durante startup

Durante a organização dos commits em 2026-08-30, o oráculo de `SIGTERM` passou a exigir exit code zero. A primeira execução reprovou com código `134`: o host observava o sinal e não abria o listener, mas a `OperationCanceledException` do startup ainda chegava sem tratamento à fronteira do processo.

A correção preserva a exceção dentro de `StartingAsync`, garantindo que o Host aborte antes de iniciar o Kestrel. `Program.cs` captura apenas o cancelamento quando `ApplicationStopping` está ativo e `ApplicationStarted` ainda não ocorreu. Deadline continua `TimeoutException`; falha SQLite e cancelamento após prontidão continuam propagando.

| Gate | Resultado observado |
|---|---|
| Prova negativa e teste focado | Sem a normalização, `SigtermCancelsMigrationLifecycleBeforeApplicationReadiness` falhou com exit `134`; no estado final passou em 1 segundo com exit zero, sem listener e sem resíduo técnico. |
| Backend Docker | Build Release com 0 warnings/erros; 121/121 integrações, 0 falha e 0 skip, em 31 segundos. |
| Contrato e operação | OpenAPI com 6 operações/53 referências, `docker compose config --quiet` e smoke completo aprovados. A pilha principal foi restaurada com o volume preservado; `/`, `/health` e `/swagger/index.html` responderam `200`. |
| Mutation testing | Não reexecutado nesta correção: `Program.cs` e o lifecycle permanecem explicitamente fora da allowlist; a baseline limpa aplicável continua 97,50%, sem `NoCoverage`, timeout ou erro de execução. |

**Resultado corrente:** shutdown antes da prontidão agora termina cooperativamente com exit zero. API HTTP, schema, frontend e regras de negócio permanecem inalterados.

## Adendo — rate limiting local de autenticação

Em 2026-08-30, `NFR-SEC-02`, `SEC-RATE-01`, `SEC-RATE-02` e `API-ERROR-02` foram especificados antes do comportamento. A mudança protege somente `POST /api/auth/login` e `POST /api/auth/register` no Nginx, sem middleware backend, serviço Compose, migration, lockout, estado distribuído ou nova funcionalidade de negócio.

### Revisões e correções

As revisões somente leitura não encontraram falha alta. A primeira rodada encontrou quatro P2: sessão/countdown insuficientemente observados no frontend, corpo 429 verificado apenas por fragmentos, `Cache-Control` ausente do OpenAPI e estados SDD conflitantes; também encontrou um P3 de teste por contar cópias textuais de `X-Forwarded-For`. A re-revisão detectou um P2 adicional: mover somente XFF para `server` não funcionava porque outras diretivas `proxy_set_header` nas locations anulavam a herança do conjunto. As rodadas finais encontraram quatro P2 de confiabilidade adicionais: iniciar o parser antes dos probes temporais podia permitir reposição legítima; cadastro ainda não tinha rajada própria; um único login após recriar `web` não distinguia reset de reposição natural; e igualdade exata do JSON rejeitava extensões seguras permitidas por `ProblemDetails`. Todos foram corrigidos: os specs usam sessão-sentinela e relógio falso avançado por um minuto; o smoke executa primeiro os probes temporais, prova 10+1 separadamente nos dois endpoints e novamente após reset, e depois valida os cinco campos normativos mais a ausência de dados sensíveis no corpo salvo; contrato/runtime exigem os dois headers; os estados foram reconciliados; e Host/XFF/Proto são herdados juntos sem override nas locations.

Um apontamento adicional propôs aceitar qualquer distribuição equivalente de `proxy_set_header`. Ele foi classificado como flexibilidade futura, não defeito: o gate congela deliberadamente a forma simples aprovada — os três headers juntos em `server` e nenhum override nas locations — porque foi justamente uma distribuição parcial que causou a falha real de herança. Uma futura refatoração equivalente deve atualizar o oráculo junto com sua prova runtime.

A primeira execução frontend após a revisão falhou porque o spy genérico também observou timers internos de 0 ms do Angular; a tentativa seguinte com `fakeAsync` foi incompatível com o runner Vitest sem `ProxyZone`. O oráculo final usa o relógio falso do próprio Vitest, avança 60.001 ms e exige que todo o texto renderizado, formulário e sessão permaneçam iguais. Não houve retry, aumento de timeout nem enfraquecimento do comportamento exigido.

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| `ruby scripts/validate-openapi.rb` e profile Docker `contract-tests` | `SPEC-OAS-001..006`, seis operações e 56 referências locais aprovadas. |
| Profile Docker `backend-tests` | Build Release com 0 warnings/erros; 121/121 integrações, sem falha ou skip. O Swagger runtime expõe `429`, `Retry-After` e `Cache-Control` somente nas duas operações de autenticação. |
| Profile Docker `frontend-tests` | Lint aprovado; 70/70 testes em 10 arquivos; build de 327,59 kB bruto/90,19 kB estimado. |
| `docker compose --profile backend-tests --profile frontend-tests --profile e2e --profile mutation-tests config --quiet` | Configuração completa aprovada sem `.env`. |
| `./scripts/validate-m1-compose.sh` | Rajadas de 11 logins, 11 cadastros e 11 logins após reset: cada uma produziu 10 respostas `400` da API e um `429` Nginx; JSON/headers, query/XFF/caixa/barra, demais rotas, `413`, persistência e logs seguros foram aprovados. O projeto isolado e seu volume foram removidos. |
| `./scripts/e2e-playwright.sh` | 3/3 jornadas aprovadas em 28,4 s, sem quarta jornada ou seed compartilhado. |
| `docker run ... rhysd/actionlint:1.7.12` | Workflows existentes aprovados sem saída de erro. |
| Profile Docker `mutation-tests` | Exit 0 em `00:18:48`: 513 mutantes descobertos, 200 executados, 195 killed, 5 survived equivalentes, 109 ignored, 3 `CompileError` classificados, 0 timeout, 0 `NoCoverage` e 0 erro; score 97,50% e ratchet 97/97/97. |
| Stack principal restaurada | `api` e `web` ativos/saudáveis no volume preservado; SPA, `/health` e Swagger responderam `200` em `http://localhost:8080`. |

### Resultado e limitações

O bucket usa o endereço TCP real observado pelo Nginx e o endpoint canônico; query, caixa, barra final e `X-Forwarded-For` forjado não renovam a cota. `rate=10r/m`, `burst=9 nodelay` aceita dez tentativas imediatas e o próximo excesso recebe `429 application/problem+json`, `Retry-After: 60` e `Cache-Control: no-store`. O cadastro possui bucket independente, `413` continua anterior/independente e outras rotas/métodos não consomem cota.

O estado é deliberadamente local e efêmero: reiniciar `web` limpa a cota. Não há lockout por conta, coordenação entre réplicas ou proteção distribuída. Essa limitação e o trade-off de `sessionStorage`/ausência de refresh permanecem explícitos no README e ADRs. Relatórios gerados ficaram sob `artifacts/`, ignorado pelo Git; nenhum segredo, banco ou relatório foi versionado.

**Resultado corrente:** `NFR-SEC-02` e seus três critérios estão Verified localmente. CI hospedada, publicação e confirmação humana continuam Pending; nenhum push foi realizado.

## Adendo — fortalecimento dos oráculos do rate limiter

Em 2026-08-30, uma revisão completa do commit `3a169f7` não encontrou defeito qualificável na implementação Nginx, mas confirmou três problemas de sinal em `OPS-RATE-001`: uma diretiva `10r/m` comentada podia mascarar `1r/m` ativo; headers duplicados e conflitantes satisfaziam uma busca por presença; e o teste tratava o texto inglês ilustrativo de `detail` como valor normativo.

### Correção aplicada

- O SDD e a descrição OpenAPI foram atualizados antes do oráculo para explicitar cardinalidade dos headers e semântica genérica de `detail`.
- O inventário duplicado da fonte foi removido. A configuração carregada por `nginx -T` agora é normalizada para excluir comentários e só então exige as linhas ativas exatas de `rate=10r/m`, `burst=9 nodelay`, mapa, status e error handler.
- A resposta HTTP real exige exatamente uma ocorrência case-insensitive de `Retry-After: 60` e `Cache-Control: no-store`; duplicata idêntica ou conflitante reprova.
- `status` deve ser o inteiro `429`; `detail` deve conter um caractere não branco segundo Unicode e continua submetido, junto com extensões, aos scanners recursivos de chaves e valores sensíveis. `type`, `title` e `instance` permanecem confrontados.
- A re-revisão de consistência encontrou que o OpenAPI ainda aceitava qualquer `Retry-After >= 1`, herdava apenas dois campos obrigatórios de `ProblemDetails` e os oráculos não resolviam tipos/nulabilidade efetivos do `allOf`. Contrato, validador, filtro do Swagger runtime e integração foram alinhados para 60 exato, cinco campos obrigatórios/não nulos com seus tipos e `detail` não branco.
- A re-revisão de testes também eliminou falsos sinais: `allOf` passou a ser avaliado sem depender da ordem; whitespace equivalente de diretivas Nginx é normalizado; `429.0` e detalhe somente NBSP são rejeitados pelos mesmos oráculos usados no runtime.
- A forma normativa também ficou fechada contra composições concorrentes que poderiam reintroduzir DTOs sensíveis: erros ordinários usam somente `$ref` direto de erro; o `429` usa somente o `allOf` contratado; headers de metadata são únicos sem diferenciar caixa. O filtro reconstrói esse dicionário case-insensitive antes de publicar Swagger.

### Comandos e resultados observados

| Gate | Resultado resumido |
|---|---|
| `sh -n scripts/validate-m1-compose.sh`; `git diff --check` | Sintaxe e whitespace aprovados durante a implementação. |
| `ruby scripts/validate-openapi.rb`; profile Docker `contract-tests` | Ambos aprovaram `SPEC-OAS-001..006`, seis operações e 56 referências, inclusive o schema restrito do `429`. |
| Profile Docker `backend-tests` | Build Release com 0 warnings/erros e 121/121 integrações; o Swagger runtime apresenta as mesmas restrições do contrato normativo. |
| Compose config com todos os profiles | Configuração aprovada sem `.env`. |
| Probes sintéticos dentro de `./scripts/validate-m1-compose.sh` | Comentário `10r/m` sobre `1r/m` ativo, texto quoted multilinha com nome de diretiva, `Retry-After: 60` junto de `0`, `status: 429.0` e detalhe somente NBSP foram rejeitados; quebra dentro da diretiva, diretivas compactas com `#` entre aspas e `detail` alternativo “Please retry later.” foram aceitos pelos mesmos oráculos usados no runtime. |
| `./scripts/validate-m1-compose.sh` | Estado final aprovado: três rajadas `10+1`, configuração ativa, headers/corpo, não-bypass, reset, persistência, logs e cleanup. |
| Stack principal restaurada | Contêineres/volume prévios preservados; `/`, `/health` e `/swagger/v1/swagger.json` responderam `200`. |

Duas execuções intermediárias reprovaram corretamente a própria tentativa de refatoração e não foram promovidas: a primeira contou `Cache-Control` de handlers distintos como duplicidade; a segunda examinou o `log_format` inativo da imagem base para XFF. O estado final mede cardinalidade na resposta `429` e restringe a proibição de confiança em XFF ao arquivo do servidor publicado. Frontend, Nginx de produção, banco e política `10r/m` não mudaram; por isso frontend/E2E e Stryker não foram repetidos. Somente metadados Swagger do backend e seu teste de integração mudaram, e a suíte backend completa foi reexecutada.

**Resultado corrente:** os três achados de teste e a inconsistência contratual encontrada na re-revisão foram encerrados com evidência real. As limitações local/efêmera e os itens externos Pending permanecem inalterados.

## Adendo — mensagens responsivas sem sobreposição

Em 2026-08-30, a inspeção manual da aplicação publicada encontrou dois defeitos de apresentação em 320 px: um erro de email em duas linhas invadia o campo seguinte em 8,8 px; confirmação curta exibia simultaneamente seu erro local e a divergência, com 18,4 px de colisão. Alertas globais de login, cadastro e perfil não apresentaram interseção.

### Correção e limites

- `mat-form-field` usa `subscriptSizing: dynamic` por provider raiz, reservando a altura real de hints e erros longos em todas as rotas lazy.
- Cadastro e perfil mostram a divergência somente quando a confirmação está tocada e localmente válida; obrigatoriedade e limites continuam validando o grupo, mas recebem precedência visual.
- A margem superior negativa do alerta externo foi removida. Não houve componente, dependência, rota, API, persistência, autenticação ou regra de negócio nova.

### Comandos e resultados observados

| Gate | Resultado resumido |
|---|---|
| Regressão frontend antes do código | Lint passou; 68/71 testes ficaram verdes. As três falhas isolaram provider ausente e mensagens de divergência simultâneas nos formulários de cadastro/perfil. |
| `docker compose --profile frontend-tests run --rm --build frontend-tests` | Lint, 71/71 testes em 10 arquivos e build de produção aprovados. |
| `./scripts/e2e-playwright.sh` | Estado final aprovado em 3/3 jornadas, 6,4 s. `E2E-001` mede erros, campos, alertas e ações em `320×568`, sem request inválida à API. |
| Provas negativas | Altura fixa e margem antiga foram recolocadas separadamente somente durante os testes: `E2E-001` reprovou com 7,8 px e 1,4 px de interseção, enquanto `E2E-002/003` passaram. Os dois responsáveis corretos foram restaurados antes do verde final. |
| Inventário de feedback | Quatro templates contêm 17 blocos condicionais `role=alert/status`: 15 usam as classes compartilhadas de sucesso/erro/loading, sem posicionamento ou margem negativos, e os dois `field-error` usam a margem corrigida. Specs dos quatro componentes renderizam os estados; runtime manual confirmou os alertas globais de login, cadastro e perfil. |
| Publicação e inspeção real | `docker compose up --build --detach --wait web` preservou API/volume e deixou ambos os serviços saudáveis. Em 320 px, erro de email/campo seguinte ficaram separados por 6 px e confirmação curta/ações por 17 px; o estado desktop em 1280 px também ficou sem interseção. |

O oráculo geométrico aguarda as animações Web do `mat-form-field` ancestral e seus descendentes terminarem, sem pausa fixa, antes de medir o erro renderizado contra o próximo controle. Quando não há esse ancestral, usa o próprio elemento como raiz. Também renderiza uma confirmação válida e diferente e compara campo/alerta/ação para provar que a margem externa não puxa o alerta sobre o campo. Isso encerra as lacunas encontradas nas re-revisões sem snapshot pixel a pixel.

**Resultado corrente:** `UI-RESP-01` está Verified pela geometria discriminante nos responsáveis reproduzidos, pelo inventário estrutural de todos os feedbacks e pela inspeção publicada. A aplicação em `http://localhost:8080` contém a correção e preserva o volume existente.
