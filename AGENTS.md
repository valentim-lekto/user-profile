# Regras para agentes

- Antes de alterar comportamento, leia `docs/sdd/README.md` e os documentos SDD pertinentes.
- Implemente uma única etapa do plano por vez.
- Associe cada mudança a critérios de aceite identificados.
- Inicie toda mudança de comportamento pela atualização da especificação e inclua os testes automatizados correspondentes.
- Nunca remova, desative ou enfraqueça testes para fazer o build passar.
- Nunca registre em logs nem versione segredos, senhas ou tokens.
- Obtenha a identidade do usuário autenticado exclusivamente do claim `sub` do JWT, nunca de IDs enviados pelo cliente.
- Considere uma tarefa pronta somente após build e testes aprovados, revisão do diff, documentação atualizada e rastreabilidade entre especificação, critérios de aceite, implementação e testes.
