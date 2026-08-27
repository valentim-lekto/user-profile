# Testes E2E

As três jornadas Playwright atravessam Nginx, Angular, API e SQLite reais:

1. cadastro, login, dashboard, persistência de nome/email, logout e reproteção;
2. rota protegida anônima e login inválido;
3. troca de senha, reproteção sem sessão e reautenticação.

Cada teste usa um contexto de navegador e emails próprios, sem seed, chamadas diretas à API, retries ou dependência de ordem. Campos e botões críticos são localizados por seus nomes acessíveis. As senhas sintéticas são geradas e mantidas dentro do contexto do navegador, de modo que os argumentos registrados pelo Playwright não contenham credenciais. Execute a pilha isolada somente com Docker:

```sh
./scripts/e2e-playwright.sh
```

O script cria um projeto e volume Compose exclusivos por execução da suíte, espera o healthcheck e remove apenas os contêineres, a rede e o volume desse projeto. `E2E_ARTIFACTS_DIR` altera a raiz padrão `artifacts/e2e`; cada execução recebe um subdiretório próprio. JUnit e HTML são gravados sempre que o runner Playwright chega a iniciar, enquanto screenshots de campos mascarados e traces minimizados sem snapshots de rede/DOM são retidos somente em falha. Uma falha anterior ao runner ainda preserva os diagnósticos disponíveis do Compose. Em falha, serviços, imagens, `ps` e logs filtrados são preservados antes do teardown, sem publicar log bruto; falha de cleanup também reprova o comando.
