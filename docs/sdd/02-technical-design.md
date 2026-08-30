# 02 — Design técnico

**Status:** aprovado e implementado em M1–M6; mutation testing, refinamento visual/responsivo, robustez SQLite, queries/startup e seus oráculos pós-M6 validados localmente · **Data:** 2026-08-30 · **Fontes normativas:** [`00-challenge.md`](00-challenge.md), [`01-requirements.md`](01-requirements.md) e [`03-api-contract.yaml`](03-api-contract.yaml)

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
   |-- / e rotas SPA ----------------> arquivos Angular
   |-- POST /api/auth/login|register
   |   -> limiter local por IP+URI ---\
   |-- demais /api/*, /swagger/*      |
   |   e /health ---------------------> UserProfile.Api:8080
                                    |
                                    v
                              /data/user-profile.db
                              volume Docker nomeado
```

Somente o Nginx publica porta no host. A API fica acessível apenas na rede do Compose. O Nginx encaminha `/api/*`, `/swagger/*` e `/health`, preserva método, corpo e status e usa fallback para `index.html` somente nas rotas da SPA. Antes do proxy, somente os `POST` públicos de login e cadastro atravessam um limiter local com buckets independentes por endereço TCP e endpoint.

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
| Stryker.NET | `dotnet-stryker` `4.16.0` | Versão estável verificada para mutation testing do backend após M6. | Tool manifest raiz, versão exata e restore dentro do target Docker de mutação. |
| GitHub Actions | checkout `6.0.2`; upload-artifact `7.0.1` | Releases oficiais usadas pelos workflows. | SHAs completos `de0fac2e4500dabe0009e67214ff5f5447ce83dd` e `043fb46d1a93c77aae656e7c1c64a875d1fc6a0a`. |

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
- [pacote oficial do dotnet-stryker](https://www.nuget.org/packages/dotnet-stryker) e [configuração oficial do Stryker.NET](https://stryker-mutator.io/docs/stryker-net/configuration/);
- [releases oficiais de actions/checkout](https://github.com/actions/checkout/releases) e [actions/upload-artifact](https://github.com/actions/upload-artifact/releases);
- [releases suportadas do Node.js](https://nodejs.org/en/about/previous-releases), [manifesto oficial da imagem Node](https://github.com/docker-library/official-images/blob/master/library/node) e [manifesto oficial da imagem Nginx](https://github.com/docker-library/official-images/blob/master/library/nginx).

O scaffold de M1 reconfirmou as tags exatas ao construir as imagens. Uma atualização posterior exige mudança explícita deste documento e validação completa; não se deve substituir silenciosamente uma tag por alias flutuante.

Depois que todos os `PackageReference` de M1 estiverem definidos, o bootstrap executa uma única vez `dotnet restore UserProfile.sln --use-lock-file`, revisa e versiona os `packages.lock.json` gerados por projeto. Somente depois disso restores recorrentes, Docker e CI usam `--locked-mode`. Assim, mudança do grafo NuGet ou do SDK exige atualização explícita dos arquivos de fixação, em vez de ser aceita silenciosamente.

## Estrutura lógica da solução

Estrutura incremental adotada. M1 materializou a solution, `Data`, a operação de health, o workspace Angular e os diretórios de testes; pastas das funcionalidades futuras serão criadas somente no milestone que as implementar:

```text
UserProfile.sln
.config/
  dotnet-tools.json                  # dotnet-stryker 4.16.0 fixado
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
    UserProfile.Api.IntegrationTests/ # único projeto de integração e configuração Stryker
  e2e/                                # três jornadas Playwright de M5
.github/workflows/
  ci.yml                              # gate de push/PR
  mutation.yml                        # gate manual/semanal pós-M6
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
- O contrato comum `ProblemDetails` e sua leitura defensiva ficam em um único módulo de `core/http`, reutilizado pelos services de cadastro, autenticação e perfil. Esse limite compartilhado aceita somente `status` numérico, `title`/`detail` textuais e mapas de `string[]`; não introduz service base, wrapper HTTP ou hierarquia.
- O estado de leitura do perfil pertence à ativação corrente da rota protegida. Ao sair e entrar novamente, o dashboard recebe uma nova instância desse estado; uma resposta iniciada pela sessão anterior não pode bloquear nem preencher a sessão seguinte.
- Um functional interceptor anexa o Bearer somente às URLs relativas protegidas `GET /api/profile`, `PUT /api/profile` e `PUT /api/profile/password` quando existe token válido. Sem token válido, uma dessas chamadas é cancelada no cliente e conduz ao login, em vez de sair anonimamente. Login, cadastro, health, URLs absolutas e qualquer outro destino nunca recebem o token.
- Um `401` recebido por uma chamada protegida só remove a sessão se o token corrente ainda for o mesmo Bearer anexado àquela chamada; uma resposta tardia de sessão anterior não encerra uma autenticação mais recente.
- Um functional route guard bloqueia rotas protegidas quando a sessão está ausente ou expirada.
- Ao carregar ou substituir um JWT válido, o serviço de autenticação agenda seu `exp`. Se o mesmo token ainda for a sessão corrente nesse instante, ele é removido e qualquer dashboard ou perfil ativo é substituído pelo login, mesmo quando a URL contém matrix params, query ou fragment; o timer de uma sessão anterior não afeta uma sessão posterior nem interrompe uma rota pública, e é cancelado quando o serviço é destruído.
- A API continua sendo a autoridade: presença ou conteúdo decodificado do token no browser não concede autorização.
- O refinamento visual pós-M6 preserva os componentes Material, a estrutura de headings, os nomes acessíveis e todos os estados já testados. A linguagem visual usa roxo como cor estrutural, amarelo e coral somente como acentos, superfícies claras, cantos amplos, tipografia sans-serif do sistema e formas orgânicas discretas. O site público da Lekto foi usado apenas como referência de composição e paleta: nenhum logo, texto, fonte, imagem ou outro ativo da empresa é copiado, e a aplicação não declara afiliação.
- Login e cadastro compartilham uma composição responsiva com painel editorial e cartão de formulário; dashboard usa um hero de boas-vindas, resumo dos dados de apresentação retornados por `GET /api/profile` e ações diretas para perfil/logout, sem cartões descritivos redundantes de dados pessoais, senha ou sessão; perfil mantém os dois formulários separados em cartões. O `id` do `ProfileResponse` permanece no contrato de transporte, mas não é renderizado: a interface apresenta somente nome e email. Em largura estreita, o conteúdo passa para uma coluna sem overflow horizontal; em viewport também baixa, o painel editorial não antecede o formulário funcional. Ações empilhadas preservam a ordem do DOM/foco, e o nome no limite de 200 caracteres permanece integral no DOM e no perfil, mas recebe limite visual de linhas no dashboard para manter as ações principais na primeira viewport. Foco, contraste, labels, `aria-live`, loading e bloqueio de submissão permanecem observáveis.

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
`ProfileResponse`, junto de nome e email, como dado técnico de transporte e não
como conteúdo visual da tela. Os timestamps são uma decisão interna do
design registrada originalmente em `b184432` e formalizada no ADR-0002, não um
requisito do enunciado; seu ciclo de vida será implementado e testado nas fatias
M2 e M4 sem ampliar o contrato HTTP.

### Normalização e unicidade

- `PREM-INPUT-01` é uma decisão interna defensiva de M2, não requisito original: nome/email/senhas têm limites superiores `200/320/128`, request bodies têm limite de 1 MiB e emails aceitos ficam no subconjunto ASCII definido abaixo.
- Cadastro, login e alteração de email calculam `NormalizedEmail` da mesma forma.
- `Email` armazena o valor aparado para exibição; `NormalizedEmail` existe apenas para busca e unicidade.
- Como todo email aceito é ASCII, `Email.Trim().ToUpperInvariant()` produz uma chave canônica sem as lacunas de case folding Unicode que permitiriam duas contas equivalentes, como `ß` e `ẞ`.
- A atualização cadastral valida todo o request antes de mutar a entidade e persiste `Name`, `Email`, `NormalizedEmail` e `UpdatedAtUtc` em um único `SaveChangesAsync`; validação ou conflito não deixam alterações parciais.
- A troca de senha lê o usuário sem tracking, valida integralmente senha atual, nova senha e confirmação e persiste `PasswordHash`/`UpdatedAtUtc` em um único `UPDATE` condicionado ao `Id` do `sub` e ao hash observado. Assim, duas requisições que validaram o mesmo hash anterior têm no máximo uma vencedora; zero linhas afetadas usa o mesmo `400 ValidationProblemDetails` de senha atual incorreta, sem persistência parcial.
- O índice único `UX_Users_NormalizedEmail` é a garantia autoritativa contra corrida.
- Uma consulta prévia pode melhorar a mensagem, mas violação do índice também deve ser convertida em `409 Conflict`.
- Na edição, o próprio email normalizado é permitido; quando a chave normalizada não mudou, a API não executa a consulta de conflito. Quando mudou, somente outro usuário gera conflito e o índice único continua protegendo a corrida.

### Migrations

A API executa a preparação do lock técnico e `Database.MigrateAsync()` em um `IHostedLifecycleService.StartingAsync`. O host registra primeiro o tratamento de sinais e somente depois chama esse lifecycle; todos os `StartingAsync` terminam antes de os serviços HTTP iniciarem. Assim, `SIGTERM` durante migrations alcança o token de startup sem abrir o listener. Antes da chamada do EF, `DROP TABLE IF EXISTS "__EFMigrationsLock"` remove exclusivamente o artefato técnico que pode ficar órfão quando um processo é interrompido; a tabela é recriada pelo próprio provider. Isso é aceitável apenas porque a entrega executa uma única instância de demonstração com SQLite.

A preparação usa timeout de comando de 5 segundos e toda a rotina recebe um deadline cooperativo independente de 15 segundos, ligado ao token do lifecycle. Como o provider SQLite executa parte das APIs ADO.NET assíncronas de forma síncrona, o token é observado entre operações e nas esperas cooperativas; o timeout SQLite continua sendo o limite das operações nativas bloqueadas. Dados e histórico de migrations não são removidos. Falha real ou deadline interrompem o startup com erro; o cancelamento solicitado pelo host durante `SIGTERM` propaga pelo lifecycle e é reconhecido apenas na fronteira de execução do processo para concluir o encerramento normalmente, sem abrir o listener nem continuar com esquema parcial.

Em uma implantação concorrente ou de produção, o startup não poderia remover um lock possivelmente pertencente a outra instância; migrations seriam uma etapa separada. Esse cenário está fora de escopo.

O health check abre uma única conexão e consulta os IDs da tabela de histórico de migrations, em vez de executar uma tentativa de conexão separada ou comparar apenas cardinalidades. Ele só fica saudável quando o conjunto
aplicado coincide exatamente com o conjunto esperado e existe ao menos uma
migration. O comando dessa consulta usa timeout explícito de 1 segundo,
independente do timeout geral da conexão, para não manter threads ocupadas depois
dos limites de 2 segundos da probe do Compose. O proxy admite até 30 segundos de
leitura para acomodar o primeiro hash de senha sob contenção; esse limite é
independente do SLO de menos de 5 segundos exigido pelo teste de indisponibilidade
do health. Falha durante o startup mantém o serviço fora do ar; `503` representa
uma perda de acesso ao SQLite depois de um startup bem-sucedido. O teste dessa
transição usa bloqueio exclusivo do arquivo SQLite temporário, conserva o timeout
padrão de 30 segundos na conexão da fixture para provar que o comando do health é
independente e exige resposta em menos de 5 segundos, sem substituir o health check
por um mock.

## Contrato HTTP

O contrato normativo está em [`03-api-contract.yaml`](03-api-contract.yaml). A API possui somente estas operações:

| Operação | Autenticação | Sucesso | Erros esperados |
|---|---|---|---|
| `POST /api/auth/register` | Pública | `201` com mensagem, sem token | `400`, `409`, `413`, `415`, `429`, `500`, `503` |
| `POST /api/auth/login` | Pública | `200` com JWT curto | `400` para payload inválido; `401` genérico para credenciais não reconhecidas; `413`, `415`, `429`, `500`, `503` |
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
- O Nginx limita rajadas somente de `POST /api/auth/login` e `POST /api/auth/register`. Cada endpoint possui bucket independente por IP observado, com zona de 1 MiB, `rate=10r/m`, `burst=9` e `nodelay`. Depois de dez tentativas imediatas, `429 application/problem+json` precede respostas ordinárias da API (`400`, `401`, `409` ou `415`) e inclui `Retry-After: 60` e `Cache-Control: no-store`. A barreira de corpo `413` continua independente.
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
| `/register` | Pública | Formulário reativo; `201` navega para `/login` com aviso de sucesso e sem criar sessão; `429` preserva os valores e mostra o aviso de espera sem navegar. |
| `/login` | Pública | Formulário reativo; `200` grava o JWT em `sessionStorage` e navega para `/dashboard`; `429` preserva os valores e mostra o aviso de espera sem criar sessão. |
| `/dashboard` | Guard | Consulta `/api/profile` a cada ativação, mostra boas-vindas com `name` e link para `/profile`; portanto reflete uma edição persistida quando consultado novamente. |
| `/profile` | Guard | Consulta e altera nome/email; não renderiza o `id` técnico recebido; oferece formulário separado para senha, sem misturar campos de senha no payload cadastral. |

Cada operação assíncrona expõe estado de carregamento, impede submissão duplicada e apresenta sucesso ou erro. Em login/cadastro, `429` encerra o loading, reabilita a ação, mantém todos os valores e usa o alerta acessível já existente com a mensagem fixa “Muitas tentativas. Aguarde um minuto e tente novamente.”; não há navegação, sessão ou countdown. Nos dois formulários de edição, o `FormGroup` correspondente permanece desabilitado durante a requisição para que uma resposta não sobrescreva uma entrada posterior nem associe a ela um erro produzido para valores anteriores. O estado de carregamento do perfil é recriado a cada ativação do dashboard, isolando respostas pendentes de uma sessão encerrada. O interceptor cancela e conduz ao login uma chamada protegida iniciada sem token válido. Quando existe Bearer, reage a `401` somente se a sessão corrente ainda contém exatamente aquele token; nesse caso limpa a sessão e conduz ao login. Pelo mesmo isolamento, o sucesso da troca de senha remove o token e navega ao login somente se a sessão corrente ainda contém o token capturado no início daquela operação; uma resposta tardia não afeta uma autenticação posterior. O `401` esperado do próprio login não levava Bearer, portanto permanece disponível para a mensagem genérica da tela e não dispara limpeza/navegação global.

O token fica somente em `sessionStorage`. O estado de autenticação é um signal derivado da presença e do `exp` do token. Um timer é rearmado ao carregar ou substituir a sessão e, ao alcançar `exp` no primeiro ciclo de execução disponível, confirma que o token ainda é o corrente antes de limpá-lo e reproteger uma rota ativa. Decodificar o payload no cliente serve apenas à experiência de navegação; a API continua validando o JWT em toda chamada recebida.

## Docker e rede

- `compose.yaml` terá serviços `web` e `api` e um volume nomeado para `/data`; SQLite não cria um terceiro contêiner.
- `web` é uma imagem multi-stage: Node compila o Angular e Nginx serve o resultado.
- `api` é uma imagem multi-stage: SDK publica e runtime ASP.NET executa como usuário não-root; `/data` é preparado com permissão de escrita para esse usuário antes de receber o volume.
- Perfis opt-in do Compose executam restore/build/test do backend, mutation testing crítico do backend, `npm ci`/lint/test/build do frontend, contrato OpenAPI e as três jornadas Playwright sem exigir SDKs no host. O `npm ci` reprova install scripts fora da allowlist versionada. Esses serviços não publicam portas nem alteram a pilha padrão.
- O runner Vitest do frontend usa timeout global e limitado de 30 segundos por teste, sem retry e sem overrides locais mais curtos. A margem cobre a latência observada dos testes Angular Material/Router em container frio ou sob contenção, preservando o timeout de 45 minutos do job como limite externo da suíte.
- Cada execução da suíte E2E recebe projeto Compose, volume e diretório de artefatos próprios; cada jornada recebe contexto e dados próprios. Emails são únicos; senhas sintéticas são geradas e mantidas dentro do contexto do navegador, e o runner Playwright recebe somente chaves não sensíveis. Relatórios JUnit/HTML são gravados sempre que o runner Playwright chega a iniciar; screenshot de inputs mascarados e trace minimizado, sem snapshots, sources ou attachments, são retidos somente em falha. Falhas anteriores ao runner preservam os diagnósticos do Compose disponíveis. O `finally` limpa os inputs em melhor esforço, sem ser tratado como a única defesa de artefatos. Os traps de E2E e smoke registram o nome do projeto, persistem `ps`, imagens/serviços e logs processados por um filtro compartilhado/testado antes do teardown quando há falha, e nunca publicam a cópia bruta. Falha de teardown reprova uma execução antes bem-sucedida, preserva a falha primária quando já existe e deixa saída filtrada; a CI tenta novamente somente projetos sob seu prefixo único e faz upload depois do cleanup.
- `web` publica `127.0.0.1:8080:8080`, restringindo a demonstração HTTP ao
  loopback IPv4 do host; `api` expõe `8080` apenas para a rede interna.
- Nginx escuta em `8080`, encaminha `/api/`, `/swagger/` e `/health` para `http://api:8080`, usa timeout explícito de conexão de 2 segundos e de resposta de 30 segundos, converte falha de conexão/timeout do upstream em `503 application/problem+json`, converte corpo acima de 1 MiB em `413 application/problem+json`, devolve `404` para assets com extensão que não existem e usa fallback para `index.html` somente nas rotas da SPA. Uma localização anterior ao proxy genérico aplica `limit_req` aos `POST` de login/cadastro. A zona `auth_rate_limit` tem 1 MiB; um `map` recebe `$request_method:$uri` e produz chave vazia fora desses `POSTs` ou `$binary_remote_addr` combinado ao endpoint canônico. Assim, query, caixa e barra final não multiplicam buckets aceitos pelo roteamento. `X-Forwarded-For` é sobrescrito com `$remote_addr` em todo proxy, por isso valor forjado pelo cliente não é tratado como cadeia confiável. A API do Compose limita a espera SQLite das operações e da preparação do lock técnico a 5 segundos; depois que a API começa a atender, essa margem reduz o risco de a contenção consumir toda a janela de 30 segundos do proxy. Separadamente, preparação e aplicação das migrations têm deadline total de 15 segundos durante o startup. A janela de resposta ainda acomoda o primeiro hash de senha em runners Docker sob contenção sem criar retry; indisponibilidade de conexão continua falhando rapidamente.
- Existe um único health check do Compose no serviço `web`: `wget -q -O /dev/null http://127.0.0.1:8080/health`, usando o BusyBox presente na imagem Alpine. `web` depende de `api` com `condition: service_started`; como a probe atravessa Nginx, API e a consulta SQLite, `docker compose up --wait` só conclui quando a pilha inteira está saudável, sem instalar cliente HTTP na imagem da API.
- O inventário exato das imagens Compose, incluindo todos os perfis, é: `ruby:3.4.10-slim-bookworm`, `user-profile-api:0.1.0`, `user-profile-backend-tests:0.1.0`, `user-profile-e2e-tests:0.1.0`, `user-profile-frontend-tests:0.1.0`, `user-profile-mutation-tests:0.1.0` e `user-profile-web:0.1.0`. O Dockerfile backend contém, nesta ordem, os stages `build`, `test`, `mutation-test` (derivado de `test`) e `final`; o frontend contém `dependencies`, `test`, `build` e o stage final Nginx; o E2E contém somente o stage Playwright. Todas as linhas `FROM` e esse conjunto renderizado são comparados às versões completas deste documento; `latest`, `lts`, `stable` ou apenas major/minor são proibidos. Todas as linhas `uses:` de terceiros nos workflows usam SHA completo e pertencem ao inventário aprovado; checkout não persiste credenciais Git.

### Mutation testing do backend após M6

`BE-MUT-001` mede a capacidade da suíte xUnit de detectar alterações artificiais na lógica crítica, sem mudar API, banco, frontend ou regras de negócio. O Stryker executa em `Release`, `net10.0`, nível `standard`, análise `perTest`, dois workers, timeout adicional de 5 segundos e interrompe se a suíte inicial estiver vermelha. Os reporters são `progress`, `html` e `json`; não há dashboard ou armazenamento externo.

O alvo é uma allowlist explícita e selecionada: Controllers e requests de autenticação/perfil (`AuthController`, `LoginRequest`, `RegisterRequest`, `ProfileController`, `ChangePasswordRequest`, `UpdateProfileRequest`), configuração/emissão JWT (`JwtBearerConfiguration`, `JwtTokenIssuer`, `JwtOptions`) e lógica de persistência/saúde (`DatabaseHealthCheck`, `UserConfiguration`). Migrations, DTOs passivos, filtros Swagger, `User`, `UserProfileDbContext`, factories, wiring de `Program.cs` e o lifecycle operacional de migrations ficam fora desse gate. O lifecycle não é classificado como wiring sem lógica: sua recuperação, deadline e propagação de cancelamento são cobertos por integrações de startup e por processo real; ampliar a allowlist exigiria uma decisão e baseline próprias. Não existe ignore global de mutações ou métodos; uma exclusão futura só pode ser pontual, junto ao mutante comprovadamente equivalente e com justificativa versionada.

O target `mutation-test` restaura o tool manifest e executa o único projeto xUnit existente. Depois do Stryker, um gate local exige o JSON, reprova `Timeout`, `NoCoverage` ou `RuntimeError` e aceita somente os três `CompileError` da baseline que geram C# inválido; assim, timeouts não podem passar apenas por serem contabilizados como mutantes detectados no score. O profile `mutation-tests` não inicia API, frontend, SQLite compartilhado nem publica portas; grava relatórios em `${MUTATION_ARTIFACTS_DIR:-./artifacts/mutation}`, diretório ignorado pelo Git. O comando público é:

```sh
docker compose --profile mutation-tests run --rm --build mutation-tests
```

A primeira baseline usa `thresholds.break = 0` somente de forma temporária. Depois de classificar survivors, o score final `S` fixa `break = floor(S)`, `low = max(60, break)` e `high = max(80, low)`; `break = 0` não pode permanecer na entrega. Mutantes observáveis ligados a requisitos recebem testes focados; regras de negócio não são distorcidas para elevar score. Survivors não equivalentes fora de requisito podem permanecer visíveis e compor a baseline, enquanto `NoCoverage`, timeout ou erro de execução no alvo crítico exigem explicação ou correção.

A baseline limpa corrente, reexecutada em 2026-08-30 com o rate limiting, foi `S = 97,50%`: 513 mutantes foram descobertos, 200 executados, 195 mortos, 5 sobreviventes, 109 ignorados, 3 com erro de compilação gerado pelo mutador e nenhum `NoCoverage`, timeout ou erro de execução, em `00:18:48`. Portanto, a configuração final continua com `break = low = high = 97`. O filtro Swagger novo fica fora da allowlist e altera somente metadados descobertos/ignorados; os 200 mutantes ativos e a classificação observável permaneceram estáveis, sem novo ignore. Os cinco survivors equivalentes anteriores permanecem visíveis no JSON/HTML; a única exclusão pontual documenta a atribuição inicial de um parâmetro `out` cujo valor é ignorado no retorno falso e sempre sobrescrito no retorno verdadeiro. Os arquivos de request permanecem na allowlist, embora o nível `standard` não tenha produzido mutante executável neles nesta versão.

`CI-MUT-001` é um workflow próprio, somente `workflow_dispatch` e cron semanal na segunda-feira às `06:00 UTC`, em `ubuntu-24.04`, com timeout de 90 minutos, projeto Compose exclusivo, cleanup obrigatório e upload dos relatórios HTML/JSON por 14 dias quando produzidos. Ele não executa em `push` ou `pull_request` e, portanto, não bloqueia PRs. Sua execução hospedada permanece pendente até publicação e observação real.

No desenvolvimento local, o proxy do Angular CLI encaminha `/api`, `/swagger` e `/health` para a API, mantendo URLs relativas. Não haverá configuração CORS permissiva para compensar URLs absolutas.

## Configuração e observabilidade

Configurações previstas:

| Chave | Sensível | Regra |
|---|---|---|
| `ConnectionStrings__Default` | Não | No Compose, aponta para `/data/user-profile.db` e fixa `Default Timeout=5`; o health mantém timeout próprio de 1 segundo. |
| `Jwt__Issuer` | Não | Valor padrão de demonstração substituível. |
| `Jwt__Audience` | Não | Valor padrão de demonstração substituível. |
| `Jwt__LifetimeMinutes` | Não | `15`; mudança exige revisão deste design. |
| `Jwt__SigningKey` | Sim | Base64 de ao menos 32 bytes aleatórios; fallback aleatório somente quando ausente em `Development`. |

Logs estruturados podem registrar método, rota sem query string, status, duração e um identificador de correlação gerado para a requisição, mas nunca argumentos da URL, corpo de requests de autenticação, cabeçalho de autorização, token, senha, hash ou chave. O access log do Nginx usa `$uri`, nunca `$request`/`$request_uri`/`$args`, e os diagnostics de acesso padrão do ASP.NET Core que incluem `QueryString` ficam abaixo do nível habilitado. O limiter usa somente IP binário e URI internamente; seu `429` não devolve nem registra email, query ou credencial. Respostas `ProblemDetails` não incluem stack trace, SQL nem caminhos internos.

## Decisões registradas

- [`ADR-0001`](adr/0001-modular-monolith.md) — monólito modular e estrutura mínima.
- [`ADR-0002`](adr/0002-sqlite-persistence.md) — SQLite, volume e migrations no startup.
- [`ADR-0003`](adr/0003-jwt-authentication.md) — JWT curto, `sub`, sessão e chave.
- [`ADR-0004`](adr/0004-nginx-same-origin.md) — Nginx e origem única.
- [`ADR-0005`](adr/0005-nginx-auth-rate-limiting.md) — rate limiting local dos endpoints públicos de autenticação.

## Limites deliberados

Não serão adicionados versionamento de API, refresh/revogação de token, Identity completo, roles, mensageria, cache distribuído, store global, abstração de repositório, múltiplas APIs ou contêiner de banco. O rate limiter não é lockout, não coordena réplicas e perde o estado ao reiniciar o Nginx; mover a demonstração para trás de outro proxy exige nova decisão de real IP. A complexidade mantida — autenticação, índice único, proxy, limiter local, migrations e testes ponta a ponta — existe porque critérios explícitos a exigem.
