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
