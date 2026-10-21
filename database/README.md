# Banco original e correspondência das classes

`fecho.sql` é uma cópia integral do arquivo fornecido pela equipe. O código desta
etapa não executa o script nem abre uma conexão MySQL. Os testes validam regras
em memória, não comprovam persistência ou comportamento de um servidor MySQL.

| Tabela | Classe | Relações representadas |
| --- | --- | --- |
| `usuarios` | `Usuario` | Identidade e dados cadastrais; senha como hash PBKDF2 |
| `categorias` | `Categoria` | Classificação dos objetos e desejos |
| `objetos` | `Objeto` | `Proprietario.Id` → `usuario_id`; `Categoria.Id` → `categoria_id` |
| `desejos` | `Desejo` | `Usuario.Id` e categoria opcional |
| `interesses` | `Interesse` | `Usuario.Id`, `Objeto.Id` e tipo LIKE/DISLIKE |
| `matches` | `Match` | `Usuario1`, `Usuario2`, `Objeto1` e `Objeto2` |
| `trocas` | `Troca` | `Match.Id` → `match_id`, data e observação |

Os nomes dos enums C# seguem os valores dos enums do SQL. Os IDs são inteiros
gerados pelos repositórios em memória; na integração real, o MySQL deverá gerá-los.
Os horários da aplicação usam UTC. Valor estimado é `decimal?`, não `double`.

As restrições de unicidade do SQL foram refletidas nos repositórios de usuários,
categorias, interesses e trocas. As regras de reciprocidade, participação e
conclusão ficam nos serviços e não são garantidas apenas pelas chaves estrangeiras.

Telefone e senha do usuário têm visibilidade interna ao Core. O serviço autorizado
de contato devolve somente o telefone do outro participante. O adaptador futuro
precisará de um mapeamento explícito dos campos internos e da reidratação dos hashes.

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

Criar as tabelas não integra a aplicação ao banco. Consulte os passos necessários
em [ARQUITETURA.md](../docs/ARQUITETURA.md).
