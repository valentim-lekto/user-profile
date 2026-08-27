# User Profile Web

Frontend Angular standalone/strict da aplicação de perfil, com cadastro,
login, dashboard e dois formulários protegidos de perfil. O guia suportado da
entrega e dos testes Docker está no [`README.md`](../../../README.md) da raiz.

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

As versões de Node/npm são fixadas no Dockerfile e no lock. A validação oficial
não exige SDK local: use o profile `frontend-tests` e o runner E2E descritos no
README raiz.
