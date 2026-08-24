# ADR-0001 — Monólito modular

**Status:** aceita · **Data:** 2026-08-24

## Contexto

O desafio possui um domínio pequeno, cinco operações de negócio e uma única equipe/entrega. A solução precisa ser explicável, testável e executada por Docker Compose, sem justificar comunicação distribuída ou múltiplas camadas.

## Decisão

- Usar um monólito modular com um único executável de backend: `UserProfile.Api`.
- Usar Controllers do ASP.NET Core e organizar o código por `Auth` e `Profile`, com persistência e segurança compartilhadas mínimas.
- Manter um único projeto `UserProfile.Api.IntegrationTests` para os testes de integração do backend.
- Controllers dependem diretamente de `DbContext`, `IPasswordHasher<User>` e serviços concretos pequenos; não haverá generic repository, CQRS, MediatR, AutoMapper nem interfaces com uma única implementação sem necessidade real.
- O Angular é um cliente separado, mas não cria outro serviço de backend.

## Consequências

### Positivas

- Menos projetos, contratos internos e indireções.
- Transações e consistência permanecem locais.
- Estrutura suficiente para localizar funcionalidades e testar o pipeline completo.
- Um único processo de API simplifica migrations, autenticação e Compose.

### Negativas

- Os módulos não têm isolamento de processo.
- Crescimento futuro pode exigir refatoração das fronteiras.
- Controllers precisam ser mantidos pequenos por revisão, sem depender de uma camada artificial para impor isso.

## Alternativas rejeitadas

- Microserviços: custo operacional sem fronteira de domínio ou escala que o justifique.
- Arquitetura em múltiplos projetos por camada: aumenta navegação e contratos sem ganho para este escopo.
- CQRS/MediatR e repositórios genéricos: duplicariam recursos já fornecidos por Controllers e EF Core.

## Rastreabilidade

`PREM-ARCH-01`, `PREM-ARCH-02`, `PREM-ARCH-03`, `NFR-TECH-01`, `DOC-SDD-01`.
