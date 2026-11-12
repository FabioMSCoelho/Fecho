# Entrega anterior — CRUD local com MySQL

A etapa atual é o **[MVP Web](MVP-WEB.md)**. Este documento registra o console de
CRUD, preservado para consulta e demonstração da etapa anterior.

## Preparação

Use o banco original da Entrega 4. Em banco existente, **não reimporte** o SQL.
Em uma instância nova, importe `database/fecho.sql` ou use o Docker Compose.
Preencha `.env` com host, porta, banco, usuário e senha `FECHO_DB_*`.
A aplicação agora lê esse arquivo automaticamente; variáveis exportadas têm prioridade.

```sh
bash scripts/preparar-local.sh
bash scripts/fecho.sh crud
```

A primeira linha é opcional se o MySQL já estiver instalado e configurado.
O SDK .NET 10 é necessário. O driver MySqlConnector é restaurado pelo NuGet oficial.

## Operações disponíveis

Menu inicial: `1 Categorias`, `2 Usuários`, `3 Objetos`, `0 Sair`.
Dentro de cada cadastro: `1 Cadastrar`, `2 Listar`, `3 Buscar por ID`,
`4 Atualizar`, `5 Excluir` e `0 Voltar`. Usuários também têm `6 Alterar senha`.

| Cadastro | Dados e regras |
| --- | --- |
| Categorias | Nome e descrição, nome único, exclusão protegida por vínculos |
| Usuários | Nome, e-mail único, telefone, cidade, foto, descrição e ativo; senha em hash PBKDF2 |
| Objetos | Dono ativo existente, categoria existente, nome, conservação, descrição, valor decimal e imagem |

O MySQL gera os IDs. Nome/e-mail e limites de campos são validados pelas classes.
Editar o perfil mantém o hash e a data original; a senha é alterada em operação
própria. Editar o objeto mantém seu dono, status e data de cadastro.
Campos opcionais vazios são gravados como NULL. UPDATE com valores idênticos
continua reconhecendo o registro existente.

O console é uma ferramenta administrativa local, sem sessão de login. A Web
acrescenta autenticação e autorização por proprietário. O console não lista
telefones ou hashes. A imagem informada no console é um caminho/URL; o upload
pela interface está disponível no MVP Web.

## Demonstração do CRUD

1. Cadastre uma categoria e um usuário; anote os IDs.
2. Cadastre um objeto associado aos dois IDs.
3. Liste e busque os registros.
4. Saia, execute novamente e busque o mesmo objeto para comprovar persistência.
5. Atualize o objeto e o perfil do usuário e consulte novamente.
6. Tente cadastrar o mesmo e-mail e excluir a categoria/dono ainda vinculados.
7. Exclua um objeto sem match, depois o usuário sem vínculos e a categoria.
8. Demonstre mensagem de conexão com servidor indisponível ou credencial incorreta.

As chaves estrangeiras seguem o SQL original. Excluir usuário/objeto pode também
remover desejos/interesses por cascata; confirme apenas registros de demonstração.
Objetos e usuários associados a matches não podem ser excluídos fisicamente.
O console exige confirmação `SIM` para exclusão.

## Organização e testes

`MenuCrud` compõe os serviços e repositórios. O Core conhece `ICrudCategorias`,
`ICrudUsuarios` e `ICrudObjetos`. A infraestrutura concentra SQL parametrizado,
conexões descartadas com `using` e tradução de erros. `RepositorioMySql` compartilha
esse comportamento com os três adaptadores.

```sh
bash scripts/fecho.sh build
bash scripts/fecho.sh test
bash scripts/fecho.sh integration
```

A integração tem 68 verificações em MySQL real, incluindo leituras em processos
separados após cadastro, atualização e exclusão; hashes, metadados, NULL, decimal,
FKs, duplicidade, entradas inválidas e erros de conexão. Use banco de teste.
Os registros criados pela suíte têm nomes únicos e são removidos ao terminar.

A aplicação não importa nem limpa o banco ao iniciar. Compose com volume existente
preserva dados; scripts e novas senhas só são aplicados automaticamente a volume vazio.
O ambiente local usa `127.0.0.1` e usuário MySQL com SELECT/INSERT/UPDATE/DELETE.
