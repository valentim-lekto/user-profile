# ADR-0003 — Autenticação JWT

**Status:** aceita · **Data:** 2026-08-24

## Contexto

A API precisa autenticar por email e senha, proteger dashboard/perfil e identificar sempre o usuário pelo token, sem refresh token ou infraestrutura de sessão no servidor.

## Decisão

- Armazenar somente `PasswordHash`, gerado e verificado por `PasswordHasher<User>` no formato Identity V3.
- Emitir JWT Bearer HMAC SHA-256 com duração de 15 minutos e claims mínimas `sub`, `jti`, `iat` e `exp`.
- Validar issuer, audience, assinatura e expiração, com tolerância de relógio de 30 segundos.
- Desabilitar `MapInboundClaims`, exigir as quatro claims mínimas e ler diretamente o claim chamado `sub`; claim ausente ou malformada resulta em `401`.
- Usar exclusivamente o `sub` validado como ID do usuário; contratos de perfil não recebem `userId`.
- Armazenar o JWT em `sessionStorage`; não emitir refresh token.
- Após trocar a senha, remover o token no frontend. Não haverá revogação server-side dos tokens já emitidos.
- Exigir chave externa fora de `Development`. Na demonstração, gerar uma chave aleatória por processo quando ela não estiver configurada, sem persistir ou logar o valor.

## Consequências

### Positivas

- A API permanece stateless e o contrato é pequeno.
- Tokens expiram rapidamente e não há armazenamento persistente no browser.
- O Compose funciona sem versionar ou preparar segredo manualmente.

### Negativas

- Um token capturado continua válido até expirar, inclusive após troca de senha.
- Reiniciar a API com chave de desenvolvimento gerada invalida sessões abertas.
- `sessionStorage` continua acessível a JavaScript; prevenção de XSS e dependências revisadas são essenciais.

## Alternativas rejeitadas

- Refresh tokens: explicitamente fora de escopo e exigiriam rotação/revogação persistente.
- Cookies de autenticação: divergiriam da decisão aprovada de JWT em `sessionStorage`.
- ASP.NET Core Identity completo: roles, stores e fluxos adicionais excedem o necessário; somente o hasher é reutilizado.
- Chave fixa no repositório: violaria a política de segredos.

## Rastreabilidade

`FR-LOGIN-02`, `FR-AUTH-02`, `FR-PASS-03`, `PREM-AUTH-01`, `PREM-AUTH-03`, `SEC-AUTH-01`, `SEC-SESSION-01`, `SEC-SECRET-01`.
