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

`BE-MUT-001` e a infraestrutura local de `CI-MUT-001` foram implementados em 2026-08-27 sem alterar endpoint, OpenAPI, schema/migration, frontend ou regra de negócio. A execução hospedada continua Pending até o workflow ser publicado e observado.

### Implementação confrontada

- tool manifest raiz fixa `dotnet-stryker` `4.16.0`;
- `stryker-config.json` usa `Release`, `net10.0`, nível `standard`, análise `perTest`, dois workers, timeout adicional de 5 segundos, falha para suíte inicial vermelha e reporters progress/HTML/JSON;
- a allowlist contém exatamente os onze arquivos aprovados de autenticação, perfil, JWT, health e configuração EF; migrations, DTOs passivos, Swagger, entidade/DbContext e `Program.cs` não recebem mutante ativo;
- o target/profile `mutation-tests` não sobe a aplicação, não publica porta e grava em `${MUTATION_ARTIFACTS_DIR:-./artifacts/mutation}`;
- o runner valida o JSON depois do Stryker e reprova relatório ausente, `Timeout`, `NoCoverage`, `RuntimeError` ou quantidade de `CompileError` diferente dos dois mutantes inválidos classificados;
- o workflow separado possui somente disparo manual e cron de segunda-feira às `06:00 UTC`, runner `ubuntu-24.04`, limite de 90 minutos, `contents: read`, projeto Compose exclusivo, Actions por SHA, cleanup sem `--volumes` e upload por 14 dias.

### Comandos e resultados observados

| Comando ou gate | Resultado resumido |
|---|---|
| `docker compose --profile backend-tests run --rm --build backend-tests` | Build Release reutilizado sem warning/erro; 111/111 integrações passaram, 0 falha e 0 skip, em 6 s de testes. |
| Baseline temporária com `thresholds.break = 0` | Depois dos testes focados, 465 mutantes foram criados e 193 executados; 188 killed, 5 survived, 106 ignored, 2 `CompileError`, 0 `NoCoverage`, 0 timeout e 0 erro de execução; `S = 97,41%` em `00:04:41`. |
| `docker compose --profile mutation-tests run --rm --build mutation-tests`, configuração final | Execução definitiva da imagem com o gate incorporado: 188 killed, 5 survived, 0 timeout, 0 erro de execução, score `97,41%`; HTML/JSON gerados; gate do relatório aprovado; exit code 0 em `00:02:43`. |
| Prova negativa do gate | Um passe sob contenção produziu score `97,93%`, mas 12 timeouts; o Stryker retornou sucesso pelo score e o gate adicional encerrou o comando com exit code 1. Esse relatório foi rejeitado e substituído pela execução limpa. |
| `docker compose --profile mutation-tests config --quiet` | Configuração renderizada aprovada sem `.env`. |
| `docker run --rm --volume "$PWD:/repo:ro" --workdir /repo rhysd/actionlint:1.7.12` | Workflows aprovados sem diagnóstico. |
| `./scripts/validate-m1-compose.sh` | Inventários, builds, origem única, migrations, cadastro/login/perfil/senha, autorização, persistência, `413/415/503`, logs e cleanup isolado aprovados. |
| Auditoria do JSON/HTML | Contagens exatas; zero mutante ativo fora da allowlist; reporters presentes; nenhum padrão de JWT compacto, chave privada, valor Bearer ou arquivo SQLite. `artifacts/` continua ignorado e nenhum relatório/banco é rastreado pelo Git. |
| `docker compose --profile mutation-tests down --remove-orphans` | Removeu somente a rede residual do projeto de mutação. O volume `user-profile-sdd-challenge_user-profile-data` foi observado antes e depois; nenhum container/rede desse projeto permaneceu. |

O host compartilhava o daemon com containers de outros projetos em reinício e produziu passes intermediários com 25, 7 e 12 timeouts. Eles não foram promovidos. A repetição limpa com os mesmos dois workers e os mesmos 5 segundos comprova que não havia timeout intrínseco no alvo; nenhum container ou volume externo foi interrompido para obter o resultado.

### Baseline, ratchet e escopo

O score limpo `S = 188 / (188 + 5) = 97,41%` resultou em `floor(S) = 97`; logo, `break = 97`, `low = 97` e `high = 97`. O valor temporário zero não permanece na configuração. Não há `ignore-mutations` nem `ignore-methods` global.

O JSON do Stryker preserva algumas fontes fora da allowlist como `Ignored` pelo mutate filter; a auditoria encontrou zero mutante killed, survived, timeout, `NoCoverage` ou `CompileError` fora dos onze alvos. Os quatro arquivos de request estão explicitamente selecionados, mas o nível `standard` do Stryker `4.16.0` não gerou mutante executável em seus atributos/auto-properties.

Os 106 ignored do relatório incluem 56 mutantes de `Program.cs` removidos pelo mutate filter, 49 removidos pelo filtro de bloco já coberto e uma exclusão pontual. Os dois `CompileError` são mutações C# inválidas: trocar `Count()` por `Sum()` sobre IDs de migration em `DatabaseHealthCheck` e remover o initializer que satisfaz o membro obrigatório `HttpContext` de `ProblemDetailsContext` em `JwtBearerConfiguration`.

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

**Resultado:** `UI-RESP-01`, `FE-VISUAL-001`, `FR-UI-01` e `PREM-FE-01` estão Verified localmente. Os 19 requisitos funcionais, 14 não funcionais, 18 premissas e 41 critérios correntes permanecem rastreados; nenhuma funcionalidade de negócio nova foi criada.
