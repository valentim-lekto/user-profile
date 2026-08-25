# ADR-0004 — Nginx e origem única

**Status:** aceita · **Data:** 2026-08-24

## Contexto

O avaliador deve iniciar frontend, backend e persistência com Compose e acessar a aplicação sem conhecer múltiplas portas. O Angular precisa chamar a API sem configuração de URL por ambiente no bundle.

## Decisão

- Compilar o Angular em uma imagem multi-stage e servir os arquivos estáticos com Nginx.
- Publicar somente `http://localhost:8080` pelo serviço `web`.
- Encaminhar `/api/*`, `/swagger/*` e `/health` ao serviço interno `api:8080`.
- Converter falha de conexão ou timeout gerada pelo próprio Nginx (`502`/`504`) em `503 application/problem+json`, sem interceptar respostas já formatadas pela API.
- Executar a única probe do Compose no serviço `web`, com BusyBox `wget` contra `http://127.0.0.1:8080/health`; `web` depende de `api` apenas como `service_started`.
- Usar URLs relativas no frontend e fallback para `index.html` apenas nas rotas da SPA.
- Não habilitar CORS amplo na entrega Docker; o proxy de desenvolvimento do Angular reproduz a mesma origem localmente.
- Fixar a imagem em `nginx:1.30.4-alpine3.24-slim`.

## Consequências

### Positivas

- Uma URL para o avaliador, sem CORS e sem expor a API diretamente no host.
- O mesmo artefato Angular funciona sem injetar URL de backend em runtime.
- Nginx pode servir estáticos e atuar como limite simples de rede.
- O frontend recebe o mesmo formato de erro quando o upstream está indisponível, em vez da página HTML padrão do proxy.

### Negativas

- A configuração do proxy faz parte do caminho crítico: conexão recusada exige teste runtime, e os mapeamentos de `502` e `504` exigem inspeção automatizada.
- Logs e health checks atravessam dois serviços.
- HTTPS não é fornecido na demonstração local; terminação TLS de produção está fora de escopo.

## Alternativas rejeitadas

- Expor frontend e API em portas diferentes com CORS: aumenta configuração e superfície sem benefício.
- Servir o Angular pelo ASP.NET Core: misturaria os pipelines de build e reduziria a clareza dos contêineres.
- Servidor de desenvolvimento Angular na entrega: não é apropriado para servir artefatos estáticos.

## Rastreabilidade

`NFR-OPS-01`, `NFR-OPS-02`, `PREM-OPS-01`, `OPS-DOCKER-01`, `OPS-DOCKER-02`.
