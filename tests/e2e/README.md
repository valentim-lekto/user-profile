# Testes E2E

As três jornadas Playwright atravessam Nginx, Angular, API e SQLite reais:

1. cadastro, login, dashboard, edição cadastral e logout;
2. rota protegida anônima e login inválido;
3. troca de senha, encerramento da sessão e reautenticação.

Cada teste usa um contexto de navegador e emails próprios, sem seed, chamadas diretas à API, retries ou dependência de ordem. As senhas sintéticas são geradas e mantidas dentro do contexto do navegador, de modo que os argumentos registrados pelo Playwright não contenham credenciais. Execute a pilha isolada somente com Docker:

```sh
./scripts/e2e-playwright.sh
```

O script cria um projeto Compose exclusivo, espera o healthcheck e remove apenas os contêineres, a rede e o volume desse projeto. `E2E_ARTIFACTS_DIR` altera a raiz padrão `artifacts/e2e`; cada execução recebe um subdiretório próprio. Relatórios são gravados sempre, enquanto screenshots, traces minimizados sem snapshots de rede/DOM e logs do Compose são retidos somente em falha.
