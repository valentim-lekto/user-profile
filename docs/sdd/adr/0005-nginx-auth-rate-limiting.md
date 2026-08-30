# ADR-0005 — Rate limiting de autenticação no Nginx

**Status:** aceita · **Data:** 2026-08-30

## Contexto

Cadastro e login são operações públicas e executam validação, consulta e, no login, verificação de hash. A demonstração precisa limitar rajadas triviais na única origem publicada sem introduzir estado de conta, mudança de banco, dependência distribuída ou novo componente backend.

O Nginx já é a fronteira TCP exposta e distingue transporte de comportamento da API. O controle precisa proteger apenas as duas operações públicas de autenticação, manter seus buckets independentes e não aceitar cabeçalhos ou query strings do cliente como identidade da cota.

## Decisão

- Aplicar `limit_req` somente a `POST /api/auth/login` e `POST /api/auth/register`.
- Usar uma zona compartilhada de 1 MiB, `rate=10r/m`, `burst=9` e `nodelay`. A primeira requisição e as nove posições de burst permitem até dez tentativas imediatas; a taxa média repõe uma tentativa a cada seis segundos.
- Particionar a zona pela composição de `$binary_remote_addr` com o endpoint canônico derivado de `$uri`. O `map` usa o caminho normalizado sem query e reúne no mesmo bucket variações de caixa ou barra final aceitas pelo roteamento; login e cadastro continuam independentes.
- Usar chave vazia para métodos diferentes de `POST`, deixando-os fora do controle, e manter `/health`, `/swagger` e perfil no proxy genérico sem `limit_req`.
- Sobrescrever `X-Forwarded-For` enviado ao backend com `$remote_addr`; um valor fornecido pelo cliente não influencia a cota nem é propagado como cadeia confiável.
- Contar toda tentativa ordinária admitida, independentemente do resultado posterior da API. Após a cota acabar, a rejeição `429` do Nginx precede `400`, `401`, `409` ou `415`; o `client_max_body_size` e seu `413` continuam uma barreira de transporte independente.
- Responder ao limite com `429 application/problem+json`, `Retry-After: 60`, `Cache-Control: no-store` e corpo genérico com `type`, `title`, `status`, `detail` e `instance`, sem IP, email ou credencial.
- Manter o estado local e efêmero no Nginx. Reiniciar ou recriar o serviço `web` limpa as cotas.

## Consequências

### Positivas

- Rajadas simples deixam de alcançar hash, validação e persistência da API depois do limite.
- Login e cadastro não competem pela mesma cota.
- Query string, variações equivalentes de caminho e `X-Forwarded-For` forjado não permitem renovar a partição.
- A implementação não altera endpoints, DTOs, banco, migrations, autenticação JWT ou topologia Compose.

### Negativas e limites

- A cota não é lockout de conta e não impede ataques distribuídos por muitos IPs.
- Réplicas de Nginx teriam estados independentes; não há Redis ou coordenação externa.
- Reiniciar o Nginx restaura imediatamente todas as cotas.
- O IP observado é o peer TCP. Se a aplicação for colocada atrás de outro proxy confiável, a topologia e a política de real IP precisam ser redesenhadas antes de reutilizar esta chave.
- `Retry-After: 60` é uma orientação conservadora e estática; o bucket continua sendo reposto gradualmente.

## Alternativas rejeitadas

- Rate limiter no ASP.NET Core: duplicaria no backend uma responsabilidade já pertencente ao único ingresso publicado e exigiria outro caminho de teste/configuração.
- Lockout por email/conta: criaria regra de negócio, estado persistido e risco de negação dirigida a terceiros.
- Redis ou limiter distribuído: desproporcional à instância única de demonstração.
- Bucket único para todas as rotas de autenticação: cadastro poderia bloquear login, contrariando a independência aprovada.
- Usar query string ou `X-Forwarded-For` do cliente na chave: permitiria escolher ou multiplicar partições.

## Rastreabilidade

`NFR-SEC-02`, `SEC-RATE-01`, `SEC-RATE-02`, `API-ERROR-02`, `SPEC-OAS-006`, `FE-RATE-001`, `OPS-RATE-001`, `OPS-RATE-002`.
