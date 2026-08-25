# User Profile Web

Shell Angular standalone do milestone M1, gerado com Angular CLI 22.1.3 e executado com Node 24.19.0.

## Desenvolvimento

Com a API disponível em `http://localhost:5080`:

```sh
npm ci
npm start
```

O Angular CLI atende em `http://localhost:4200` e encaminha `/api`, `/swagger` e `/health` pelo arquivo `proxy.conf.json`.

## Gates locais

```sh
npm run lint
npm test
npm run build
```

As telas funcionais e as jornadas E2E pertencem aos milestones posteriores.
