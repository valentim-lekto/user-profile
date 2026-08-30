# ADR-0002 — Persistência SQLite

**Status:** aceita · **Data:** 2026-08-24 · **Última revisão:** 2026-08-30

## Contexto

A entrega precisa persistir dados, iniciar apenas com Docker Compose e não exigir um banco instalado no host. O ambiente de demonstração executará uma única instância da API.

## Decisão

- Usar EF Core 10 com o provider SQLite.
- Persistir `/data/user-profile.db` em volume Docker nomeado.
- Criar a entidade `User` e um índice único de banco em `NormalizedEmail`.
- Manter `CreatedAtUtc` e `UpdatedAtUtc` como metadados internos: ambos são
  inicializados no cadastro, `CreatedAtUtc` é preservado e `UpdatedAtUtc` muda em
  alterações persistidas. Esta é uma decisão interna de design, não requisito do
  enunciado nem campo do contrato HTTP.
- Versionar migrations e aplicá-las em `IHostedLifecycleService.StartingAsync`,
  depois que o host registra sinais de encerramento e antes de a API aceitar
  requisições.
- Sob a premissa de uma única instância, remover no startup somente a tabela
  técnica `__EFMigrationsLock` antes de aplicar migrations, recuperando o
  artefato órfão deixado por interrupção. A preparação e a migration têm limites de tempo;
  dados e `__EFMigrationsHistory` não são apagados.
- Usar um arquivo SQLite isolado por fixture nos testes de integração; EF InMemory não substitui esses testes.

## Consequências

### Positivas

- Não há contêiner de banco nem credenciais de banco.
- O avaliador consegue criar dados pelo cadastro e preservá-los ao recriar os contêineres.
- Os testes exercitam o mesmo provider e as mesmas restrições da aplicação.

### Negativas

- Escritas concorrentes e operação multi-instância são limitadas.
- Migrations no startup podem disputar entre instâncias; por isso a decisão vale apenas para a demonstração de instância única.
- A recuperação automática do lock não é segura para múltiplas instâncias, pois
  uma delas poderia remover o lock legítimo da outra; produção exige etapa de
  migration separada.
- O token do lifecycle permite que `SIGTERM` cancele cooperativamente o startup;
  esse cancelamento solicitado pelo host propaga até a fronteira de execução e é
  reconhecido ali para concluir o encerramento normalmente, enquanto falhas reais
  e o deadline continuam sendo propagados.
  Operações nativas bloqueadas permanecem limitadas pelo timeout do SQLite.
- Excluir o volume remove os dados e deve ser uma ação explícita.

## Alternativas rejeitadas

- SQL Server ou MySQL: exigiriam outro serviço, credenciais e mais tempo de inicialização sem benefício para o desafio.
- Banco em memória: não valida migrations, índice único nem persistência real.
- Migration manual: violaria a execução por `docker compose up` sem preparação adicional.

## Rastreabilidade

`PREM-DATA-01`, `PREM-DATA-02`, `NFR-DATA-01`, `NFR-OPS-01`, `OPS-DOCKER-01`, `OPS-DOCKER-03`, `OPS-DOCKER-04`, `BE-DB-002` e `BE-DB-003`.
