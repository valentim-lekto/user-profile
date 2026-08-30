# User Profile SDD Challenge

Aplicação full stack para cadastro, autenticação e manutenção do próprio perfil. A entrega usa uma única origem HTTP, mantém a identidade exclusivamente no `sub` do JWT e foi construída em milestones orientados pelos artefatos SDD.

A interface Angular Material é responsiva e usa uma identidade visual própria, inspirada apenas nos princípios de composição e paleta do site público da Lekto, sem copiar marca ou ativos.

O estado validado da entrega está em [`docs/sdd/07-validation-report.md`](docs/sdd/07-validation-report.md).

## Stack e versões

| Camada | Versão fixada |
|---|---|
| .NET SDK | `10.0.400` |
| ASP.NET Core / EF Core SQLite | `10.0.11` |
| Angular / Angular Material | `22.1.3` |
| Node.js / npm | `24.19.0` / `11.17.0` |
| Nginx | `1.30.4-alpine3.24-slim` |
| Playwright | `1.62.0` |
| Stryker.NET | `4.16.0` |
| Ruby do validador OpenAPI | `3.4.10-slim-bookworm` |

As versões completas estão justificadas em [`docs/sdd/02-technical-design.md`](docs/sdd/02-technical-design.md). Não há tag Docker `latest`.

## Arquitetura resumida

- monólito modular ASP.NET Core com Controllers e módulos `Auth` e `Profile`;
- EF Core com SQLite em volume nomeado e migrations aplicadas por lifecycle de startup, depois do registro dos sinais do host e antes do listener HTTP;
- senhas tratadas com `PasswordHasher<User>` e nunca devolvidas pelos DTOs;
- JWT Bearer de 15 minutos, sem refresh token, validando issuer, audience, assinatura, algoritmo e expiração;
- Angular standalone/strict, Reactive Forms, Material, services e signals;
- Nginx serve a SPA e encaminha `/api`, `/swagger` e `/health` para a API interna;
- somente o Nginx publica porta no host, restrita ao loopback IPv4 em
  `http://localhost:8080`.

Fluxo principal: navegador → Nginx → Angular ou API → Controller → EF Core → SQLite. O cliente nunca envia um `userId` para selecionar o perfil; a API usa exclusivamente o `sub` validado.

## Executar

Para executar a aplicação, o único pré-requisito é Docker com Docker Compose
v2; a auditoria final usou `v2.37.1-desktop.1`. Não é necessário instalar
.NET, Node, npm, Ruby, Playwright ou SQLite, nem criar `.env`.

```sh
docker compose up --build --detach --wait
```

O comando cria as imagens, aplica a migration inicial, cria o volume `user-profile-sdd-challenge_user-profile-data` e só retorna sucesso quando o healthcheck da origem única estiver saudável.

## URLs

| Recurso | URL |
|---|---|
| Aplicação | [http://localhost:8080](http://localhost:8080) |
| Cadastro | [http://localhost:8080/register](http://localhost:8080/register) |
| Login | [http://localhost:8080/login](http://localhost:8080/login) |
| Health | [http://localhost:8080/health](http://localhost:8080/health) |
| Swagger UI | [http://localhost:8080/swagger/index.html](http://localhost:8080/swagger/index.html) |
| OpenAPI runtime | [http://localhost:8080/swagger/v1/swagger.json](http://localhost:8080/swagger/v1/swagger.json) |

## Fluxo rápido de validação

1. Abra `/register`, informe nome, email e uma senha sintética com pelo menos 6 caracteres.
2. Após o `201`, confirme o redirecionamento ao login e entre com a conta criada.
3. No dashboard, confira a saudação com o nome retornado por `GET /api/profile`.
4. Abra o perfil, altere nome/email e volte ao dashboard para observar a nova consulta.
5. Troque a senha. A sessão deve ser encerrada e a rota protegida deve voltar ao login.
6. Confirme que a senha antiga falha e a nova autentica; use **Sair** para testar o logout normal.

Não há seed nem conta padrão. Use dados sintéticos e emails únicos.

## Testes somente com Docker

Os comandos abaixo usam imagens/profiles fixados e não dependem de SDKs no host:

```sh
docker compose --profile contract-tests run --rm contract-tests
docker compose --profile backend-tests run --rm --build backend-tests
docker compose --profile mutation-tests run --rm --build mutation-tests
docker compose --profile frontend-tests run --rm --build frontend-tests
./scripts/e2e-playwright.sh
```

Os dois scripts `./scripts/...` pressupõem macOS, Linux ou WSL com shell POSIX,
`curl` e utilitários comuns (`sed`, `grep`, `awk`, `sort`, `cmp`, `dd` e
`mktemp`). SDKs, browser e banco continuam sendo executados nos contêineres.

O E2E cria projeto, rede e volume próprios, executa exatamente três jornadas independentes e remove seus recursos ao terminar. Screenshot e trace são retidos somente em falha.

O profile `mutation-tests` executa Stryker.NET somente sobre a allowlist crítica do backend. A baseline limpa corrente foi recalibrada após o fortalecimento dos oráculos para `97,50%`: 492 mutantes descobertos, 200 executados, 195 killed, 5 survived, 105 ignored, 3 `CompileError` gerados por mutações não compiláveis, 0 timeout, 0 `NoCoverage` e 0 erro de execução. O ratchet versionado continua `break/low/high = 97/97/97`. Um gate adicional do relatório reprova timeout, lacuna de cobertura, erro de runtime ou alteração na quantidade dos três erros de compilação já classificados. HTML e JSON são gravados em `artifacts/mutation/reports/` por padrão e nunca devem ser versionados. Os cinco survivors equivalentes permanecem visíveis no relatório limpo; a estratégia e as justificativas estão em [`docs/sdd/04-test-strategy.md`](docs/sdd/04-test-strategy.md) e [`docs/sdd/07-validation-report.md`](docs/sdd/07-validation-report.md).

O smoke completo usa a porta 8080; encerre antes a pilha principal sem remover seu volume:

```sh
docker compose down --remove-orphans
./scripts/validate-m1-compose.sh
```

O smoke valida origem única, banco vazio/migration, cadastro, login genérico, autorização, perfil, senha, persistência após recriação, `413/415/503`, logs seguros, tags e cleanup. Para validar o workflow localmente em container:

```sh
docker run --rm --volume "$PWD:/repo:ro" --workdir /repo rhysd/actionlint:1.7.12
```

## Parar, reiniciar e apagar dados

Parar e remover containers/rede, preservando o SQLite:

```sh
docker compose down --remove-orphans
```

Subir novamente com os dados preservados:

```sh
docker compose up --build --detach --wait
```

Reiniciar containers já em execução e aguardar saúde:

```sh
docker compose restart
docker compose up --detach --wait --wait-timeout 300
```

Sem uma chave JWT externa, reiniciar a API invalida os tokens existentes, mas não apaga usuários. Para apagar definitivamente os dados deste projeto:

```sh
docker compose down --volumes --remove-orphans
```

Esse último comando remove o volume SQLite e não pode ser desfeito pelo Git.

## Variáveis de ambiente

O Compose funciona sem `.env`. [`.env.example`](.env.example) documenta overrides opcionais; copie-o apenas se precisar de valores próprios e nunca versione a cópia com segredo.

| Variável | Padrão em Development | Regra |
|---|---|---|
| `Jwt__Issuer` | `UserProfile.Api` | não pode ser vazio quando sobrescrito |
| `Jwt__Audience` | `UserProfile.Web` | não pode ser vazio quando sobrescrito |
| `Jwt__LifetimeMinutes` | `15` | somente `15` é aceito |
| `Jwt__SigningKey` | aleatória em memória por processo | Base64 de pelo menos 32 bytes quando informada; obrigatória fora de Development |
| `MUTATION_ARTIFACTS_DIR` | `./artifacts/mutation` | diretório host para relatórios HTML/JSON do profile de mutação; não contém configuração da aplicação |

Para sessões sobreviverem ao restart local, gere uma chave própria fora do repositório, por exemplo com `openssl rand -base64 32`, e forneça-a pelo ambiente. O valor real nunca deve entrar em logs, documentação ou commits.

## Estrutura do repositório

```text
.
├── .config/dotnet-tools.json                    dotnet-stryker fixado
├── src/backend/UserProfile.Api/                 API, EF Core e migrations
├── src/frontend/user-profile-web/               Angular e Nginx
├── tests/backend/UserProfile.Api.IntegrationTests/  integração HTTP/SQLite
├── tests/e2e/                                    três jornadas Playwright
├── scripts/                                      contrato, smoke e cleanup seguro
├── docs/sdd/                                     requisitos, design e evidências
├── .github/workflows/ci.yml                      pipeline de CI
├── .github/workflows/mutation.yml                mutação manual/semanal
├── compose.yaml                                  aplicação e profiles de teste
└── UserProfile.sln                               solution .NET
```

## SDD e decisões

- [`docs/sdd/README.md`](docs/sdd/README.md) — índice dos artefatos;
- [`docs/sdd/01-requirements.md`](docs/sdd/01-requirements.md) — requisitos e critérios;
- [`docs/sdd/02-technical-design.md`](docs/sdd/02-technical-design.md) — design e versões;
- [`docs/sdd/03-api-contract.yaml`](docs/sdd/03-api-contract.yaml) — contrato OpenAPI;
- [`docs/sdd/04-test-strategy.md`](docs/sdd/04-test-strategy.md) — estratégia de testes;
- [`docs/sdd/05-execution-plan.md`](docs/sdd/05-execution-plan.md) — milestones e evidências;
- [`docs/sdd/06-traceability.md`](docs/sdd/06-traceability.md) — matriz de rastreabilidade;
- [`docs/sdd/07-validation-report.md`](docs/sdd/07-validation-report.md) — auditoria final;
- [`ADR-0001`](docs/sdd/adr/0001-modular-monolith.md), [`ADR-0002`](docs/sdd/adr/0002-sqlite-persistence.md), [`ADR-0003`](docs/sdd/adr/0003-jwt-authentication.md) e [`ADR-0004`](docs/sdd/adr/0004-nginx-same-origin.md).

## Decisões e trade-offs

- SQLite, migrations no startup e uma única instância mantêm a demonstração simples; não são uma estratégia de rollout concorrente.
- Alterações de senha usam compare-and-swap pelo hash observado; o health compara os IDs exatos das migrations. Operações SQLite e a preparação do lock técnico usam timeout de 5 segundos; o startup completo de migrations tem deadline de 15 segundos e encerra normalmente quando `SIGTERM` chega antes da prontidão. O proxy usa 30 segundos depois que a API já está atendendo.
- `sessionStorage` reduz persistência entre sessões do navegador, mas continua acessível a JavaScript; token curto e dependências controladas limitam parte da exposição, porém uma CSP não está configurada e permanece hardening de produção.
- Não há refresh ou revogação: logout e troca de senha limpam a sessão cliente, enquanto um token capturado permanece válido até `exp`.
- O `409` de cadastro torna email duplicado observável para cumprir o requisito; o login usa sempre o mesmo `401` genérico.
- A chave efêmera permite `docker compose up` sem segredo versionado; uma chave externa é necessária para sessões estáveis entre restarts e para qualquer ambiente não Development.
- Mutation testing cobre somente lógica crítica do backend e roda manualmente/semanalmente; o custo de multiplicar a suíte HTTP/SQLite não é imposto a cada PR. O ratchet deriva da baseline real, não de uma meta arbitrária, e não há mutation testing frontend.

## Limitações conhecidas

- ambiente de demonstração em HTTP, com Swagger exposto e sem terminação TLS;
- SQLite e migrations no startup suportam uma única instância;
- sem confirmação/recuperação de email, roles, administração, refresh token ou deploy de produção;
- CI hospedada, inclusive o workflow semanal/manual de mutação, ainda não foi observada porque o repositório não foi associado/publicado;
- E2E cobre Chromium e três jornadas deliberadamente pequenas, não uma matriz de browsers;
- a demonstração não possui rate limiting ou lockout em cadastro/login; por isso o bind fica restrito ao loopback e exposição externa exige hardening adicional;
- não há header CSP; reduzir o risco de XSS em produção exige política de conteúdo, TLS e revisão dos assets permitidos;
- o Dockerfile do frontend usa o modelo de usuário padrão da imagem oficial Nginx, sem `USER` explícito; o container não é privilegiado, não recebe bind mount do host e publica somente a porta alta 8080 no loopback.

## Uso de IA

IA apoiou requisitos, design, implementação incremental, testes e revisões independentes. Nenhuma afirmação de conclusão foi aceita sem inspeção do código e execução real. O registro responsável está em [`docs/sdd/ai-usage.md`](docs/sdd/ai-usage.md); a capacidade do responsável humano de explicar a solução permanece uma verificação humana.

## Roteiro técnico para apresentação

1. Mostre `compose.yaml`: apenas o Nginx publica `127.0.0.1:8080`; API e SQLite ficam internos.
2. Siga cadastro/login nos Controllers: normalização e índice único no banco, hash Identity e `401` genérico.
3. Siga um endpoint de perfil: o `sub` vira o único ID consultado e DTOs impedem overposting/dados sensíveis.
4. Mostre guard/interceptor/sessionStorage e explique expiração curta, logout cliente e ausência de refresh.
5. Execute os profiles Docker, a mutação backend e as três jornadas; relacione cada evidência à matriz SDD.

## Troubleshooting

- **Porta 8080 ocupada:** encerre o processo conflitante ou ajuste temporariamente o mapeamento local; a URL oficial validada é 8080.
- **`localhost` resolve somente para IPv6:** use
  `http://127.0.0.1:8080`; o bind validado da demonstração é IPv4.
- **Health não fica saudável:** execute `docker compose ps` e `docker compose logs --no-color api web`; erros de migration/configuração fazem startup falhar de propósito.
- **Token deixa de funcionar após restart:** configure `Jwt__SigningKey` estável; usuários continuam no volume.
- **Reset completo:** use `docker compose down --volumes --remove-orphans` e suba novamente.
- **Mutation testing demora:** a baseline levou cerca de cinco minutos sem contenção e pode se aproximar do timeout de 90 minutos em host compartilhado; não aumente workers nem reduza a suíte para mascarar o ambiente.
