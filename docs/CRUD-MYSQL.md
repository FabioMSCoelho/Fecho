# Entrega — CRUD com persistência MySQL

O CRUD desta entrega gerencia **categorias**, tabela do SQL original da Entrega 4.
Permite cadastrar nome e descrição, listar, buscar por ID, atualizar e excluir.
IDs são gerados pelo MySQL; encerrar o console não apaga dados. A demonstração
anterior de matches continua separada, em memória. Não há persistência de todo
o fluxo de negociação nesta entrega nem interface web.

## Preparação local (.NET 10 e MySQL 8.4)

Na raiz do repositório, instale o SDK .NET 10 e escolha uma das opções abaixo.
O build agora restaura MySqlConnector 2.6.2 pelo NuGet oficial; requer Internet
na primeira restauração. Não coloque senhas em código, commits ou capturas de tela.

### Opção A: Docker Compose

1. Copie `.env.example` para `.env` e preencha `FECHO_DB_PASSWORD` e
   `MYSQL_ROOT_PASSWORD` com senhas locais diferentes. O arquivo é ignorado pelo Git.
   Mantenha `FECHO_DB_USER=fecho` e `FECHO_DB_NAME=fecho`, usados pelo Compose.
2. Execute `docker compose up -d --wait`. Em Linux, se o container indicar
   `Permission denied` ao ler o SQL, execute `chmod a+r database/*.sql` antes
   da primeira inicialização: os scripts precisam ser legíveis pelo usuário MySQL.
3. Exporte as variáveis para o console (o Compose lê `.env`, o .NET não).
   Em Bash, se o arquivo usa atribuições shell válidas:

   ```sh
   set -a
   source .env
   set +a
   bash scripts/fecho.sh crud
   ```

   Se a senha contém caracteres especiais, use aspas simples no `.env`.
   O usuário da aplicação recebe somente SELECT/INSERT/UPDATE/DELETE em `fecho.*`.
   O banco escuta apenas em `127.0.0.1`; o volume `mysql_data` preserva os dados.
   Para parar: `docker compose down`. Não use `down -v` se quiser preservar dados.
   Scripts de inicialização e novas senhas só se aplicam automaticamente a um
   volume vazio; em banco existente, use administração MySQL para ajustes.

### Opção B: MySQL já instalado

Se o banco da Entrega 4 já existe, use-o **sem reimportar**. Em banco novo:

```sh
mysql -u root -p < database/fecho.sql
```

Crie um usuário local com senha escolhida por você e conceda as quatro permissões
em `fecho.*` no cliente administrativo do MySQL. Não rode o console como root.
Defina `FECHO_DB_HOST`, `FECHO_DB_PORT`, `FECHO_DB_NAME`, `FECHO_DB_USER` e
`FECHO_DB_PASSWORD` no ambiente do terminal e execute `bash scripts/fecho.sh crud`.
O host padrão é `127.0.0.1`, porta `3306`, banco/usuário `fecho`.

PowerShell: use `$env:FECHO_DB_PASSWORD = 'sua senha local'` e o mesmo padrão
para as demais variáveis; depois compile e execute:

```powershell
dotnet build Fecho.slnx --configuration Release --disable-build-servers -m:1
dotnet src/Fecho.Console/bin/Release/net10.0/Fecho.Console.dll --crud
```

A conexão usa TLS quando oferecido (`Preferred`), adequado ao banco local desta
entrega; não se destina a um banco remoto de produção sem configuração adicional.

## Demonstração prática ao professor

Esta etapa pede o CRUD funcionando localmente e o repositório atualizado.
O roteiro de seminário em `APRESENTACAO.md` pertence à entrega anterior.

| Operação | Opção no menu | Efeito no MySQL |
| --- | --- | --- |
| Create | `1 Cadastrar` | `INSERT`, com ID gerado pelo banco |
| Read | `2 Listar` / `3 Buscar por ID` | `SELECT`, com resultado convertido em `Categoria` |
| Update | `4 Atualizar` | `UPDATE` do nome e da descrição pelo ID |
| Delete | `5 Excluir` | `DELETE` pelo ID, após confirmação com `SIM` |

O caminho dos dados é: `MenuCategorias` → `CategoriaService` →
`ICrudCategorias` → `RepositorioCategoriasMySql` → `ConexaoMySql` → MySQL.
O driver é MySqlConnector; esta implementação usa SQL parametrizado, sem ORM.

1. Inicie o CRUD e cadastre uma categoria com nome único e descrição.
   Mostre o ID devolvido pelo banco.
2. Liste as categorias e busque pelo ID.
3. Saia com `0`, abra o CRUD novamente e busque o mesmo ID: prova de persistência
   entre processos. Opcionalmente confira no cliente MySQL:
   `SELECT id, nome, descricao FROM categorias ORDER BY id;`.
4. Atualize nome e descrição; consulte de novo. Descrição vazia vira SQL NULL.
5. Tente cadastrar o mesmo nome: deve aparecer uma mensagem de duplicidade.
6. Exclua a categoria de demonstração, confirmando com `SIM`; consulte para
   mostrar que não existe mais. Uma categoria vinculada a objetos/desejos não
   pode ser excluída: as chaves estrangeiras preservam a integridade.
7. Pare o banco e tente iniciar o CRUD para mostrar erro de conexão; reinicie-o.
   Uma senha incorreta deve produzir mensagem de acesso negado, sem expor valores.

A aplicação não importa nem recria o banco ao iniciar e não executa limpeza
automática das categorias do usuário. Entradas inválidas não encerram o menu.
Falha de conexão inicial retorna código 1; saída normal retorna 0.

## Organização e testes

- `Categoria` valida o domínio; `CategoriaService` orquestra o CRUD.
- `ICrudCategorias` separa o serviço da persistência e deixa o banco gerar IDs.
- `ConexaoMySql` concentra configuração, abertura e mensagens seguras de falha.
- `RepositorioCategoriasMySql` usa comandos parametrizados e descarta conexões,
  comandos e leitores com `using`. Cada alteração é uma instrução SQL atômica.
- `MenuCategorias` cuida exclusivamente da interação no terminal.

```sh
bash scripts/fecho.sh build
bash scripts/fecho.sh test         # 41 cenários anteriores, sem depender de MySQL
bash scripts/fecho.sh integration  # MySQL real com o esquema completo importado
bash scripts/fecho.sh demo         # demonstração anterior em memória
```

Execute a integração em banco de teste: cria fixtures com nomes únicos e remove
somente seus registros ao terminar. Verifica CRUD, novas conexões, UTF-8/NULL,
unicidade, entrada SQL literal, validação, FK, ID ausente, credenciais inválidas
e servidor indisponível. A suíte inicia processos independentes para confirmar
que cadastro, atualização e exclusão são persistentes: não compartilham objetos
em memória nem o pool de conexões do processo principal.
O resultado esperado é **23/23 verificações de integração aprovadas**.
A CI também sobe um MySQL real. `dotnet test` não executa os verificadores console.

Na revisão local de 04/11/2026, passaram os 41 cenários de domínio, as 23
verificações de integração e 10 verificações adicionais do menu, com entrada
por terminal e reinício entre operações. A compilação passou sem avisos ou erros.
Os testes usaram um banco separado, com o esquema completo da Entrega 4.

## GitHub e colaboração

- Repositório da entrega: [FabioMSCoelho/Fecho](https://github.com/FabioMSCoelho/Fecho).
- `feature/crud-categorias-mysql`: preserva o commit local original da implementação.
- `feature/validacao-crud-mysql`: acrescenta a comprovação de persistência entre processos.
- `docs/entrega-crud`: atualiza o roteiro prático, o README e a documentação da sprint.

O commit original do CRUD foi mantido; os ajustes de validação e documentação
foram registrados separadamente e integrados à `main` com commits de merge.

Veja `git diff` e execute as verificações antes de commitar. Cada integrante deve
registrar os próprios commits com a autoria real e revisar a contribuição do outro.
Não divida artificialmente a autoria nem simule colaboração. Para esta sprint,
registrem implementação, validação e documentação conforme o trabalho realizado.
A entrega exige enviar os commits ao GitHub e demonstrar o CRUD ao professor;
publicar o ambiente de nuvem não substitui atualizar o repositório.
