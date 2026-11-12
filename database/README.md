# Banco original e correspondência das classes

`fecho.sql` é uma cópia integral do arquivo fornecido pela equipe. Os CRUDs de categorias, usuários e objetos conectam-se a esse esquema usando MySqlConnector.
Veja [setup e demonstração](../docs/CRUD-MYSQL.md). A aplicação não importa o
script automaticamente; o Compose o importa na criação de um volume vazio.
Os testes de domínio validam regras, hashes e algoritmos; as suítes de integração e Web validam MySQL real.

| Tabela | Classe | Relações representadas |
| --- | --- | --- |
| `usuarios` | `Usuario` | Identidade e dados cadastrais; senha como hash PBKDF2 |
| `categorias` | `Categoria` | Classificação dos objetos e desejos |
| `objetos` | `Objeto` | `Proprietario.Id` → `usuario_id`; `Categoria.Id` → `categoria_id` |
| `desejos` | `Desejo` | `Usuario.Id` e categoria opcional |
| `interesses` | `Interesse` | `Usuario.Id`, `Objeto.Id` e tipo LIKE/DISLIKE |
| `matches` | `Match` | `Usuario1`, `Usuario2`, `Objeto1` e `Objeto2` |
| `trocas` | `Troca` | `Match.Id` → `match_id`, data e observação |

Os nomes dos enums C# seguem os valores dos enums do SQL. Os IDs são inteiros.
Nos três CRUDs persistentes são gerados pelo MySQL; na demonstração
anterior de negociação são gerados pelos repositórios em memória.
Os horários da aplicação usam UTC. Valor estimado é `decimal?`, não `double`.

As restrições de unicidade do SQL foram refletidas nos repositórios de usuários,
categorias, interesses e trocas. As regras de reciprocidade, participação e
conclusão ficam nos serviços e não são garantidas apenas pelas chaves estrangeiras.

Telefone e senha do usuário têm visibilidade interna ao Core. O serviço autorizado
de contato devolve somente o telefone do outro participante. `UsuarioService`
encaminha o telefone e o hash ao repositório no cadastro. A leitura reconstitui
`SenhaProtegida` sem recalcular nem revelar a senha original. O console não lista
telefones ou hashes. A edição do perfil preserva a senha; a opção Alterar senha
grava um novo hash com salt individual. Dados existentes com senha em outro
formato precisam de redefinição pelo ID antes de serem lidos pelo novo adaptador.

O INSERT de usuários e objetos grava a data com `UTC_TIMESTAMP()`. Consultas
preservam a data gravada e os estados `ativo`/`status`; a edição não redefine esses
metadados. O perfil permite alterar `ativo`; o CRUD de objetos não muda seu status
nem transfere sua propriedade. Não é necessária alteração no esquema SQL.

No diagrama dos slides, `Interesses` aparece com `ID_Categoria`; a implementação
segue `objeto_id` do SQL e a explicação textual de interesses em objetos.

As telas também mostram quatro fotos por objeto, prioridade de desejos, OAuth,
avaliações e verificação. Esses itens não foram acrescentados à estrutura original
nem simulados como funcionalidades prontas. A foto do objeto nesta etapa corresponde
ao campo único `imagem`.

O código cria `Troca` somente na conclusão, preenchendo sua data. O SQL permite
data nula, mas essa possibilidade não é usada pela regra desta entrega.

Para futura importação em uma instância onde o banco `fecho` ainda não existe:

```sh
mysql -u SEU_USUARIO -p < database/fecho.sql
```

Depois da importação, configure as variáveis FECHO_DB_* e execute o CRUD,
conforme [CRUD-MYSQL.md](../docs/CRUD-MYSQL.md).

## Persistência do MVP Web

As sete tabelas do SQL original são usadas no MVP: usuários, categorias, objetos,
desejos, interesses, matches e trocas. A unidade de negociação mantém as alterações
da conclusão na mesma transação. O algoritmo da tela recebe objetos reconstruídos
do banco e aplica busca, filtros, ordenação e paginação em memória.

O telefone só é exposto ao titular do perfil ou a participantes autorizados de
um match. As regras de sessão e propriedade são aplicadas nas rotas Web.
Dados de exemplo são opcionais e criados pelo comando `bash scripts/fecho.sh seed`;
importar o esquema não cria automaticamente contas, categorias ou objetos de demonstração.
