# ADR-0004 — Nginx e origem única

**Status:** aceita · **Data:** 2026-08-24

## Contexto

O avaliador deve iniciar frontend, backend e persistência com Compose e acessar a aplicação sem conhecer múltiplas portas. O Angular precisa chamar a API sem configuração de URL por ambiente no bundle.

## Decisão

- Compilar o Angular em uma imagem multi-stage e servir os arquivos estáticos com Nginx.
- Publicar somente `http://localhost:8080` pelo serviço `web`.
- Encaminhar `/api/*` e `/health` ao serviço interno `api:8080`.
- Usar URLs relativas no frontend e fallback para `index.html` apenas nas rotas da SPA.
- Não habilitar CORS amplo na entrega Docker; o proxy de desenvolvimento do Angular reproduz a mesma origem localmente.
- Fixar a imagem em `nginx:1.30.4-alpine3.24-slim`.

## Consequências

### Positivas

- Uma URL para o avaliador, sem CORS e sem expor a API diretamente no host.
- O mesmo artefato Angular funciona sem injetar URL de backend em runtime.
- Nginx pode servir estáticos e atuar como limite simples de rede.

### Negativas

- A configuração do proxy faz parte do caminho crítico e precisa de testes E2E.
- Logs e health checks atravessam dois serviços.
- HTTPS não é fornecido na demonstração local; terminação TLS de produção está fora de escopo.

## Alternativas rejeitadas

- Expor frontend e API em portas diferentes com CORS: aumenta configuração e superfície sem benefício.
- Servir o Angular pelo ASP.NET Core: misturaria os pipelines de build e reduziria a clareza dos contêineres.
- Servidor de desenvolvimento Angular na entrega: não é apropriado para servir artefatos estáticos.

## Rastreabilidade

`NFR-OPS-01`, `NFR-OPS-02`, `PREM-OPS-01`, `OPS-DOCKER-01`, `OPS-DOCKER-02`.
