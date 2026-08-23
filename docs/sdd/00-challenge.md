# Desafio técnico full-stack

> Transcrição estruturada do enunciado recebido. As decisões já aprovadas estão registradas como premissas posteriores e não como requisitos originais.

## Contexto e restrições desta etapa

Este é um novo repositório Git destinado a um desafio técnico full-stack. O trabalho desta etapa deve ser autônomo e não deve implementar código da aplicação.

Antes de editar:

1. Confirmar a raiz do repositório com `git status`.
2. Inspecionar os arquivos existentes.
3. Preservar qualquer conteúdo existente.
4. Não alterar a configuração global do Git.
5. Não fazer push nem configurar remote.

Se este arquivo já existisse, deveria ser preservado como fonte original. Como estava ausente, ele foi criado como esta transcrição estruturada.

## Enunciado original do desafio

Construir uma aplicação completa com backend ASP.NET Core e frontend Angular que permita:

- Cadastro com Nome, Email, Senha e Confirmação de Senha.
- Nome obrigatório com no mínimo 3 caracteres.
- Email obrigatório e válido.
- Senha obrigatória com no mínimo 6 caracteres.
- Confirmação de senha correspondente.
- Mensagem de sucesso ou erro após o cadastro.
- Login com email e senha.
- Mensagem de erro para credenciais inválidas.
- Redirecionamento ao dashboard após login.
- Dashboard protegido com boas-vindas contendo o nome do usuário.
- Navegação para edição dos dados cadastrais.
- Consulta e edição de Nome, Email e Senha.
- Validações de edição equivalentes às do cadastro.
- Mensagem de sucesso ou erro após edição.

### Tecnologias obrigatórias

- ASP.NET Core/C#.
- Entity Framework Core.
- JWT.
- Angular.
- SQLite, SQL Server ou MySQL.
- Framework visual Angular opcional.

### Requisitos adicionais

- Processo AI First e Spec-Driven Development.
- Artefatos SDD versionados junto ao código.
- Especificação, critérios de aceite, design técnico, contratos de API, modelo de dados, plano de implementação e estratégia de testes.
- ADRs quando houver decisões relevantes.
- README com instruções para executar e validar.
- Testes automatizados para os principais fluxos.
- Configurações e segredos externalizados.
- Nenhuma credencial real versionada.
- Repositório público na entrega.
- `compose.yaml` ou `docker-compose.yml` na raiz.
- `docker compose up` deve iniciar frontend, backend e persistência.
- O avaliador deve precisar somente de Docker e Docker Compose.
- `.env.example` quando aplicável.
- URLs, portas e dados de teste documentados.
- Estados de sucesso, erro e carregamento no frontend.
- Coerência entre especificação, implementação e testes.
- O candidato deve conseguir explicar todas as decisões e código gerado com auxílio de IA.

## Decisões já aprovadas

> As decisões abaixo são premissas, não requisitos originais.

- Arquitetura de monólito modular proporcional ao escopo.
- Um único projeto executável de backend e um projeto de integração.
- Organização por funcionalidades, sem CQRS, MediatR, AutoMapper, generic repository ou múltiplas camadas artificiais.
- SQLite com EF Core migrations e volume Docker.
- Angular standalone, strict mode, Reactive Forms e Angular Material.
- Identificadores e código em inglês.
- README e documentos SDD em português.
- Email único ignorando espaços externos e diferenças de caixa.
- Cadastro redireciona ao login com mensagem de sucesso.
- Cadastro não autentica automaticamente.
- Alteração de senha exige senha atual, nova senha e confirmação.
- Nome/email e senha serão atualizados por operações separadas.
- Usuário autenticado será identificado pelo claim `sub` com seu ID.
- Nenhum endpoint de perfil receberá `userId` do frontend.
- Dashboard buscará o perfil atual na API.
- JWT curto armazenado em `sessionStorage`.
- Sem refresh token.
- Após trocar a senha, o frontend encerrará a sessão.
- Erros HTTP usarão `ProblemDetails`/`ValidationProblemDetails`.
- Não haverá seed obrigatório; o cadastro permite validar o sistema.
- Recuperação de senha, confirmação de email, roles, administração, refresh tokens e deploy em produção estão fora de escopo.
- `docker compose up` deve funcionar sem criar `.env` manualmente.

## Artefatos desta etapa

Criar:

- `.gitignore`, caso ainda não exista.
- `AGENTS.md`.
- `PLANS.md`.
- `docs/sdd/README.md`.
- `docs/sdd/00-challenge.md`, somente se estiver ausente.
- `docs/sdd/01-requirements.md`.
- `docs/sdd/ai-usage.md`.

### Regras para `AGENTS.md`

O arquivo deve ser curto e determinar que:

- os documentos SDD devem ser lidos antes de alterar comportamento;
- uma única etapa do plano deve ser implementada por vez;
- mudanças devem estar associadas a critérios de aceite;
- mudanças de comportamento começam pela atualização da especificação;
- toda mudança comportamental deve incluir testes;
- testes não podem ser removidos ou enfraquecidos para fazer o build passar;
- segredos, senhas e tokens nunca podem ser logados ou versionados;
- o usuário autenticado vem do JWT, nunca de IDs enviados pelo cliente;
- uma tarefa só está pronta após build, testes, revisão do diff, documentação e rastreabilidade.

### Formato de `PLANS.md`

O arquivo deve definir apenas o formato dos planos executáveis, incluindo:

- objetivo;
- critérios de aceite relacionados;
- contexto;
- milestones;
- progresso;
- comandos;
- validação observável;
- riscos;
- descobertas;
- decision log;
- resultado final.

### Conteúdo de `docs/sdd/01-requirements.md`

O documento deve conter:

- escopo;
- atores;
- casos de uso;
- requisitos funcionais e não funcionais;
- critérios de aceite com IDs estáveis;
- premissas;
- fora de escopo;
- Definition of Done geral.

Devem ser usados IDs como `AC-REG-01`, `AC-LOGIN-01`, `AC-DASH-01`, `AC-PROF-01`, `OPS-DOCKER-01`, `SEC-SECRET-01` e `DOC-RUN-01`.

### Conteúdo de `docs/sdd/ai-usage.md`

O documento deve registrar como IA será usada em requisitos, design, implementação, testes e revisão, sem armazenar conversas completas.

## Validação e commit

- Comparar os documentos com o enunciado.
- Procurar requisitos esquecidos, contradições e escopo inventado.
- Executar `git diff --check`.
- Não criar backend, frontend, Dockerfiles ou dependências.
- Atualizar `docs/sdd/README.md` com o estado dos artefatos.
- Criar um commit local com a mensagem `docs: establish SDD foundation`.
- Não fazer push.

## Relatório de conclusão

Ao terminar, informar:

- arquivos criados;
- critérios identificados;
- premissas registradas;
- hash do commit;
- confirmação de que nenhum código foi implementado.
