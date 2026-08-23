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
| `FR-REG-02` | Nome deve ser obrigatório e ter ao menos 3 caracteres; email deve ser obrigatório e válido; senha deve ser obrigatória e ter ao menos 6 caracteres; a confirmação deve coincidir com a senha. |
| `FR-REG-03` | O sistema deve impedir mais de uma conta com o mesmo email conforme a regra de unicidade de `PREM-EMAIL-01`. |
| `FR-REG-04` | O sistema deve informar sucesso ou erro após o cadastro. No sucesso, deve redirecionar ao login sem autenticar automaticamente. |
| `FR-LOGIN-01` | O login deve solicitar email e senha. |
| `FR-LOGIN-02` | Credenciais válidas devem criar uma sessão JWT e redirecionar ao dashboard. |
| `FR-LOGIN-03` | Credenciais inválidas devem produzir mensagem de erro e não criar sessão autenticada. |
| `FR-AUTH-01` | Dashboard e operações de perfil devem estar disponíveis somente para usuário autenticado. |
| `FR-AUTH-02` | Endpoints de perfil devem identificar o usuário exclusivamente pelo claim `sub` do JWT, sem receber `userId` do frontend. |
| `FR-DASH-01` | O dashboard deve buscar o perfil atual na API e exibir uma mensagem de boas-vindas com o nome do usuário. |
| `FR-DASH-02` | O dashboard deve oferecer navegação para edição dos dados cadastrais. |
| `FR-PROF-01` | O usuário deve poder consultar o próprio nome e email e acessar a operação de alteração de senha; a senha atual não é um dado consultável. |
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
| `AC-REG-02` | Nome ausente ou com menos de 3 caracteres impede a criação do usuário e exibe o erro correspondente. |
| `AC-REG-03` | Email ausente ou inválido impede a criação do usuário e exibe o erro correspondente. |
| `AC-REG-04` | Senha ausente ou com menos de 6 caracteres, ou confirmação divergente, impede a criação do usuário e exibe o erro correspondente. |
| `AC-REG-05` | Um email que, após remoção de espaços externos e comparação sem diferença de caixa, pertença a outra conta é rejeitado e nenhum segundo usuário é criado. |
| `AC-REG-06` | Uma falha de cadastro é apresentada como estado de erro, sem redirecionamento ao dashboard. |

### Login

| ID | Critério |
|---|---|
| `AC-LOGIN-01` | Credenciais válidas autenticam o usuário, armazenam um JWT curto em `sessionStorage` e redirecionam ao dashboard. |
| `AC-LOGIN-02` | Credenciais inválidas exibem mensagem de erro e não criam uma sessão autenticada. |
| `AC-LOGIN-03` | Enquanto a autenticação estiver em andamento, o frontend apresenta estado de carregamento. |

### Dashboard e autorização

| ID | Critério |
|---|---|
| `AC-DASH-01` | Após autenticação, o dashboard consulta o perfil atual na API e exibe boas-vindas contendo o nome retornado. |
| `AC-DASH-02` | Sem JWT válido, o dashboard não fica acessível e a API rejeita chamadas a recursos protegidos. |
| `AC-DASH-03` | O dashboard oferece navegação para edição dos dados cadastrais. |
| `AC-DASH-04` | A consulta do perfil apresenta estado de carregamento e, em caso de falha, estado de erro. |

### Perfil e senha

| ID | Critério |
|---|---|
| `AC-PROF-01` | O usuário autenticado consulta o próprio nome e email, sem enviar `userId`, e acessa uma operação separada para alterar a senha. |
| `AC-PROF-02` | Nome e email são alterados em operação distinta da alteração de senha. |
| `AC-PROF-03` | Nome/email inválidos são rejeitados segundo as mesmas regras do cadastro. |
| `AC-PROF-04` | Alterar o email para o email normalizado de outra conta é rejeitado; manter o próprio email não produz conflito. |
| `AC-PROF-05` | Uma atualização válida persiste os dados e informa sucesso; uma falha informa erro. |
| `AC-PASS-01` | A alteração de senha solicita senha atual, nova senha e confirmação da nova senha. |
| `AC-PASS-02` | Senha atual incorreta não altera a senha e produz mensagem de erro. |
| `AC-PASS-03` | Nova senha ausente ou menor que 6 caracteres, ou confirmação divergente, não altera a senha e produz erro de validação. |
| `AC-PASS-04` | Após alteração válida, o frontend informa o resultado e encerra a sessão removendo o JWT de `sessionStorage`. |

### Operação, segurança, documentação e qualidade

| ID | Critério |
|---|---|
| `UI-STATE-01` | Cadastro, login, consulta e edições apresentam estado de carregamento e resultado de sucesso ou erro conforme aplicável. |
| `API-ERROR-01` | Respostas HTTP de erro seguem `ProblemDetails`; falhas de validação seguem `ValidationProblemDetails`. |
| `SEC-AUTH-01` | Endpoints de perfil identificam o usuário exclusivamente pelo claim `sub` do JWT e não aceitam `userId` do cliente. |
| `SEC-SESSION-01` | O JWT de curta duração fica em `sessionStorage` e não há refresh token. |
| `SEC-SECRET-01` | O repositório não contém credenciais reais, segredos, senhas ou tokens; valores sensíveis são fornecidos por configuração externa. |
| `SEC-LOG-01` | Logs não expõem senhas, segredos ou tokens. |
| `OPS-DOCKER-01` | Em checkout limpo, `docker compose up` disponibiliza frontend, backend e persistência SQLite em volume, sem exigir criação manual de `.env`. |
| `OPS-DOCKER-02` | A aplicação pode ser acessada e validada sem instalar SDKs, runtimes ou banco de dados fora do Docker e Docker Compose. |
| `OPS-DOCKER-03` | Os dados SQLite sobrevivem à recriação dos contêineres da aplicação enquanto o volume Docker for preservado. |
| `DOC-RUN-01` | O README permite iniciar e validar a solução e informa comandos, URLs, portas e dados de teste ou o procedimento de cadastro para criá-los. |
| `DOC-SDD-01` | Os artefatos SDD exigidos estão versionados e ADRs existem para decisões relevantes. |
| `DOC-TRACE-01` | Cada mudança comportamental possui vínculo verificável entre requisito, critério de aceite, implementação e teste. |
| `TEST-FLOW-01` | Testes automatizados cobrem ao menos cadastro válido e inválido, login válido e inválido, proteção de acesso, dashboard, atualização de nome/email e alteração de senha. |
| `AI-SDD-01` | O uso de IA em requisitos, design, implementação, testes e revisão é registrado sem armazenar conversas completas. |
| `AI-EXPLAIN-01` | O candidato consegue explicar as decisões e o código produzido com auxílio de IA. |
| `DEL-REPO-01` | Na entrega, o repositório está publicamente acessível. |

## Premissas aprovadas

Estas premissas refinam a solução, mas não são requisitos originais do desafio.

| ID | Premissa |
|---|---|
| `PREM-ARCH-01` | Será usado monólito modular proporcional ao escopo. |
| `PREM-ARCH-02` | Haverá um único projeto executável de backend e um projeto de integração. |
| `PREM-ARCH-03` | A organização será por funcionalidades, sem CQRS, MediatR, AutoMapper, generic repository ou múltiplas camadas artificiais. |
| `PREM-DATA-01` | Será usado SQLite, com migrations do EF Core e volume Docker. |
| `PREM-FE-01` | O Angular será standalone, em strict mode, com Reactive Forms e Angular Material. |
| `PREM-LANG-01` | Identificadores e código serão escritos em inglês; README e documentos SDD, em português. |
| `PREM-EMAIL-01` | A unicidade de email ignora espaços externos e diferenças de caixa. |
| `PREM-REG-01` | Cadastro bem-sucedido redireciona ao login com mensagem de sucesso e não autentica automaticamente. |
| `PREM-PASS-01` | Alterar senha exige senha atual, nova senha e confirmação; no sucesso, o frontend encerra a sessão. |
| `PREM-PROF-01` | Atualização de nome/email e alteração de senha serão operações separadas. |
| `PREM-AUTH-01` | O usuário autenticado será identificado pelo ID no claim `sub`; endpoints de perfil não receberão `userId`. |
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

## Pontos a detalhar no design

Estes pontos permanecem deliberadamente sem decisão nesta etapa para evitar escopo inventado:

- duração exata e regras de expiração do JWT;
- rotas, métodos, status HTTP, códigos e textos dos erros;
- efeitos da normalização do email no login, armazenamento e exibição, além da regra de unicidade já aprovada;
- tratamento de espaços e contagem de caracteres na validação do nome;
- mecanismo de proteção e armazenamento da senha;
- significado preciso de “projeto de integração”;
- portas e URLs da solução.

Com SQLite, a persistência exigida pelo Compose será materializada por arquivo em volume Docker; não é necessário um contêiner de banco separado. Esse refinamento deverá ser detalhado no design técnico.

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
