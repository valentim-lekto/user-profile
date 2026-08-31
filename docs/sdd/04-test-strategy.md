# 04 — Estratégia de testes

**Status:** aprovada e executada em M1–M6; atividades pós-M6, inclusive o rate limiting de autenticação, aprovadas localmente · **Data:** 2026-08-30 · **Contrato:** [`03-api-contract.yaml`](03-api-contract.yaml)

## Objetivo

Validar os comportamentos e riscos principais com a menor suíte capaz de fornecer evidência confiável. A integração HTTP real do backend é a base; testes de frontend cobrem comportamento do cliente; poucas jornadas E2E e a validação do Compose comprovam a entrega completa.

## Princípios

- Cada milestone entrega seus próprios testes; M5 não acumula testes que deveriam acompanhar M2–M4.
- O backend é exercitado por HTTP com Controllers, middleware, autenticação, EF Core e SQLite reais.
- EF Core InMemory, mocks de repositório e chamadas diretas a Controllers não substituem integração.
- O mesmo cenário não é repetido em todas as camadas sem um risco específico que justifique.
- Casos positivos, negativos, autorização e formato de erro têm a mesma importância.
- Testes geram usuários, emails, senhas sintéticas e chaves JWT em runtime; não há seed nem credencial fixa versionada.
- Testes não são removidos, ignorados ou afrouxados para obter sucesso artificial.
- Testes frontend usam timeout global de 30 segundos, sem retry ou overrides locais mais curtos. Esse limite continua detectando travamentos, mas evita falsos negativos dos limites anteriores de 5/10 segundos quando fixtures Angular Material/Router executam em container frio ou sob contenção; a configuração deve ser exercitada pelo mesmo profile Docker usado na CI.

## Níveis e responsabilidades

| Nível | Ferramenta/abordagem planejada | Responsabilidade principal |
|---|---|---|
| Especificação | Parser/linter OpenAPI e checagens de rastreabilidade | Sintaxe, operações, segurança, schemas, IDs e ausência de escopo extra. |
| Integração backend | `UserProfile.Api.IntegrationTests`, `WebApplicationFactory` e `HttpClient` | Pipeline HTTP real em processo, SQLite real, migrations, autenticação, persistência e ProblemDetails. |
| Mutação backend | Stryker.NET `4.16.0` sobre a suíte xUnit real | Medir se alterações artificiais na lógica crítica são detectadas, com alvo explícito e ratchet baseado em baseline observada. |
| Frontend focado | Runner padrão do Angular, TestBed, Reactive Forms e `HttpTestingController` | Validações, signals, estados de UI, services, guard, interceptor e navegação. |
| E2E | Playwright `1.62.0` | Três jornadas completas pelo Nginx e API reais. |
| Operação | Docker Compose e verificações HTTP em checkout limpo | Build, origem única, health, volume, configuração e instruções do README. |

`WebApplicationFactory` exercita o pipeline ASP.NET completo sem abrir socket externo. As jornadas E2E e a validação Docker cobrem o tráfego TCP real e o proxy Nginx.

## Ambiente dos testes de integração

- Cada fixture usa um arquivo SQLite exclusivo em diretório temporário.
- A suíte aplica migrations ao banco do teste; pelo menos um teste parte de arquivo inexistente.
- A factory substitui apenas configuração de conexão, chave JWT e relógio; não substitui Controllers, `DbContext`, hasher nem autenticação.
- `TimeProvider` controlável permite testar `iat`/`exp` sem esperas reais.
- A chave JWT do teste é gerada em memória para cada execução.
- Usuários e emails recebem sufixos únicos; a senha sintética é gerada em runtime e reutilizada somente dentro do teste que a criou.
- Cenários de indisponibilidade partem de startup saudável e mantêm um bloqueio exclusivo real sobre o arquivo SQLite, com timeout curto; não usam endpoint, Controller ou health check exclusivo de teste.
- Arquivos temporários são removidos ao fim da fixture, inclusive em falha.
- Testes que compartilham banco são serializados; fixtures independentes podem executar em paralelo.

## Catálogo planejado — backend

| ID | Cenário | Evidência principal |
|---|---|---|
| `BE-REG-001` | Cadastro válido, incluindo limites inclusivos de nome `3/200`, email ASCII de 320 caracteres e senha/confirmação `6/128` (com espaços significativos), retorna `201`, persiste hash verificável diferente do texto puro, inicializa timestamps no relógio controlado e não retorna JWT, senha ou hash. | `AC-REG-01`, `PREM-DATA-02`, `PREM-INPUT-01`, `SEC-SECRET-01` |
| `BE-REG-002` | Cada campo obrigatório ausente, nome curto/longo, email não ASCII ou inválido por casos de borda da política comum (sem ponto no domínio ou com espaço interno)/longo, senha curta/longa, confirmação ausente/curta/longa/divergente ou propriedade JSON desconhecida/com caixa incorreta retorna `400 ValidationProblemDetails`, e nenhum usuário é persistido. | `AC-REG-02`–`04`, `PREM-INPUT-01`, `API-ERROR-01` |
| `BE-REG-003` | Emails ASCII com espaços externos ou caixa diferente colidem em `409`; emails Unicode que escapariam do `ToUpperInvariant` são rejeitados antes da normalização. | `AC-REG-05`, `PREM-EMAIL-01`, `PREM-INPUT-01` |
| `BE-REG-004` | O índice único existe e uma violação concorrente é mapeada para `409 ProblemDetails` com o mesmo contrato do conflito detectado previamente, sem segundo usuário. | `AC-REG-05`, `API-ERROR-01` |
| `BE-LOGIN-001` | Credenciais válidas retornam Bearer curto com `sub`, `jti`, `iat` e `exp`, sem refresh token. | `AC-LOGIN-01`, `SEC-SESSION-01` |
| `BE-LOGIN-002` | Payload ausente/inválido ou acima dos limites defensivos retorna `400 ValidationProblemDetails`; para payload válido, email inexistente e senha incorreta retornam o mesmo `401 ProblemDetails` genérico com challenge Bearer, sem indicar a credencial divergente nem criar sessão. | `AC-LOGIN-02`, `PREM-INPUT-01`, `API-ERROR-01` |
| `BE-LOGIN-003` | Login usa a mesma normalização de email adotada no cadastro. | `PREM-EMAIL-01` |
| `BE-AUTH-001` | Ausência, assinatura adulterada, issuer/audience inválidos e expiração retornam `401 ProblemDetails` com challenge Bearer. | `AC-DASH-02`, `SEC-AUTH-01`, `API-ERROR-01` |
| `BE-AUTH-002` | Claims mínimas ausentes/malformadas retornam `401`; `sub` válido sem usuário retorna `404`; o GET do perfil existente é resolvido somente por `sub`. | `SEC-AUTH-01` |
| `BE-CONFIG-001` | Chave externa Base64 de ao menos 32 bytes permite startup; ausência fora de `Development`, Base64 inválido ou valor curto falha; ausência em `Development` usa fallback aleatório; nenhum cenário registra a chave. | `SEC-AUTH-01`, `SEC-SECRET-01` |
| `BE-PROF-001` | GET retorna somente ID imutável, nome e email do usuário indicado pelo `sub`. | `AC-DASH-01`, `AC-PROF-01` |
| `BE-PROF-002` | Dois usuários consultam apenas o próprio perfil; query/header arbitrários não influenciam o `sub` usado pelo GET. | `SEC-AUTH-01`, `AC-PROF-01` |
| `BE-PROF-003` | PUT válido atualiza e persiste nome/email do usuário atual, preserva `CreatedAtUtc` e avança `UpdatedAtUtc`. | `AC-PROF-02`, `AC-PROF-05`, `PREM-DATA-02` |
| `BE-PROF-004` | PUT aplica validações equivalentes ao cadastro, aceita email ASCII válido exatamente em 320 caracteres e rejeita as classes inválidas; cada falha preserva nome, email, email normalizado e timestamps. | `AC-PROF-03`, `PREM-INPUT-01` |
| `BE-PROF-005` | Email de outro usuário retorna `409` sem alteração parcial; manter o próprio email não conflita nem executa consulta redundante; uma mudança concorrente da mesma conta para o email solicitado não é confundida com outro usuário. A contagem da query usa o pipeline HTTP real e `ApiFactory.WithInterceptor`, observa comandos LINQ síncronos e assíncronos e não reconstrói controller/claims manualmente. | `AC-PROF-04` |
| `BE-PROF-006` | Dois usuários alteram somente o próprio perfil; `userId` extra no JSON retorna `400`, e query/header arbitrários não influenciam o `sub` usado pelo PUT. | `SEC-AUTH-01`, `AC-PROF-01`, `AC-PROF-02` |
| `BE-PASS-001` | Senha atual válida exatamente em 128 caracteres é aceita; senha atual incorreta, ausente ou acima de 128 caracteres retorna `400` e preserva nome, email, email normalizado, hash e timestamps. | `AC-PASS-02`, `PREM-INPUT-01` |
| `BE-PASS-002` | Nova senha ausente/curta/longa ou confirmação ausente/curta/longa/divergente retorna `400`; toda a entidade é preservada, a senha antiga continua autenticando e a nova não autentica. | `AC-PASS-03`, `PREM-INPUT-01` |
| `BE-PASS-003` | Alteração válida retorna `200`, preserva `CreatedAtUtc`, avança `UpdatedAtUtc`; senha antiga falha e nova senha autentica. | `AC-PASS-04`, `PREM-DATA-02` |
| `BE-PASS-004` | O endpoint de senha rejeita Bearer ausente/inválido; com dois usuários altera somente a senha indicada pelo `sub`, e rejeita `userId` extra no JSON. | `AC-DASH-02`, `SEC-AUTH-01` |
| `BE-PASS-005` | Duas alterações concorrentes sincronizadas antes do `UPDATE`, com a mesma senha atual e novas senhas diferentes, produzem exatamente um `200` e um `400 ValidationProblemDetails` em `currentPassword`; senha antiga e candidata perdedora falham, e somente a vencedora autentica. | `AC-PASS-02`, `AC-PASS-05`, `API-ERROR-01` |
| `BE-DTO-001` | Nenhuma resposta expõe senha, hash ou email normalizado; somente `ProfileResponse` expõe o ID imutável previsto no contrato. | `AC-PROF-01`, `SEC-SECRET-01` |
| `BE-ERR-001` | Erros previstos e gerados pelo pipeline, incluindo JSON malformado, media type não suportado `415`, rota `/api` inexistente e método não permitido, usam `ProblemDetails`/`ValidationProblemDetails` e `application/problem+json`; o `413` anterior à API é coberto por `OPS-COMPOSE-001`. | `API-ERROR-01` |
| `BE-ERR-002` | Após startup saudável, cadastro contra SQLite bloqueado percorre o handler real e retorna `500` sem stack trace, SQL ou segredo na resposta. | `SEC-SECRET-01`, `API-ERROR-01` |
| `BE-DB-001` | Startup aplica migrations a banco vazio; a asserção usa o ID exato versionado, e o schema real contém exatamente os sete campos definidos no ADR-0002 com tipo/nulabilidade/chave esperados, cria o índice único e não deixa mudança pendente entre modelo e snapshot. | `OPS-DOCKER-01`, `PREM-DATA-02`, ADR-0002 |
| `BE-DB-002` | Um banco já migrado com linha órfã em `__EFMigrationsLock` volta a iniciar dentro do limite, preserva usuário/histórico e deixa o health saudável; schema conflitante continua falhando; retenções artificiais na preparação e na abertura usada por `MigrateAsync` recebem cancelamento no deadline operacional. | `OPS-DOCKER-04`, ADR-0002 |
| `BE-DB-003` | A cobertura combina dois oráculos: (a) um processo real bloqueado no lifecycle de migrations recebe `SIGTERM` antes da prontidão, comprova por log não filtrado que o listener HTTP não abriu, encerra com código zero antes do deadline interno e não deixa linha no lock técnico; (b) um teste direto do lifecycle bloqueia a abertura de `MigrateAsync`, cancela o token do chamador e comprova que esse cancelamento alcançou o token efetivamente entregue à operação. O teste direto só libera a operação após essa observação, sem confundir o token bruto do host com o token ligado usado pela migration. | `OPS-DOCKER-04`, ADR-0002 |
| `BE-HEALTH-001` | `/health` abre uma única conexão SQLite e retorna `200` após startup somente quando o conjunto exato de IDs de migration aplicado coincide com o esperado; histórico vazio ou de mesma cardinalidade com IDs divergentes é `Unhealthy`. A contagem da conexão usa o endpoint HTTP e `ApiFactory.WithInterceptor`. Com timeout padrão de 30 segundos na conexão da fixture, bloqueio exclusivo posterior retorna `503 application/problem+json` em menos de 5 segundos. Falha de migration é testada como falha de startup. | `OPS-DOCKER-01`, `API-ERROR-01` |
| `BE-OAS-001` | O OpenAPI gerado em runtime contém exatamente as seis operações implementadas em M4, com segurança, status, extensões pós-`Trim`, JWT Bearer e DTOs sem campos sensíveis; nos dois PUTs de M4, também vincula body obrigatório, media types e schemas de request/resposta coerentes com o contrato normativo. Somente cadastro/login declaram `429 ProblemDetails`, exatamente um `Retry-After: 60` e um `Cache-Control: no-store` por nome case-insensitive, os cinco campos obrigatórios/não nulos com seus tipos e `detail` não vazio. | `DOC-SDD-01`, `DOC-TRACE-01`, `SPEC-OAS-002`, `SPEC-OAS-003`, `SPEC-OAS-004`, `SPEC-OAS-005`, `SPEC-OAS-006` |
| `BE-MUT-001` | Stryker.NET executa mutantes somente na allowlist crítica de autenticação, perfil, JWT, health e configuração EF; a suíte inicial permanece verde, não há `NoCoverage`/timeout/erro inexplicado no alvo e os relatórios HTML/JSON exibem todos os survivors. | `NFR-TEST-01`, `TEST-FLOW-01` |

## Catálogo planejado — frontend

| ID | Cenário | Evidência principal |
|---|---|---|
| `FE-REG-001` | Formulário tipado exige quatro campos, valida e exibe os erros locais, não envia payload inválido, prova limites inclusivos/máximos após o mesmo tratamento do backend, aceita espaços significativos em senha, aceita email ASCII válido exatamente em 320 caracteres, rejeita Unicode e as mesmas bordas de formato e exige igualdade das senhas. Quando a confirmação possui erro próprio, ele tem precedência sobre a mensagem de divergência e sobre seu `aria-errormessage`. | `AC-REG-02`–`04`, `PREM-INPUT-01`, `UI-RESP-01` |
| `FE-REG-002` | Loading bloqueia nova submissão; `201` leva ao login com sucesso; erro permanece visível sem criar sessão. | `AC-REG-01`, `AC-REG-06`, `UI-STATE-01` |
| `FE-LOGIN-001` | Formulário e service aplicam os limites defensivos inclusivos de email/senha, incluindo email ASCII válido com 320 caracteres e senha com 128, tratam loading, bloqueiam submissão duplicada, exibem o `401` genérico de credenciais e tratam validação `400`/erro inesperado. | `AC-LOGIN-02`, `AC-LOGIN-03`, `PREM-INPUT-01` |
| `FE-LOGIN-002` | Sucesso grava token apenas em `sessionStorage` e navega ao dashboard. | `AC-LOGIN-01`, `SEC-SESSION-01` |
| `FE-RATE-001` | Login e cadastro traduzem `429` para a mensagem fixa acessível, preservam todos os valores, encerram loading, reabilitam a submissão e permanecem na rota sem criar/limpar sessão nem iniciar countdown. | `API-ERROR-02`, `UI-STATE-01` |
| `FE-GUARD-001` | Guard permite sessão não expirada e bloqueia token ausente, malformado ou expirado. Uma sessão válida agenda seu próprio `exp`; se o mesmo token ainda for corrente nesse instante, ele é removido e dashboard/perfil já ativo conduz ao login, inclusive com matrix params/query/fragment, sem timer antigo afetar sessão posterior ou rota pública e sem timer sobreviver à destruição do serviço. | `AC-DASH-02`, `SEC-SESSION-01` |
| `FE-INT-001` | Interceptor anexa Bearer somente às URLs relativas protegidas de perfil; não o anexa a login, cadastro, health, URL absoluta nem destino externo. | `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01` |
| `FE-INT-002` | Request protegida iniciada sem token válido é cancelada localmente e conduz ao login. `401` de request que levava Bearer limpa sessão e conduz ao login somente se a sessão corrente ainda usa o mesmo token; um `401` tardio não remove uma sessão mais recente. O `401` do login público não levava Bearer, não dispara limpeza global e continua disponível para a tela. | `AC-LOGIN-02`, `AC-DASH-02`, `SEC-SESSION-01` |
| `FE-DASH-001` | Dashboard busca perfil, mostra loading/erro, saúda pelo nome retornado, navega ao perfil, não renderiza cartões descritivos redundantes de dados pessoais/senha/sessão e faz logout removendo somente o token da aplicação antes de voltar ao login. Uma nova ativação usa estado de perfil novo, de modo que a resposta pendente da sessão anterior não bloqueia nem preenche a seguinte. | `AC-DASH-01`, `AC-DASH-03`, `AC-DASH-04`, `SEC-SESSION-01` |
| `FE-WIRE-001` | A configuração real da aplicação conecta as rotas protegidas ao guard e o `HttpClient` ao interceptor: acesso anônimo redireciona e uma sessão válida produz Bearer no GET do dashboard. | `AC-DASH-01`, `AC-DASH-02`, `SEC-AUTH-01` |
| `FE-VISUAL-001` | Shell, login, cadastro, dashboard e perfil preservam landmarks, hierarquia de headings, labels, nomes acessíveis, estados e foco; a tela de perfil não promove o `id` técnico a conteúdo visual. As jornadas existentes comprovam ausência de overflow nas quatro telas em 320 px, acesso rolável até o último campo e a ação primária da autenticação em landscape curto, ordem visual igual à sequência de Tab nas ações móveis e as ações `Ir para o perfil`/`Sair` mantidas na primeira viewport com nome de 200 caracteres visualmente limitado, visível e integral no DOM. Em `320×568`, regressões de geometria exigem que mensagens longas ou simultâneas permaneçam no fluxo: `mat-error`, alerta de confirmação, próximo campo e ação não podem possuir retângulos intersectantes. A inspeção real cobre desktop/mobile e contraste sem depender de snapshot pixel a pixel. | `FR-UI-01`, `UI-STATE-01`, `UI-RESP-01`, `PREM-FE-01` |
| `FE-PROF-001` | Perfil carrega nome/email com estados de loading/erro sem renderizar o `id` recebido; o formulário renderizado vincula os inputs reais, exibe erros locais e valida edição com as regras do cadastro. | `AC-PROF-01`, `AC-PROF-03`, `UI-STATE-01`, `TEST-FLOW-01` |
| `FE-PROF-002` | A submissão pelo formulário renderizado envia exatamente nome/email — nunca senha, mesmo vazia —, mostra loading, bloqueia os campos e submissões duplicadas e apresenta feedback de sucesso/erro, incluindo `409`; nova consulta do dashboard mostra o nome persistido. | `AC-PROF-02`, `AC-PROF-04`, `AC-PROF-05`, `UI-STATE-01`, `TEST-FLOW-01` |
| `FE-PASS-001` | O formulário separado renderizado exige senha atual, nova senha e confirmação, e aceita o limite inclusivo `128` em todas as entradas antes de rejeitar `129`. Erro próprio da confirmação tem precedência sobre a divergência e seu `aria-errormessage`. | `AC-PASS-01`, `AC-PASS-03`, `PREM-INPUT-01`, `UI-RESP-01`, `TEST-FLOW-01` |
| `FE-PASS-002` | A submissão pelo formulário renderizado bloqueia os campos e submissões duplicadas durante o loading; sucesso remove o JWT e navega ao login com feedback somente se a sessão ainda usa o token que iniciou a operação; resposta tardia preserva uma sessão posterior, e senha atual incorreta mantém a sessão e mostra erro. | `AC-PASS-02`, `AC-PASS-04`, `UI-STATE-01`, `SEC-SESSION-01`, `TEST-FLOW-01` |

Os testes de frontend não reimplementam criptografia, EF ou validação JWT. Services recebem respostas HTTP controladas; guard e interceptor são testados como funções no contexto de injeção Angular.

## Jornadas E2E mínimas

| ID | Jornada | Abrangência |
|---|---|---|
| `E2E-001` | Cadastrar → ver sucesso no login → autenticar → ver saudação → editar nome/email → consultar novamente dashboard e perfil sem identificador técnico visível → encerrar a sessão → tentar reabrir o dashboard. | Caminho feliz principal, persistência de ambos os campos, ausência do `id` na interface, logout e reproteção. |
| `E2E-002` | Abrir uma rota protegida sem token e confirmar o redirecionamento; tentar login com credenciais não reconhecidas e permanecer no login com a mensagem genérica. | Guard, credenciais inválidas e ausência de sessão. |
| `E2E-003` | Cadastrar e autenticar uma conta própria → alterar a senha → confirmar sessão encerrada tentando reabrir o dashboard → senha antiga falhar → senha nova autenticar e abrir o dashboard. | Senha, encerramento de sessão comprovado pelo guard e nova autenticação. |

As jornadas usam a origem publicada pelo Nginx e não chamam a API diretamente para preparar estado. Cada uma cria seus próprios dados pela interface, usa email único gerado em runtime, abre contexto de navegador isolado e não depende de ordem ou seed compartilhado; o volume é isolado por execução da suíte. Campos e ações críticos são localizados pelo nome acessível, para que a remoção de labels ou nomes de botões quebre o gate. Senhas sintéticas são geradas e mantidas no próprio contexto do navegador; chamadas registradas pelo Playwright recebem somente chaves não sensíveis, e os campos são limpos em `finally` como defesa adicional. Playwright executa sem retries ocultos, grava screenshot de campos mascarados e trace minimizado somente quando há falha e espera estados observáveis, nunca pausas fixas. A indisponibilidade real da API e o `503 ProblemDetails` do proxy permanecem cobertos por `OPS-COMPOSE-001`, sem criar uma quarta jornada.

`E2E-001` também concentra a matriz responsiva de `FE-VISUAL-001`, sem criar `E2E-004`: antes do fluxo funcional, verifica login/cadastro completos e operáveis em `667×375`; em seguida percorre login, cadastro, dashboard e perfil em `320×568`, incluindo ordem visual/Tab das ações. Submissões localmente inválidas, sem request à API, comprovam que erro de email em múltiplas linhas não intercepta o campo seguinte, que confirmação curta não acumula divergência e que o feedback de senha permanece separado da ação. Depois da edição, usa o nome válido de 200 caracteres para comprovar texto integral no DOM, visibilidade e contenção horizontal, limite visual de três linhas no hero e duas no resumo, `Ir para o perfil`/`Sair` integralmente visíveis e ausência de overflow horizontal.

## Validação de especificação e contrato

| ID | Checagem |
|---|---|
| `SPEC-OAS-001` | YAML parseável e documento reconhecido como OpenAPI 3.0.3. |
| `SPEC-OAS-002` | Seis `operationId` únicos, cinco operações de negócio e `/health`; nenhuma rota fora de escopo. |
| `SPEC-OAS-003` | Operações de perfil têm Bearer, não definem `userId` e seus request bodies rejeitam propriedades extras; públicas declaram `security: []`. |
| `SPEC-OAS-004` | Campos obrigatórios, nomes case-sensitive, validações pós-`Trim`, limites, formatos e status coincidem com requisitos/design. |
| `SPEC-OAS-005` | Erros referenciam ProblemDetails; operações JSON declaram `413/415`; payload de login inválido usa `400`, credenciais não reconhecidas usam `401` genérico e todo `401` declara `WWW-Authenticate` como obrigatório; indisponibilidade declara `503`; respostas não contêm campos sensíveis além do ID imutável permitido em `ProfileResponse`. |
| `SPEC-OAS-006` | Somente `POST /api/auth/register` e `POST /api/auth/login` declaram `429` por `RateLimitProblem`; a resposta usa somente a composição `ProblemDetails` contratada em `application/problem+json`, exige exatamente uma declaração case-insensitive de `Retry-After: 60` e de `Cache-Control: no-store`, os cinco campos não nulos/tipados de `API-ERROR-02` e `detail` com ao menos um caractere não branco. Outros erros continuam como referência direta exclusiva a `ProblemDetails`/`ValidationProblemDetails`. O Swagger runtime apresenta as mesmas garantias. |
| `SPEC-TRACE-001` | Cada requisito/critério aplicável possui linha em `06-traceability.md` e teste planejado. |

`scripts/validate-openapi.rb` executa `SPEC-OAS-001`–`006` sobre o contrato
versionado, incluindo métodos extras, schemas, status, segurança, `userId`,
ProblemDetails e campos sensíveis. O teste `BE-OAS-001` cobre as seis operações
do documento exposto em runtime e, nos dois PUTs de M4, inclui os `$ref` e media
types que associam cada request e resposta ao schema normativo correspondente.
A CI deve validar o contrato versionado e o documento runtime por esses gates;
divergência quebra o build.

## Validação Docker e entrega

| ID | Cenário | Evidência principal |
|---|---|---|
| `TECH-BACKEND-001` | Solution/projetos e lock NuGet usam ASP.NET Core/C#, EF Core SQLite e JWT nas versões fixadas; restore locked e build passam. | `TECH-BACKEND-01` |
| `TECH-FRONTEND-001` | `package.json`/lockfile usam Angular standalone/strict, Reactive Forms e Material nas versões fixadas; o Dockerfile confere npm `11.17.0`; o Vitest usa timeout global de 30 segundos sem retry ou override local menor; `npm ci` com allowlist estrita, lint, teste e build passam no profile Docker, inclusive em repetições frias. | `TECH-FRONTEND-01`, `TEST-FLOW-01` |
| `OPS-COMPOSE-001` | `scripts/validate-m1-compose.sh` é o smoke acumulado M1+M2+M3+M4: em projeto/volume nomeado efêmero executa `docker compose up --build --wait` sem `.env` nem SDKs, exige timeout SQLite de 5 segundos com margem para os 30 segundos do proxy, valida origem única, cadastro, login, os dois PUTs, falhas sem mutação, `401` equivalente com Bearer, `413/415`, ausência de credenciais/marcadores/JWT nos logs, recria a API preservando perfil/senha, renova a sessão e remove somente os recursos temporários ao final. | `OPS-DOCKER-01`, `OPS-DOCKER-02`, `OPS-DOCKER-03`, `API-ERROR-01` |
| `OPS-ORIGIN-001` | O mesmo smoke verifica SPA, fallback de rota, `404` para assets inexistentes de classes distintas, precedência do proxy em `/api/*.json` e `/swagger/*.json`, `/health` por `http://localhost:8080`, bind público do web exatamente em `127.0.0.1:8080`, ausência de porta pública da API, `404 ProblemDetails`, upstream parado convertido em `503 ProblemDetails` e mapeamento explícito de `502`/`504`. | `API-ERROR-01`, ADR-0004 |
| `OPS-PERSIST-001` | Criar usuário, recriar serviços sem remover volume e autenticar novamente. | `OPS-DOCKER-03` |
| `OPS-TAGS-001` | Todas as linhas `FROM` e o conjunto de imagens renderizado com todos os perfis Compose coincidem exatamente com as versões completas do design; mutações para major/minor, `latest`, `stable` ou `lts` reprovam o gate. | `OPS-DOCKER-02` |
| `OPS-SECRET-001` | Compose inicia sem segredo versionado; `.env.example` é opcional e não contém valor utilizável; o smoke envia marcadores sintéticos em query/body/header, usa uma senha sintética no fluxo válido e comprova que logs da API/Nginx não contêm esses valores nem o JWT observado; a auditoria acumulada final também cobre hash e chave. | `SEC-SECRET-01`, `SEC-LOG-01` |
| `OPS-RATE-001` | O smoke recria somente `web`, aguarda health e dispara rajadas independentes de 11 `POST` concorrentes ao login e ao cadastro: em cada endpoint, exatamente 10 alcançam a API e uma recebe `429` com media type, corpo e headers contratados. O inventário da configuração efetivamente carregada considera somente diretivas ativas, normaliza whitespace entre tokens/quebras dentro da diretiva, preserva conteúdo entre aspas e localiza diretivas compactas na mesma linha; exige exatamente uma zona de 1 MiB com `rate=10r/m` e um `limit_req` com `burst=9 nodelay`. Probes comprovam que texto comentado ou quoted — inclusive multilinha — não mascara configuração incorreta, sem transformar formatação ativa equivalente em falso-vermelho. Cada resposta `429` deve conter uma única ocorrência de `Retry-After: 60` e `Cache-Control: no-store`, e um probe rejeita valores duplicados/conflitantes. O corpo exige os cinco campos não nulos/tipados, `status` inteiro, instância contratada, `detail` genérico com caractere não branco Unicode e ausência recursiva de dados sensíveis; probes rejeitam `429.0` e detalhe formado só por NBSP, enquanto uma mensagem segura alternativa deve passar sem transformar o texto ilustrativo do OpenAPI em valor normativo. Métodos diferentes, health, Swagger e perfil não são limitados; `413` continua independente. | `NFR-SEC-02`, `SEC-RATE-01`, `API-ERROR-02` |
| `OPS-RATE-002` | Com o bucket de login esgotado, query string, variações de caixa/barra final e `X-Forwarded-For` forjado não renovam a cota. Depois de recriar somente `web`, uma nova rajada completa de 11 logins volta a produzir 10 respostas da API e um `429`, distinguindo reset integral de reposição natural; usuário/perfil no SQLite permanecem acessíveis e logs não contêm email, query, senha, JWT ou header forjado. | `SEC-RATE-02`, `SEC-LOG-01`, `OPS-DOCKER-03` |
| `CI-001` | O workflow executa contrato, restore/build/test backend, `npm ci` com allowlist estrita/lint/test/build frontend, build das imagens, Compose saudável e `E2E-001`–`003`; Actions usam SHA completo e o checkout não persiste credenciais. Cada script registra seu projeto e persiste `ps`, imagens/serviços e logs processados pelo filtro compartilhado antes do teardown em falha, sem publicar a cópia bruta. A etapa final aceita somente nomes sob o prefixo único da execução, tenta novamente o cleanup, filtra sua saída e ocorre antes do upload. Falha de teardown reprova sucesso sem substituir a falha primária; M5 testa essa precedência e o filtro com marcadores sintéticos, força traces e reprova qualquer senha sintética ou JWT. | `TECH-BACKEND-01`, `TECH-FRONTEND-01`, `TEST-FLOW-01`, `OPS-DOCKER-02`, `SEC-LOG-01` |
| `CI-MUT-001` | Workflow separado, manual e semanal, executa somente o profile `mutation-tests` em projeto Compose exclusivo, respeita o ratchet, faz cleanup sem tocar no volume da aplicação e publica HTML/JSON por 14 dias quando produzidos. Não possui gatilho de push/PR. | `NFR-TEST-01`, `TEST-FLOW-01`, `OPS-DOCKER-02`, `SEC-LOG-01` |
| `DOC-RUN-001` | Uma pessoa segue o README em ambiente limpo e reproduz comandos, URLs e cadastro de dados. | `DOC-RUN-01` |
| `DOC-EXPLAIN-001` | Walkthrough manual cobre ADRs, fluxo `sub`, senha/JWT, proxy/SQLite, estados do frontend e um caminho rastreado de requisito até teste, com resultado resumido. | `AI-EXPLAIN-01` |

## Gates por milestone

| Milestone | Gates mínimos |
|---|---|
| M1 | Build backend/frontend; `dotnet test` deve descobrir e aprovar exatamente os testes M1, não apenas retornar exit code zero; `TECH-FRONTEND-001`, parte aplicável de `TECH-BACKEND-001`, `SPEC-OAS-*`, `BE-DB-001`, `BE-HEALTH-001`, `BE-OAS-001`, `OPS-COMPOSE-001`, `OPS-ORIGIN-001`, `OPS-TAGS-001`, `.env.example` sem segredo, ProblemDetails runtime, Swagger e smoke Compose. |
| M2 | `BE-REG-*`, `FE-REG-*`, `BE-ERR-001/002`, `BE-OAS-001`, parcela M2 de `OPS-COMPOSE-001`/`OPS-SECRET-001`, assertion aplicável de `BE-DTO-001` e regressão dos gates M1. |
| M3 | `BE-LOGIN-*`, `BE-AUTH-*`, `BE-CONFIG-001`, `BE-PROF-001/002`, `TECH-BACKEND-001`, parte de `.env.example`/logs de `OPS-SECRET-001`, `FE-LOGIN-*`, `FE-GUARD-*`, `FE-INT-*`, `FE-DASH-*`, `FE-WIRE-*` e assertions aplicáveis de `BE-ERR-001`/`BE-DTO-001`. |
| M4 | `BE-PROF-003/004/005/006`, `BE-PASS-*`, `FE-PROF-*`, `FE-PASS-*`, parcela M4 de `OPS-COMPOSE-001`/`OPS-SECRET-001` e assertions aplicáveis de `BE-ERR-001`/`BE-DTO-001`. |
| M5 | `E2E-*`, `CI-001`, suíte acumulada completa, perfis Compose para qualidade/E2E sem SDKs no host, `OPS-TAGS-001`, auditorias de log/segredo e build de produção. |
| M6 | Reexecução de `TECH-*`, `OPS-*`, `DOC-RUN-001`, `DOC-EXPLAIN-001`, `SPEC-TRACE-001`, revisão manual e execução completa em checkout limpo. |
| Pós-M6 | `BE-MUT-001`, `CI-MUT-001`, `BE-DB-002/003`, evolução de `BE-HEALTH-001`/`BE-PROF-005`, `SPEC-OAS-006`, `FE-RATE-001`, `OPS-RATE-001/002`, suíte backend/frontend normal, profiles Docker de teste/mutação, contrato, configuração/smoke, três E2E existentes, actionlint quando aplicável e registro das baselines/evidências. |

## Política de cobertura

Não será fixado percentual arbitrário. A cobertura é orientada à matriz e aos riscos: todos os critérios funcionais, autorização, unicidade, formato de erro e operação Docker precisam de evidência automatizada ou validação manual explicitamente registrada. Código trivial de configuração não recebe teste unitário isolado quando já é exercitado por integração.

### Baseline e ratchet de mutation testing

O primeiro passe de `BE-MUT-001` usa `thresholds.break = 0` apenas para medir a baseline. Cada survivor é classificado: comportamento observável ou requisito recebe teste xUnit focado; regra de negócio não é alterada apenas para matar mutante; mutante comprovadamente equivalente só pode receber exclusão pontual com justificativa junto ao código. Não são permitidos `ignore-mutations` ou `ignore-methods` globais.

Após não restarem `NoCoverage`, timeout ou erro de execução sem explicação no alvo crítico, o score final `S` fixa `break = floor(S)`, `low = max(60, break)` e `high = max(80, low)`. A configuração final precisa executar com exit code zero e `break > 0`. Quantidades killed/survived/ignored/no-coverage, score, duração e justificativas são registradas em `07-validation-report.md`; survivor não equivalente fora de requisito permanece visível e participa da baseline.

Baseline corrente reexecutada em 2026-08-30: `S = 97,50%`, com 513 mutantes descobertos, 200 executados, 195 killed, 5 survived, 109 ignored, 3 `CompileError` produzidos por mutações não compiláveis, 0 `NoCoverage`, 0 timeout e 0 erro de execução, em `00:18:48`. O ratchet final continua `break/low/high = 97/97/97`. Nenhum ignore foi adicionado; os cinco survivors equivalentes permanecem no relatório, e uma única exclusão pontual justificável continua contabilizada como ignored; não há ignore global.

Como o score considera timeout como detectado, o runner Docker não confia apenas no exit code do Stryker: ele valida o JSON final e reprova `Timeout`, `NoCoverage`, `RuntimeError`, relatório ausente e quantidade de `CompileError` diferente dos três mutantes não compiláveis já classificados. Esse gate impede falso-verde sem aumentar timeout, reduzir suíte ou ocultar mutante.

## Critério de conclusão da suíte

- Todos os testes planejados para o milestone atual estão implementados e passam.
- Não existem skips, exclusões ou retries ocultando falhas determinísticas.
- O profile `frontend-tests` não depende dos limites anteriores de 5/10 segundos: todos os testes herdam o limite global documentado e o profile deve permanecer verde em repetições contra a imagem reconstruída.
- Bancos, containers e processos temporários são limpos após a execução.
- Falhas produzem evidência suficiente sem imprimir valores sensíveis.
- A matriz de rastreabilidade reflete os IDs implementados e seu estado real.
- O gate de M4 também reexecuta `OPS-COMPOSE-001` de forma acumulada: os dois PUTs, preservação após falhas, senha antiga/nova, persistência após recriar a API, logs seguros e cleanup isolado devem passar; a interface real cobre o fluxo cadastral e a inspeção dos dois formulários sem antecipar as jornadas Playwright de M5.
- O gate de M5 executa as três jornadas independentes contra a origem Nginx real, sem chamadas de preparação à API, comprova persistência de nome/email, reproteção após logout/troca de senha e nomes acessíveis, e confirma que os mesmos comandos de backend, frontend e E2E podem rodar por perfis do Compose sem SDKs instalados no host.
- O gate pós-M6 executa primeiro a suíte backend normal e depois a configuração final de mutação pelo Compose; somente os arquivos-alvo podem conter mutantes ativos/executados, e o relatório não pode expor senha, JWT, chave ou banco e precisa atender ao ratchet versionado.
- No JSON do Stryker `4.16.0`, `files` também preserva fontes fora da allowlist com seus mutantes marcados `Ignored` pelo mutate filter. A verificação de escopo considera somente mutantes ativos/executados: todos precisam pertencer aos onze arquivos-alvo; nenhuma fonte externa pode ter killed, survived, timeout ou `NoCoverage`.
