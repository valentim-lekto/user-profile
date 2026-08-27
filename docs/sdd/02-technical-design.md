# 02 — Design técnico

**Status:** aprovado para implementação · **Data:** 2026-08-24 · **Fontes normativas:** [`00-challenge.md`](00-challenge.md), [`01-requirements.md`](01-requirements.md) e [`03-api-contract.yaml`](03-api-contract.yaml)

## Objetivo

Definir a menor arquitetura que atende ao desafio completo com fronteiras claras, segurança proporcional e execução reproduzível por Docker Compose. Este documento fecha as decisões que estavam pendentes em `01-requirements.md`; ele não implementa a aplicação.

## Princípios

- Um monólito modular implantado como uma única API, sem distribuição interna.
- Organização por funcionalidades e fluxo direto; sem CQRS, MediatR, AutoMapper, generic repository ou camadas artificiais.
- Contratos HTTP por DTOs; entidades de persistência nunca atravessam a borda da API.
- Validação no frontend para feedback rápido e no backend como autoridade.
- Testes de integração exercitam o pipeline HTTP e SQLite reais; mocks ficam restritos às bordas do frontend.
- Uma única origem no Docker elimina CORS como preocupação da entrega.

## Visão de execução

```text
Navegador
   |
   | http://localhost:8080
   v
Nginx / Angular estático
   |-- / e rotas SPA --------> arquivos Angular
   |-- /api/*, /swagger/*
   |   e /health ------------> UserProfile.Api:8080
                                    |
                                    v
                              /data/user-profile.db
                              volume Docker nomeado
```

Somente o Nginx publica porta no host. A API fica acessível apenas na rede do Compose. O Nginx encaminha `/api/*`, `/swagger/*` e `/health`, preserva método, corpo e status e usa fallback para `index.html` somente nas rotas da SPA.

## Versões verificadas e fixação

Verificação realizada em 2026-08-24. Foram escolhidas versões estáveis e suportadas, sem previews. Dependências de aplicação usarão versões exatas e lockfiles; imagens nunca usarão `latest` nem aliases flutuantes.

| Componente | Versão escolhida | Motivo e suporte | Fixação prevista |
|---|---|---|---|
| .NET / ASP.NET Core | .NET 10 LTS; runtime `10.0.11` | Linha LTS ativa até novembro de 2028. | TFM `net10.0`; pacotes Microsoft `10.0.11`. |
| .NET SDK | `10.0.400` | SDK estável que inclui o runtime `10.0.11`. | `global.json` com `rollForward: disable` e `mcr.microsoft.com/dotnet/sdk:10.0.400-noble`. |
| EF Core SQLite | `10.0.11` | Mesma linha e patch do runtime; EF Core 10 é LTS. | Pacotes EF Core Microsoft em `10.0.11` e `packages.lock.json` versionado por projeto. |
| Swagger UI | Swashbuckle.AspNetCore `10.2.3` | Release estável compatível com `net10.0`. | `PackageReference` exato e lock NuGet. |
| Angular e Angular CLI | `22.1.3` | Release estável em suporte ativo; sem uso da linha `next`. | Dependências exatas e `package-lock.json`. |
| Angular Material | `22.1.3` | Alinhada à linha do framework. | Dependência exata e `package-lock.json`. |
| Angular ESLint | `22.1.0` | Linha estável alinhada ao Angular 22. | Dependência de desenvolvimento exata e `package-lock.json`. |
| Vitest | `4.1.11` | Patch estável fora da faixa afetada por `GHSA-5xrq-8626-4rwp`. | Dependência de desenvolvimento exata e `package-lock.json`. |
| Playwright | `1.62.0` | Release estável verificada no M5; pacote e imagem devem permanecer na mesma versão. | `@playwright/test` exato, lock npm e `mcr.microsoft.com/playwright:v1.62.0-noble`. |
| Ruby do validador OpenAPI | `3.4.10` | Runtime estável usado somente pelo perfil de contrato. | `ruby:3.4.10-slim-bookworm`. |
| Node.js | `24.19.0` LTS | Compatível com Angular 22 (`^24.15.0`) e em LTS. | `node:24.19.0-bookworm-slim`. |
| npm | `11.17.0` | Versão da imagem Node fixada com suporte à política `allowScripts`. | Versão conferida no Dockerfile, `packageManager` exato e `.npmrc` com `strict-allow-scripts=true`. |
| Nginx | `1.30.4` estável | Linha estável da imagem oficial. | `nginx:1.30.4-alpine3.24-slim`. |
| Runtime da API | ASP.NET `10.0.11` | Mesma atualização de segurança da aplicação. | `mcr.microsoft.com/dotnet/aspnet:10.0.11-noble`. |
| GitHub Actions | checkout `6.0.2`; upload-artifact `7.0.1` | Releases oficiais usadas pelo workflow único. | SHAs completos `de0fac2e4500dabe0009e67214ff5f5447ce83dd` e `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`. |

Fontes oficiais consultadas:

- [política de suporte do .NET](https://dotnet.microsoft.com/en-us/platform/support/policy/dotnet-core), [download do .NET 10](https://dotnet.microsoft.com/en-us/download/dotnet/10.0) e [release do EF Core 10](https://learn.microsoft.com/en-us/ef/core/what-is-new/ef-core-10.0/whatsnew);
- [tags oficiais do SDK .NET](https://github.com/dotnet/dotnet-docker/blob/main/README.sdk.md) e [tags oficiais do runtime ASP.NET](https://github.com/dotnet/dotnet-docker/blob/main/README.aspnet.md);
- [pacote oficial do Swashbuckle.AspNetCore](https://www.nuget.org/packages/Swashbuckle.AspNetCore);
- [política de releases do Angular](https://angular.dev/reference/releases) e [compatibilidade Angular–Node](https://angular.dev/reference/versions);
- [pacotes oficiais do Angular ESLint](https://www.npmjs.com/org/angular-eslint);
- [advisory oficial do Vitest](https://github.com/advisories/GHSA-5xrq-8626-4rwp);
- [documentação oficial da imagem Docker do Playwright](https://playwright.dev/docs/docker);
- [política oficial de install scripts do npm](https://docs.npmjs.com/cli/v11/commands/npm-rebuild/#strict-allow-scripts);
- [manifesto oficial da imagem Ruby](https://github.com/docker-library/official-images/blob/master/library/ruby);
- [releases oficiais de actions/checkout](https://github.com/actions/checkout/releases) e [actions/upload-artifact](https://github.com/actions/upload-artifact/releases);
- [releases suportadas do Node.js](https://nodejs.org/en/about/previous-releases), [manifesto oficial da imagem Node](https://github.com/docker-library/official-images/blob/master/library/node) e [manifesto oficial da imagem Nginx](https://github.com/docker-library/official-images/blob/master/library/nginx).

O scaffold de M1 reconfirmou as tags exatas ao construir as imagens. Uma atualização posterior exige mudança explícita deste documento e validação completa; não se deve substituir silenciosamente uma tag por alias flutuante.

Depois que todos os `PackageReference` de M1 estiverem definidos, o bootstrap executa uma única vez `dotnet restore UserProfile.sln --use-lock-file`, revisa e versiona os `packages.lock.json` gerados por projeto. Somente depois disso restores recorrentes, Docker e CI usam `--locked-mode`. Assim, mudança do grafo NuGet ou do SDK exige atualização explícita dos arquivos de fixação, em vez de ser aceita silenciosamente.

## Estrutura lógica da solução

Estrutura incremental adotada. M1 materializou a solution, `Data`, a operação de health, o workspace Angular e os diretórios de testes; pastas das funcionalidades futuras serão criadas somente no milestone que as implementar:

```text
UserProfile.sln
src/
  backend/
    UserProfile.Api/                  # único executável de backend
      Features/
        Auth/                         # cadastro e login
        Profile/                      # consulta e alterações do usuário atual
      Data/                           # User, DbContext e migrations
      Security/                       # emissão/configuração JWT e normalização
      Program.cs
  frontend/
    user-profile-web/                 # Angular standalone
      src/app/
        core/                         # sessão, guard e interceptor funcionais
        features/                     # register, login, dashboard e profile
tests/
  backend/
    UserProfile.Api.IntegrationTests/ # único projeto de integração do backend
  e2e/                                # três jornadas Playwright de M5
```

Os testes focados de frontend permanecem no workspace Angular. As poucas jornadas
Playwright ficam em `tests/e2e`, como indicado na árvore, sem criar outro projeto
de aplicação.

### Backend

- `AuthController` implementa cadastro e login.
- `ProfileController` implementa consulta, atualização de nome/email e alteração de senha.
- Controllers usam `UserProfileDbContext`, `IPasswordHasher<User>` e um emissor concreto de JWT por injeção de dependência.
- Regras compartilhadas pequenas, como normalização de email e leitura segura do claim `sub`, ficam em funções/classes concretas e focadas.
- Não haverá interfaces de fachada com uma única implementação, repositório sobre o EF Core ou mapeamento automático.
- `GET /health` usa health checks do ASP.NET Core e verifica que o banco está acessível.

### Frontend

- Componentes e rotas são standalone e compilados em strict mode.
- Reactive Forms implementam as mesmas validações visíveis definidas no contrato.
- Services encapsulam HTTP e estado simples por signals (`loading`, `data`, `error`); não haverá store global nem NgRx.
- O estado de leitura do perfil pertence à ativação corrente da rota protegida. Ao sair e entrar novamente, o dashboard recebe uma nova instância desse estado; uma resposta iniciada pela sessão anterior não pode bloquear nem preencher a sessão seguinte.
- Um functional interceptor anexa o Bearer somente às URLs relativas protegidas `GET /api/profile`, `PUT /api/profile` e `PUT /api/profile/password` quando existe token. Login, cadastro, health, URLs absolutas e qualquer outro destino nunca recebem o token.
- Um `401` recebido por uma chamada protegida só remove a sessão se o token corrente ainda for o mesmo Bearer anexado àquela chamada; uma resposta tardia de sessão anterior não encerra uma autenticação mais recente.
- Um functional route guard bloqueia rotas protegidas quando a sessão está ausente ou expirada.
- A API continua sendo a autoridade: presença ou conteúdo decodificado do token no browser não concede autorização.

## Modelo de dados

Entidade única `User`:

| Campo | Tipo C# planejado | Persistência SQLite | Regra |
|---|---|---|---|
| `Id` | `Guid` | `TEXT`, chave primária | Gerado pelo backend. |
| `Name` | `string` | `TEXT NOT NULL` | Remover espaços externos; validar comprimento entre 3 e 200 após essa remoção. |
| `Email` | `string` | `TEXT NOT NULL` | Remover espaços externos, limitar a 320 caracteres, aceitar somente a política ASCII explícita com uma única `@` e domínio com ponto, e preservar a caixa restante para exibição. |
| `NormalizedEmail` | `string` | `TEXT NOT NULL` | `Email.Trim().ToUpperInvariant()`; índice único. |
| `PasswordHash` | `string` | `TEXT NOT NULL` | Produzido e verificado por `PasswordHasher<User>`; nunca retornado ou logado. |
| `CreatedAtUtc` | `DateTime` | `TEXT NOT NULL` | Instante UTC definido na criação. |
| `UpdatedAtUtc` | `DateTime` | `TEXT NOT NULL` | Instante UTC atualizado a cada alteração persistida. |

Não haverá seed obrigatório. Hash, email normalizado e timestamps são internos
e não fazem parte dos DTOs públicos. O ID imutável aparece somente em
`ProfileResponse`, junto de nome e email. Os timestamps são uma decisão interna do
design registrada originalmente em `b184432` e formalizada no ADR-0002, não um
requisito do enunciado; seu ciclo de vida será implementado e testado nas fatias
M2 e M4 sem ampliar o contrato HTTP.

### Normalização e unicidade

- `PREM-INPUT-01` é uma decisão interna defensiva de M2, não requisito original: nome/email/senhas têm limites superiores `200/320/128`, request bodies têm limite de 1 MiB e emails aceitos ficam no subconjunto ASCII definido abaixo.
- Cadastro, login e alteração de email calculam `NormalizedEmail` da mesma forma.
- `Email` armazena o valor aparado para exibição; `NormalizedEmail` existe apenas para busca e unicidade.
- Como todo email aceito é ASCII, `Email.Trim().ToUpperInvariant()` produz uma chave canônica sem as lacunas de case folding Unicode que permitiriam duas contas equivalentes, como `ß` e `ẞ`.
- A atualização cadastral valida todo o request antes de mutar a entidade e persiste `Name`, `Email`, `NormalizedEmail` e `UpdatedAtUtc` em um único `SaveChangesAsync`; validação ou conflito não deixam alterações parciais.
- A troca de senha só atribui o novo hash e `UpdatedAtUtc` depois de validar integralmente senha atual, nova senha e confirmação; qualquer falha preserva a entidade sem persistência parcial.
- O índice único `UX_Users_NormalizedEmail` é a garantia autoritativa contra corrida.
- Uma consulta prévia pode melhorar a mensagem, mas violação do índice também deve ser convertida em `409 Conflict`.
- Na edição, o próprio email normalizado é permitido; somente outro usuário gera conflito.

### Migrations

A API aplica `Database.MigrateAsync()` antes de começar a atender requisições. Isso é aceitável apenas porque a entrega executa uma única instância de demonstração com SQLite. Falha de migration impede a prontidão e encerra o startup; não há tentativa de continuar com esquema parcial.

Em uma implantação concorrente ou de produção, migrations seriam uma etapa separada. Esse cenário está fora de escopo.

O health check consulta a tabela de histórico de migrations, não apenas abre uma
conexão. O comando dessa consulta usa timeout explícito de 1 segundo, independente
do timeout geral da conexão, para não manter threads ocupadas depois dos limites
de 2 segundos da probe do Compose. O proxy admite até 30 segundos de leitura para
acomodar o primeiro hash de senha sob contenção; esse limite é independente do SLO
de menos de 5 segundos exigido pelo teste de indisponibilidade do health. Falha durante o startup
mantém o serviço fora do ar; `503` representa uma perda de acesso ao SQLite depois
de um startup bem-sucedido. O teste dessa transição usa bloqueio exclusivo do
arquivo SQLite temporário, conserva o timeout padrão de 30 segundos na conexão da
API e exige resposta em menos de 5 segundos, sem substituir o health check por um
mock.

## Contrato HTTP

O contrato normativo está em [`03-api-contract.yaml`](03-api-contract.yaml). A API possui somente estas operações:

| Operação | Autenticação | Sucesso | Erros esperados |
|---|---|---|---|
| `POST /api/auth/register` | Pública | `201` com mensagem, sem token | `400`, `409`, `413`, `415`, `500`, `503` |
| `POST /api/auth/login` | Pública | `200` com JWT curto | `400` para payload inválido; `401` genérico para credenciais não reconhecidas; `413`, `415`, `500`, `503` |
| `GET /api/profile` | Bearer | `200` com ID imutável, nome e email | `401`, `404`, `500`, `503` |
| `PUT /api/profile` | Bearer | `200` com nome e email atualizados | `400`, `401`, `404`, `409`, `413`, `415`, `500`, `503` |
| `PUT /api/profile/password` | Bearer | `200` com mensagem, sem novo token | `400`, `401`, `404`, `413`, `415`, `500`, `503` |
| `GET /health` | Pública | `200` | `503`, `500` |

Não existe endpoint de dashboard: a tela usa `GET /api/profile`. Não existe endpoint de logout, refresh, perfil por ID ou qualquer operação fora do escopo.

### DTOs e validação

- Requests usam nomes em inglês e incluem apenas os campos descritos no OpenAPI.
- Cadastro: `name`, `email`, `password`, `passwordConfirmation`.
- Login: `email`, `password`.
- Perfil: `name`, `email`.
- Senha: `currentPassword`, `newPassword`, `newPasswordConfirmation`.
- Nome é validado após `Trim`, com 3 a 200 caracteres. Email é validado após `Trim`, tem no máximo 320 caracteres e usa nas duas camadas o padrão ASCII `^[\x21-\x3F\x41-\x7E]+@[A-Za-z0-9-]+(?:\.[A-Za-z0-9-]+)+$`; o schema de request modela separadamente a remoção de espaços externos para não aplicar limites JSON ao valor bruto errado.
- Senha não é aparada nem normalizada e aceita 6 a 128 caracteres nas operações que definem uma senha nova. Todos os caracteres são significativos; uma senha composta somente por espaços é válida se respeitar o comprimento, pois adicionar regra de complexidade seria novo requisito.
- Entradas de senha existentes, como login e senha atual, também são limitadas a 128 caracteres antes do `PasswordHasher`, evitando trabalho desnecessário com payloads defensivamente grandes.
- A confirmação deve corresponder exatamente à senha correspondente.
- DTOs de request rejeitam propriedades JSON não mapeadas ou com caixa diferente do nome camelCase normativo com `400`; os schemas OpenAPI usam `additionalProperties: false`.
- Nenhuma operação de perfil define `userId` em path, query, header ou body. Um `userId` extra no corpo é rejeitado, e valores arbitrários em query/header nunca participam da resolução de identidade.
- Nenhuma resposta contém `PasswordHash`, senha ou `NormalizedEmail`. Somente `ProfileResponse` expõe o `Id` imutável exigido pela fatia M3; cadastro e login não devolvem ID.

### Erros

- Erros usam `application/problem+json`.
- Falhas de campos e regras de formulário usam `ValidationProblemDetails` com `errors` indexado pelo nome do campo.
- Email duplicado usa `409 ProblemDetails`.
- Payload de login ausente ou estruturalmente inválido usa `400 ValidationProblemDetails`. Email inexistente e senha incorreta usam o mesmo `401 ProblemDetails`, com a mensagem genérica `Invalid email or password.`, challenge `WWW-Authenticate: Bearer` e nenhuma sessão.
- Toda resposta `401` inclui obrigatoriamente `WWW-Authenticate: Bearer`. Nos recursos protegidos, Bearer ausente, inválido ou expirado usa `401 ProblemDetails`, e o middleware deve manter cabeçalho e formato.
- Senha atual incorreta em sessão válida usa `400 ValidationProblemDetails`, não é confundida com falha do JWT.
- Exceções não tratadas são convertidas em `500 ProblemDetails` sem detalhes internos.
- Erros gerados pelo pipeline sob `/api`, como JSON malformado, media type não suportado (`415`), rota inexistente ou método não permitido, também usam `application/problem+json`.
- O Nginx limita o corpo da requisição a 1 MiB e converte sua rejeição `413` em `application/problem+json`; ela é uma barreira de transporte anterior à validação dos campos, não um substituto para os limites `200/320/128` da API.
- Se o Nginx não conseguir conectar à API ou atingir o timeout do upstream, converte apenas seus `502`/`504` de transporte em `503 ProblemDetails`; respostas já produzidas pela API são preservadas.

O `409` público de cadastro necessariamente revela que o email normalizado já existe. Esse risco de enumeração é aceito nesta demonstração porque `AC-REG-05` e `AC-REG-06` exigem rejeição e feedback de erro observável; login continua usando corpo genérico e nenhum dado adicional do usuário é exposto.

## Autenticação e sessão

### Senhas

- `PasswordHasher<User>` do ASP.NET Core, no formato Identity V3 padrão, cria e verifica os hashes.
- Senhas e hashes nunca entram em DTOs, logs, mensagens de exceção ou exemplos utilizáveis.

### JWT

- Bearer assinado com HMAC SHA-256 e chave de pelo menos 256 bits. Quando externa, a chave é Base64 válido que decodifica para ao menos 32 bytes aleatórios.
- Duração: 15 minutos a partir da emissão; sem refresh token.
- Claims mínimas: `sub` com o `Guid` do usuário, `jti` único, `iat` e `exp`.
- Validação obrigatória de issuer, audience, assinatura e expiração, com tolerância de relógio de 30 segundos.
- `MapInboundClaims` fica desabilitado para que o backend leia diretamente o claim chamado `sub`; `sub`, `jti`, `iat` ou `exp` ausente/malformado invalida o token com `401`.
- Valores padrão não sensíveis da demonstração: issuer `UserProfile.Api` e audience `UserProfile.Web`; ambos permanecem configuráveis.
- O backend nunca aceita identidade enviada pelo cliente; converte exclusivamente o claim `sub` validado para `Guid`.

Um `sub` bem formado que já não corresponde a um usuário retorna `404`; um `sub` ausente ou inválido nunca chega à resolução de perfil e retorna `401`.

Após alteração de senha, o frontend remove o token e encerra a sessão. Tokens já emitidos não são revogados no servidor e permanecem tecnicamente válidos até `exp`; essa consequência é aceita pelo token curto e pela ausência aprovada de refresh/revogação.

### Chave de assinatura e Compose sem `.env`

- Fora de `Development`, chave ausente, Base64 inválido ou valor decodificado menor que 32 bytes impede o startup. Em qualquer ambiente, uma chave externa presente mas inválida falha de modo fechado.
- No Compose de demonstração, em `Development`, a API gera em memória uma chave criptograficamente aleatória por processo quando nenhuma chave externa é fornecida.
- A chave gerada não é persistida nem logada; reiniciar a API invalida sessões existentes.
- Testes geram sua própria chave em runtime.
- Um `.env.example` versionado em M1, sem valor real utilizável, documenta substituições opcionais; `docker compose up` não depende de sua cópia.

Essa regra concilia execução sem preparação manual com a proibição de versionar segredos.

## Fluxos do frontend

| Rota | Proteção | Fonte de dados e comportamento |
|---|---|---|
| `/register` | Pública | Formulário reativo; `201` navega para `/login` com aviso de sucesso e sem criar sessão. |
| `/login` | Pública | Formulário reativo; `200` grava o JWT em `sessionStorage` e navega para `/dashboard`. |
| `/dashboard` | Guard | Consulta `/api/profile` a cada ativação, mostra boas-vindas com `name` e link para `/profile`; portanto reflete uma edição persistida quando consultado novamente. |
| `/profile` | Guard | Consulta e altera nome/email; oferece formulário separado para senha, sem misturar campos de senha no payload cadastral. |

Cada operação assíncrona expõe estado de carregamento, impede submissão duplicada e apresenta sucesso ou erro. Nos dois formulários de edição, o `FormGroup` correspondente permanece desabilitado durante a requisição para que uma resposta não sobrescreva uma entrada posterior nem associe a ela um erro produzido para valores anteriores. O estado de carregamento do perfil é recriado a cada ativação do dashboard, isolando respostas pendentes de uma sessão encerrada. O interceptor reage a `401` global somente quando a requisição levava Bearer e a sessão corrente ainda contém exatamente aquele token; nesse caso limpa a sessão e conduz ao login. Pelo mesmo isolamento, o sucesso da troca de senha remove o token e navega ao login somente se a sessão corrente ainda contém o token capturado no início daquela operação; uma resposta tardia não afeta uma autenticação posterior. O `401` esperado do próprio login não levava Bearer, portanto permanece disponível para a mensagem genérica da tela e não dispara limpeza/navegação global.

O token fica somente em `sessionStorage`. O estado de autenticação é um signal derivado da presença e do `exp` do token; decodificar o payload no cliente serve apenas à experiência de navegação.

## Docker e rede

- `compose.yaml` terá serviços `web` e `api` e um volume nomeado para `/data`; SQLite não cria um terceiro contêiner.
- `web` é uma imagem multi-stage: Node compila o Angular e Nginx serve o resultado.
- `api` é uma imagem multi-stage: SDK publica e runtime ASP.NET executa como usuário não-root; `/data` é preparado com permissão de escrita para esse usuário antes de receber o volume.
- Perfis opt-in do Compose executam restore/build/test do backend, `npm ci`/lint/test/build do frontend, contrato OpenAPI e as três jornadas Playwright sem exigir SDKs no host. O `npm ci` reprova install scripts fora da allowlist versionada. Esses serviços não publicam portas nem alteram a pilha padrão.
- Cada execução da suíte E2E recebe projeto Compose, volume e diretório de artefatos próprios; cada jornada recebe contexto e dados próprios. Emails são únicos; senhas sintéticas são geradas e mantidas dentro do contexto do navegador, e o runner Playwright recebe somente chaves não sensíveis. Relatórios JUnit/HTML são gravados sempre que o runner Playwright chega a iniciar; screenshot de inputs mascarados e trace minimizado, sem snapshots, sources ou attachments, são retidos somente em falha. Falhas anteriores ao runner preservam os diagnósticos do Compose disponíveis. O `finally` limpa os inputs em melhor esforço, sem ser tratado como a única defesa de artefatos. Os traps de E2E e smoke registram o nome do projeto, persistem `ps`, imagens/serviços e logs processados por um filtro compartilhado/testado antes do teardown quando há falha, e nunca publicam a cópia bruta. Falha de teardown reprova uma execução antes bem-sucedida, preserva a falha primária quando já existe e deixa saída filtrada; a CI tenta novamente somente projetos sob seu prefixo único e faz upload depois do cleanup.
- `web` publica `127.0.0.1:8080:8080`, restringindo a demonstração HTTP ao
  loopback IPv4 do host; `api` expõe `8080` apenas para a rede interna.
- Nginx escuta em `8080`, encaminha `/api/`, `/swagger/` e `/health` para `http://api:8080`, usa timeout explícito de conexão de 2 segundos e de resposta de 30 segundos, converte falha de conexão/timeout do upstream em `503 application/problem+json`, converte corpo acima de 1 MiB em `413 application/problem+json`, devolve `404` para assets com extensão que não existem e usa fallback para `index.html` somente nas rotas da SPA. A janela de resposta acomoda o primeiro hash de senha em runners Docker sob contenção sem criar retry; indisponibilidade de conexão continua falhando rapidamente.
- Existe um único health check do Compose no serviço `web`: `wget -q -O /dev/null http://127.0.0.1:8080/health`, usando o BusyBox presente na imagem Alpine. `web` depende de `api` com `condition: service_started`; como a probe atravessa Nginx, API e a consulta SQLite, `docker compose up --wait` só conclui quando a pilha inteira está saudável, sem instalar cliente HTTP na imagem da API.
- O inventário exato das imagens Compose, incluindo todos os perfis, é: `ruby:3.4.10-slim-bookworm`, `user-profile-api:0.1.0`, `user-profile-backend-tests:0.1.0`, `user-profile-e2e-tests:0.1.0`, `user-profile-frontend-tests:0.1.0` e `user-profile-web:0.1.0`. Todas as linhas `FROM` e esse conjunto renderizado são comparados às versões completas deste documento; `latest`, `lts`, `stable` ou apenas major/minor são proibidos. Todas as linhas `uses:` de terceiros na CI usam SHA completo e pertencem ao inventário aprovado; o checkout não persiste credenciais Git.

No desenvolvimento local, o proxy do Angular CLI encaminha `/api`, `/swagger` e `/health` para a API, mantendo URLs relativas. Não haverá configuração CORS permissiva para compensar URLs absolutas.

## Configuração e observabilidade

Configurações previstas:

| Chave | Sensível | Regra |
|---|---|---|
| `ConnectionStrings__Default` | Não | No Compose, aponta para `/data/user-profile.db`. |
| `Jwt__Issuer` | Não | Valor padrão de demonstração substituível. |
| `Jwt__Audience` | Não | Valor padrão de demonstração substituível. |
| `Jwt__LifetimeMinutes` | Não | `15`; mudança exige revisão deste design. |
| `Jwt__SigningKey` | Sim | Base64 de ao menos 32 bytes aleatórios; fallback aleatório somente quando ausente em `Development`. |

Logs estruturados podem registrar método, rota sem query string, status, duração e um identificador de correlação gerado para a requisição, mas nunca argumentos da URL, corpo de requests de autenticação, cabeçalho de autorização, token, senha, hash ou chave. O access log do Nginx usa `$uri`, nunca `$request`/`$request_uri`/`$args`, e os diagnostics de acesso padrão do ASP.NET Core que incluem `QueryString` ficam abaixo do nível habilitado. Respostas `ProblemDetails` não incluem stack trace, SQL nem caminhos internos.

## Decisões registradas

- [`ADR-0001`](adr/0001-modular-monolith.md) — monólito modular e estrutura mínima.
- [`ADR-0002`](adr/0002-sqlite-persistence.md) — SQLite, volume e migrations no startup.
- [`ADR-0003`](adr/0003-jwt-authentication.md) — JWT curto, `sub`, sessão e chave.
- [`ADR-0004`](adr/0004-nginx-same-origin.md) — Nginx e origem única.

## Limites deliberados

Não serão adicionados versionamento de API, refresh/revogação de token, Identity completo, roles, mensageria, cache distribuído, store global, abstração de repositório, múltiplas APIs ou contêiner de banco. A complexidade mantida — autenticação, índice único, proxy, migrations e testes ponta a ponta — existe porque critérios explícitos a exigem.
