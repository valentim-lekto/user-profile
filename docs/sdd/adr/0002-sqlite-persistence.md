# ADR-0002 — Persistência SQLite

**Status:** aceita · **Data:** 2026-08-24

## Contexto

A entrega precisa persistir dados, iniciar apenas com Docker Compose e não exigir um banco instalado no host. O ambiente de demonstração executará uma única instância da API.

## Decisão

- Usar EF Core 10 com o provider SQLite.
- Persistir `/data/user-profile.db` em volume Docker nomeado.
- Criar a entidade `User` e um índice único de banco em `NormalizedEmail`.
- Versionar migrations e aplicá-las no startup antes de a API ficar pronta.
- Usar um arquivo SQLite isolado por fixture nos testes de integração; EF InMemory não substitui esses testes.

## Consequências

### Positivas

- Não há contêiner de banco nem credenciais de banco.
- O avaliador consegue criar dados pelo cadastro e preservá-los ao recriar os contêineres.
- Os testes exercitam o mesmo provider e as mesmas restrições da aplicação.

### Negativas

- Escritas concorrentes e operação multi-instância são limitadas.
- Migrations no startup podem disputar entre instâncias; por isso a decisão vale apenas para a demonstração de instância única.
- Excluir o volume remove os dados e deve ser uma ação explícita.

## Alternativas rejeitadas

- SQL Server ou MySQL: exigiriam outro serviço, credenciais e mais tempo de inicialização sem benefício para o desafio.
- Banco em memória: não valida migrations, índice único nem persistência real.
- Migration manual: violaria a execução por `docker compose up` sem preparação adicional.

## Rastreabilidade

`PREM-DATA-01`, `NFR-DATA-01`, `OPS-DOCKER-01`, `OPS-DOCKER-03`.
