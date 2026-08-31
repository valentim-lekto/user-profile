# 01 — Requisitos

## Objetivo

Especificar o comportamento observável da aplicação de cadastro e perfil de usuário e os requisitos de qualidade e entrega do desafio.

Os requisitos funcionais e não funcionais abaixo derivam do enunciado original. As decisões já aprovadas são registradas separadamente como premissas e não devem ser confundidas com requisitos originais.

## Escopo

A solução deve fornecer:

- cadastro de usuário com nome, email, senha e confirmação;
- autenticação por email e senha;
- dashboard protegido com saudação nominal;
- consulta do perfil do usuário autenticado;
- atualização de nome e email;
- alteração de senha em operação separada;
- feedback de sucesso, erro e carregamento no frontend;
- backend, frontend e persistência disponibilizados por Docker Compose;
- documentação SDD, instruções de execução e testes automatizados dos fluxos principais.

## Atores

| ID | Ator | Responsabilidade |
|---|---|---|
| `ACT-VISITOR` | Visitante | Cadastrar-se e autenticar-se. |
| `ACT-USER` | Usuário autenticado | Acessar o dashboard, consultar e editar o próprio perfil e alterar a própria senha. |
| `ACT-EVALUATOR` | Avaliador | Iniciar e validar a solução usando somente Docker e Docker Compose. |

## Casos de uso

| ID | Caso de uso | Ator |
|---|---|---|
| `UC-REG` | Cadastrar usuário | `ACT-VISITOR` |
| `UC-LOGIN` | Autenticar usuário | `ACT-VISITOR` |
| `UC-DASH` | Acessar dashboard protegido | `ACT-USER` |
| `UC-PROF-READ` | Consultar perfil atual | `ACT-USER` |
| `UC-PROF-DATA` | Alterar nome e email | `ACT-USER` |
| `UC-PROF-PASS` | Alterar senha e encerrar sessão | `ACT-USER` |
| `UC-OPS-RUN` | Iniciar e validar a solução | `ACT-EVALUATOR` |

## Requisitos funcionais

| ID | Requisito |
|---|---|
| `FR-REG-01` | O cadastro deve solicitar Nome, Email, Senha e Confirmação de Senha. |
| `FR-REG-02` | Após remover espaços externos, nome deve ser obrigatório e ter ao menos 3 caracteres; email deve ser obrigatório e válido; senha deve ser obrigatória e ter ao menos 6 caracteres; a confirmação deve ser obrigatória e coincidir exatamente com a senha. Os limites defensivos e a política de email testável são decisões internas de `PREM-INPUT-01`, não requisitos originais. |
| `FR-REG-03` | O sistema deve impedir mais de uma conta com o mesmo email conforme a regra de unicidade de `PREM-EMAIL-01`. |
| `FR-REG-04` | O sistema deve informar sucesso ou erro após o cadastro. No sucesso, deve redirecionar ao login sem autenticar automaticamente. |
| `FR-LOGIN-01` | O login deve solicitar email e senha. |
| `FR-LOGIN-02` | Credenciais válidas devem criar uma sessão JWT e redirecionar ao dashboard. |
| `FR-LOGIN-03` | Email inexistente e senha incorreta devem produzir a mesma resposta genérica `401`, sem indicar qual credencial falhou e sem criar sessão autenticada. Payload de login estruturalmente inválido continua sendo validação `400`. |
| `FR-AUTH-01` | Dashboard e operações de perfil devem estar disponíveis somente para usuário autenticado. |
| `FR-AUTH-02` | Endpoints de perfil devem identificar o usuário exclusivamente pelo claim `sub` do JWT, sem receber `userId` do frontend. |
| `FR-DASH-01` | O dashboard deve buscar o perfil atual na API e exibir uma mensagem de boas-vindas com o nome do usuário. |
| `FR-DASH-02` | O dashboard deve oferecer navegação direta para edição dos dados cadastrais, sem repetir as operações disponíveis em cartões meramente descritivos. |
| `FR-PROF-01` | O usuário deve poder consultar o próprio nome e email e acessar a operação de alteração de senha; a senha atual e o identificador técnico da conta não são dados exibidos na interface. |
| `FR-PROF-02` | O usuário deve poder atualizar nome e email, aplicando as validações equivalentes às do cadastro. |
| `FR-PROF-03` | A atualização de nome/email deve informar sucesso ou erro. |
| `FR-PASS-01` | A alteração de senha deve solicitar senha atual, nova senha e confirmação da nova senha. |
| `FR-PASS-02` | A senha atual deve ser verificada; a nova senha deve ter ao menos 6 caracteres e coincidir com sua confirmação. |
| `FR-PASS-03` | A alteração de senha deve informar sucesso ou erro. No sucesso, o frontend deve encerrar a sessão. |
| `FR-UI-01` | As operações assíncronas relevantes do frontend devem possuir estados observáveis de carregamento, sucesso e erro. |
| `FR-ERR-01` | Erros HTTP devem usar `ProblemDetails` e erros de validação devem usar `ValidationProblemDetails`. |

## Requisitos não funcionais

| ID | Requisito |
|---|---|
| `NFR-TECH-01` | O backend deve usar ASP.NET Core/C#, Entity Framework Core e JWT. |
| `NFR-TECH-02` | O frontend deve usar Angular. |
| `NFR-DATA-01` | A persistência deve usar uma das tecnologias permitidas no enunciado; a premissa aprovada seleciona SQLite. |
| `NFR-OPS-01` | Um arquivo `compose.yaml` ou `docker-compose.yml` na raiz deve disponibilizar frontend, backend e persistência. |
| `NFR-OPS-02` | A solução deve ser executável pelo avaliador apenas com Docker e Docker Compose, sem criação manual de `.env`. |
| `NFR-CONFIG-01` | Configurações e segredos devem ser externalizados; nenhuma credencial real pode ser versionada; deve existir `.env.example` quando aplicável. |
| `NFR-SEC-01` | Segredos, senhas e tokens não podem ser registrados em logs nem versionados. |
| `NFR-SEC-02` | As operações públicas de cadastro e login devem ser protegidas na origem única contra rajadas de tentativas, sem permitir que dados fornecidos pelo cliente escolham a partição do limite. |
| `NFR-TEST-01` | Os principais fluxos funcionais devem possuir testes automatizados. |
| `NFR-DOC-01` | O README deve documentar execução e validação, incluindo URLs, portas e dados de teste ou como criá-los. |
| `NFR-SDD-01` | Especificação, critérios de aceite, design técnico, contratos de API, modelo de dados, plano de implementação e estratégia de testes devem ser versionados com o código. |
| `NFR-SDD-02` | Decisões relevantes devem ser registradas em ADRs. |
| `NFR-TRACE-01` | Especificação, implementação e testes devem permanecer coerentes e rastreáveis. |
| `NFR-AI-01` | O trabalho deve seguir processo AI First e Spec-Driven Development, e o candidato deve conseguir explicar todas as decisões e todo código produzido com auxílio de IA. |
| `NFR-DELIVERY-01` | O repositório deve estar público na entrega. |

## Critérios de aceite

IDs publicados não devem ser renumerados nem reutilizados para outro comportamento.

### Cadastro

| ID | Critério |
|---|---|
| `AC-REG-01` | Dados válidos criam uma conta, exibem confirmação, redirecionam ao login e não criam sessão autenticada. |
| `AC-REG-02` | Nome ausente ou, após remoção de espaços externos, com menos de 3 caracteres impede a criação do usuário e exibe o erro correspondente. |
| `AC-REG-03` | Email ausente ou fora da política explícita de validade impede a criação do usuário e exibe o erro correspondente nas duas camadas. |
| `AC-REG-04` | Senha ausente ou com menos de 6 caracteres, confirmação ausente/com menos de 6 caracteres ou confirmação divergente impede a criação do usuário e exibe o erro correspondente. |
| `AC-REG-05` | Um email que, após remoção de espaços externos e comparação sem diferença de caixa, pertença a outra conta é rejeitado e nenhum segundo usuário é criado. |
| `AC-REG-06` | Uma falha de cadastro é apresentada como estado de erro, sem redirecionamento ao dashboard. |

### Login

| ID | Critério |
|---|---|
| `AC-LOGIN-01` | Credenciais válidas autenticam o usuário, armazenam um JWT curto em `sessionStorage` e redirecionam ao dashboard. |
| `AC-LOGIN-02` | Email inexistente e senha incorreta retornam o mesmo `401 ProblemDetails` genérico, exibem a mesma mensagem de erro e não criam uma sessão autenticada. |
| `AC-LOGIN-03` | Enquanto a autenticação estiver em andamento, o frontend apresenta estado de carregamento. |

### Dashboard e autorização

| ID | Critério |
|---|---|
| `AC-DASH-01` | Após autenticação, o dashboard consulta o perfil atual na API e exibe boas-vindas contendo o nome retornado. |
| `AC-DASH-02` | Sem JWT válido, dashboard e perfil não ficam acessíveis e a API rejeita chamadas a recursos protegidos. Se o JWT expirar enquanto uma dessas rotas estiver ativa, o frontend remove a sessão e conduz ao login ao alcançar `exp`, no primeiro ciclo de execução disponível após eventual suspensão da aba ou do sistema; uma chamada protegida iniciada sem token válido também não é enviada anonimamente e conduz ao login. |
| `AC-DASH-03` | O dashboard oferece navegação direta para edição dos dados cadastrais e não apresenta cartões informativos sem ação para dados pessoais, senha ou sessão. |
| `AC-DASH-04` | A consulta do perfil apresenta estado de carregamento e, em caso de falha, estado de erro. |

### Perfil e senha

| ID | Critério |
|---|---|
| `AC-PROF-01` | O usuário autenticado consulta o próprio nome e email na tela, sem enviar `userId`, e acessa uma operação separada para alterar a senha. O `id` continua no `ProfileResponse` como dado técnico do contrato, mas não é renderizado pelo frontend. |
| `AC-PROF-02` | Nome e email são alterados em operação distinta da alteração de senha. |
| `AC-PROF-03` | Nome/email inválidos são rejeitados segundo as mesmas regras do cadastro, sem alterar parcialmente nome, email normalizado ou timestamps. |
| `AC-PROF-04` | Alterar o email para o email normalizado de outra conta é rejeitado sem alterar qualquer dado; manter o próprio email não produz conflito. |
| `AC-PROF-05` | Uma atualização válida persiste os dados e informa sucesso; uma falha informa erro. Uma nova consulta do dashboard reflete o nome atualizado. |
| `AC-PASS-01` | A alteração de senha solicita senha atual, nova senha e confirmação da nova senha. |
| `AC-PASS-02` | Senha atual incorreta não altera o hash nem qualquer outro dado do usuário e produz mensagem de erro. |
| `AC-PASS-03` | Nova senha ausente ou menor que 6 caracteres, ou confirmação divergente, não altera o hash, os timestamps nem qualquer outro dado do usuário e produz erro de validação. |
| `AC-PASS-04` | Após alteração válida, o frontend informa o resultado e encerra a sessão que iniciou a requisição removendo seu JWT de `sessionStorage`; uma resposta tardia não remove nem redireciona uma sessão autenticada posteriormente. |
| `AC-PASS-05` | Duas alterações concorrentes que verificaram o mesmo hash de senha anterior não podem ambas confirmar sucesso: no máximo uma persiste a nova senha; a perdedora retorna `400 ValidationProblemDetails` em `currentPassword`, e somente a senha vencedora autentica. |

### Operação, segurança, documentação e qualidade

| ID | Critério |
|---|---|
| `UI-STATE-01` | Cadastro, login, consulta e edições apresentam estado de carregamento e resultado de sucesso ou erro conforme aplicável. Enquanto uma edição estiver em andamento, o formulário correspondente impede nova submissão e alteração dos campos associados à requisição pendente. |
| `UI-RESP-01` | Em viewports com largura mínima de 320 px, login, cadastro, dashboard e perfil permanecem operáveis sem overflow horizontal. Mensagens de validação, carregamento, sucesso e erro crescem no fluxo do conteúdo, permanecem integralmente legíveis e não se sobrepõem entre si, aos campos ou às ações adjacentes. Quando a confirmação possui erro próprio de obrigatoriedade ou tamanho, somente esse erro prioritário é apresentado; a divergência é exibida quando a confirmação é localmente válida e diferente da senha. Quando a viewport é simultaneamente estreita e baixa, o formulário de autenticação aparece na primeira viewport sem ser antecedido pelo conteúdo editorial não essencial. A ordem visual das ações coincide com a ordem de foco; e um nome válido no limite defensivo não empurra as ações `Ir para o perfil` e `Sair` do dashboard para fora da primeira viewport, embora o valor integral permaneça no DOM e na tela de perfil. |
| `API-ERROR-01` | Respostas HTTP de erro produzidas pela API seguem `ProblemDetails`; falhas de validação seguem `ValidationProblemDetails`. Login com credenciais não reconhecidas usa `401 ProblemDetails` genérico; recursos protegidos usam `401 ProblemDetails` com challenge Bearer. Quando o proxy de mesma origem não alcança a API, ele converte a falha de transporte em `503 ProblemDetails`, sem devolver HTML ao frontend. |
| `API-ERROR-02` | Depois de esgotado o limite de cadastro ou login, o Nginx responde antes da API com `429 application/problem+json`, uma única ocorrência não conflitante de `Retry-After: 60` e de `Cache-Control: no-store`, e `ProblemDetails` genérico contendo os campos não nulos `type`, `title`, `status`, `detail` e `instance`. O `detail` é uma string genérica que permanece não vazia após remover espaços externos e não contém dados sensíveis; o texto apresentado como exemplo no OpenAPI não é normativo. O frontend mantém os valores, encerra o loading, reabilita a ação e exibe “Muitas tentativas. Aguarde um minuto e tente novamente.” sem navegar, criar sessão ou iniciar contagem regressiva. |
| `SEC-AUTH-01` | Endpoints de perfil identificam o usuário exclusivamente pelo claim `sub` do JWT e não aceitam `userId` do cliente. |
| `SEC-SESSION-01` | O JWT de curta duração fica em `sessionStorage` e não há refresh token. |
| `SEC-SECRET-01` | O repositório não contém credenciais reais, segredos, senhas ou tokens; valores sensíveis são fornecidos por configuração externa. |
| `SEC-LOG-01` | Logs não expõem senhas, segredos ou tokens. |
| `SEC-RATE-01` | Somente `POST /api/auth/login` e `POST /api/auth/register` usam buckets independentes por IP de origem e endpoint no Nginx. Cada bucket usa zona de 1 MiB, média `10r/m`, `burst=9` e `nodelay`: aceita até 10 tentativas imediatas e repõe em média uma a cada seis segundos. Tentativas ordinárias válidas ou inválidas consomem cota; após o esgotamento, `429` precede `400`, `401`, `409` e `415` da API. O limite de corpo `413` continua independente, e outros métodos/rotas não entram na cota. |
| `SEC-RATE-02` | A chave da cota combina o endereço TCP observado pelo Nginx (`$binary_remote_addr`) e o endpoint canônico derivado do caminho normalizado sem query (`$uri`). Query strings, variações de caixa/barra final aceitas pelo roteamento e `X-Forwarded-For` enviado pelo cliente não criam nova partição. O estado é local ao processo Nginx, efêmero e reinicia com o contêiner; não existe lockout, coordenação distribuída nem persistência das cotas. |
| `TECH-BACKEND-01` | A solução de backend compila usando ASP.NET Core/C#, EF Core SQLite e autenticação JWT nas versões fixadas pelo design. |
| `TECH-FRONTEND-01` | O frontend compila usando Angular standalone/strict, Reactive Forms e Angular Material nas versões fixadas pelo design. |
| `OPS-DOCKER-01` | Em checkout limpo, `docker compose up` disponibiliza frontend, backend e persistência SQLite em volume, sem exigir criação manual de `.env`. O health exige o conjunto exato de migrations esperado, e o timeout de espera por lock do SQLite permanece menor que o timeout do proxy, deixando margem para a aplicação concluir ou falhar e devolver sua resposta. |
| `OPS-DOCKER-02` | A aplicação pode ser acessada e validada sem instalar SDKs, runtimes ou banco de dados fora do Docker e Docker Compose. |
| `OPS-DOCKER-03` | Os dados SQLite sobrevivem à recriação dos contêineres da aplicação enquanto o volume Docker for preservado. |
| `OPS-DOCKER-04` | Sob a premissa explícita de uma única instância, o startup recupera um lock técnico órfão de migrations do EF Core antes de aplicar as migrations, sem remover dados da aplicação. A preparação e aplicação do schema ocorrem antes de a API aceitar requisições, possuem prazo limitado e observam o encerramento cooperativo do host, inclusive `SIGTERM`; falha real de migration continua interrompendo o startup. |
| `DOC-RUN-01` | O README permite iniciar e validar a solução e informa comandos, URLs, portas e dados de teste ou o procedimento de cadastro para criá-los. |
| `DOC-SDD-01` | Os artefatos SDD exigidos estão versionados e ADRs existem para decisões relevantes. |
| `DOC-TRACE-01` | Cada mudança comportamental possui vínculo verificável entre requisito, critério de aceite, implementação e teste. |
| `TEST-FLOW-01` | Testes automatizados cobrem ao menos cadastro válido e inválido, login válido e inválido, proteção de acesso, dashboard, atualização de nome/email e alteração de senha. |
| `AI-SDD-01` | O uso de IA em requisitos, design, implementação, testes e revisão é registrado sem armazenar conversas completas. |
| `AI-EXPLAIN-01` | O candidato consegue explicar as decisões e o código produzido com auxílio de IA. |
| `DEL-REPO-01` | Na entrega, o repositório está publicamente acessível. |

## Premissas aprovadas

Estas premissas refinam a solução, mas não são requisitos originais do desafio.
`PREM-DATA-02` preserva o ID publicado, porém é uma decisão interna de design
introduzida em `b184432` e agora formalizada no ADR-0002; não veio do enunciado.
`PREM-INPUT-01` registra separadamente os refinamentos defensivos introduzidos em
M2, para que não sejam apresentados como requisitos originais do desafio.

| ID | Premissa |
|---|---|
| `PREM-ARCH-01` | Será usado monólito modular proporcional ao escopo. |
| `PREM-ARCH-02` | Haverá um único projeto executável de backend e um projeto de integração. |
| `PREM-ARCH-03` | A organização será por funcionalidades, sem CQRS, MediatR, AutoMapper, generic repository ou múltiplas camadas artificiais. |
| `PREM-DATA-01` | Será usado SQLite, com migrations do EF Core e volume Docker. |
| `PREM-DATA-02` | Como decisão interna de design, `User` persistirá `Id`, `Name`, `Email`, `NormalizedEmail`, `PasswordHash`, `CreatedAtUtc` e `UpdatedAtUtc`; o índice de `NormalizedEmail` será único. |
| `PREM-FE-01` | O Angular será standalone, em strict mode, com Reactive Forms e Angular Material. |
| `PREM-LANG-01` | Identificadores e código serão escritos em inglês; README e documentos SDD, em português. |
| `PREM-EMAIL-01` | Para emails aceitos por `PREM-INPUT-01`, a unicidade ignora espaços externos e diferenças de caixa. |
| `PREM-INPUT-01` | Como decisão interna defensiva, nome é limitado a 200 caracteres após `Trim`; email é limitado a 320 caracteres após `Trim` e aceita somente a política ASCII explícita do design; entradas de senha são limitadas a 128 caracteres. Senhas não são aparadas: todos os caracteres, inclusive espaços, são significativos. Nomes JSON são camelCase e sensíveis a caixa. O proxy limita corpos HTTP a 1 MiB. |
| `PREM-REG-01` | Cadastro bem-sucedido redireciona ao login com mensagem de sucesso e não autentica automaticamente. |
| `PREM-PASS-01` | Alterar senha exige senha atual, nova senha e confirmação; no sucesso, o frontend encerra a sessão. |
| `PREM-PROF-01` | Atualização de nome/email e alteração de senha serão operações separadas. |
| `PREM-AUTH-01` | O usuário autenticado será identificado pelo ID no claim `sub`; endpoints de perfil não receberão `userId`. `ProfileResponse` devolve esse ID imutável junto de nome e email como parte do contrato técnico, sem exigir sua exibição no frontend. |
| `PREM-AUTH-02` | O dashboard buscará o perfil atual na API. |
| `PREM-AUTH-03` | Será usado JWT de curta duração em `sessionStorage`, sem refresh token. |
| `PREM-ERR-01` | Erros HTTP usarão `ProblemDetails`/`ValidationProblemDetails`. |
| `PREM-SEED-01` | Não haverá seed obrigatório; o cadastro será o meio de criar dados para validação. |
| `PREM-OPS-01` | `docker compose up` funcionará sem criação manual de `.env`. |

## Fora de escopo

- recuperação de senha;
- confirmação de email;
- roles;
- administração;
- refresh tokens;
- deploy em produção.

## Pontos detalhados pelo design aprovado

Estes pontos ficaram deliberadamente sem decisão na etapa inicial de requisitos,
para evitar escopo inventado. O design aprovado em
[`02-technical-design.md`](02-technical-design.md) e os ADRs já os resolveu; a
lista é preservada como handoff histórico:

- duração exata e regras de expiração do JWT;
- rotas, métodos, status HTTP, códigos e textos dos erros;
- efeitos da normalização do email no login, armazenamento e exibição, além da regra de unicidade já aprovada;
- tratamento de espaços e contagem de caracteres na validação do nome;
- mecanismo de proteção e armazenamento da senha;
- significado preciso de “projeto de integração”;
- portas e URLs da solução.

Com SQLite, a persistência exigida pelo Compose foi materializada por arquivo em
volume Docker; não é necessário um contêiner de banco separado. O refinamento
está detalhado no design técnico e no ADR-0002.

## Definition of Done geral

Uma mudança ou incremento só está concluído quando:

- os requisitos e critérios de aceite afetados estão identificados;
- mudanças de comportamento foram precedidas pela atualização da especificação;
- implementação e testes automatizados cobrem os critérios relacionados;
- testes não foram removidos nem enfraquecidos para obter sucesso artificial;
- builds e testes relevantes passam;
- os fluxos afetados apresentam estados de carregamento, sucesso e erro;
- nenhum endpoint de perfil confia em ID de usuário enviado pelo cliente;
- nenhum segredo, senha ou token foi versionado ou exposto em logs;
- o diff foi revisado;
- documentação, contratos e rastreabilidade estão atualizados;
- decisões relevantes foram registradas em ADR;
- a validação observável do incremento foi executada.

Na entrega final, adicionalmente:

- `docker compose up` funciona em checkout limpo usando somente Docker e Docker Compose;
- URLs, portas e procedimento de teste estão documentados;
- os principais fluxos automatizados passam;
- o repositório está público;
- o candidato consegue explicar as decisões e o código produzido com auxílio de IA.
