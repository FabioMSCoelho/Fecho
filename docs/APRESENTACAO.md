# Roteiro do debate — até 10 minutos

As falas abaixo são uma proposta para ensaio. Os nomes dos integrantes não
representam autoria individual de commits. Priorizem abrir o código e executar
a demonstração; não é necessário criar mais slides para explicar esta etapa.

## Antes da aula

Na raiz do repositório:

```sh
bash scripts/fecho.sh build
bash scripts/fecho.sh test
git log --graph --oneline --all --decorate
```

Confiram `41/41 cenários aprovados` e deixem abertas as abas indicadas na tabela.
Em Windows, usem os comandos equivalentes no README. Depois de instalado o SDK,
a demonstração funciona sem MySQL e sem acesso à internet. Para mostrar o
repositório online, será preciso publicá-lo antes da entrega, conforme `GIT.md`.

## Tempo e demonstração

| Tempo | Fala sugerida | Arquivo ou ação |
| --- | --- | --- |
| 0:00–0:40 | Fábio apresenta problema, escopo e fluxo | README; dizer que é a base backend em memória |
| 0:40–2:10 | Fábio mostra herança e reaproveitamento | `EntidadeBase.cs`, `Usuario.cs`, `Objeto.cs` |
| 2:10–3:10 | Fábio mostra herança nos repositórios | `RepositorioMemoria.cs` e `RepositorioUsuarios.cs` |
| 3:10–4:30 | Fábio mostra polimorfismo de regras | `CriterioCompatibilidade.cs`, as duas subclasses e chamada no `Program.cs` |
| 4:30–6:00 | Giovana explica List e HashSet | `DescobertaService.cs` e `TrocaService.cs` |
| 6:00–7:20 | Giovana executa a demonstração | `bash scripts/fecho.sh demo` |
| 7:20–8:20 | Giovana mostra Git e instruções de execução | Histórico, branches e README |
| 8:20–9:30 | Dupla justifica decisões e limites | Perguntas abaixo e resultado dos testes |
| 9:30–10:00 | Margem para pergunta do professor | Retomar o arquivo relacionado |

## Falas de apoio

### Contexto

“No Fechô, interesse é uma preferência unilateral. Match exige reciprocidade
entre dois usuários em dois objetos específicos. Esta etapa implementa as
classes e as regras em memória, a partir do banco que já apresentamos.”

### Herança

“As sete entidades possuem uma identidade. Colocamos essa identidade, sua
validação e a igualdade em EntidadeBase. Assim, não repetimos Equals e
GetHashCode em cada classe. A mudança fica concentrada na superclasse.”

Mostrem `Usuario : EntidadeBase` e `Objeto : EntidadeBase`. Em seguida mostrem
a `List<EntidadeBase>` no console chamando `Resumir()` sem verificar os tipos.

“Também reaproveitamos o repositório genérico. A operação de adicionar chama
ValidarNovo; o repositório de usuários sobrescreve esse ponto para impedir
e-mails duplicados, sem copiar o código de armazenamento.”

### Polimorfismo

“O serviço recebe a abstração CriterioCompatibilidade. PorCategoria faz uma
busca ampla; PorPalavrasChave usa as palavras do desejo. A chamada é a mesma,
e a implementação escolhida em execução muda o resultado.”

Na demonstração: categoria retorna violão, teclado e guitarra; palavras-chave
retornam apenas o violão. Não confundam recomendação com reciprocidade.

### Coleções

“A lista controla a ordem dos cards e permite acessar o primeiro pelo índice.
Um mesmo objeto pode satisfazer vários desejos; usamos HashSet para ele não
aparecer duas vezes. No match usamos um conjunto de pares normalizados para
não duplicar a mesma combinação de objetos na ordem inversa.”

### Execução

Apontem os resultados no terminal: zero matches com um único like; um match
após o segundo; repetição sem duplicatas; contato recusado a terceiro; dois
objetos trocados e proposta concorrente cancelada. A demo inteira dura poucos
segundos depois de compilada.

### Git

```sh
git branch --list
git log --graph --oneline --all --decorate
git show --stat feature/classes-base
git show --stat feature/regras-e-colecoes
```

“Separamos classes base, regras/coleções e documentação em branches. A main
recebe as integrações com commits de merge. Este é o histórico real da criação
desta base. Para o trabalho em dupla daqui em diante, cada responsável abre
sua branch e a outra pessoa revisa o pull request.”

Não afirmem que houve revisão de pull request ou commits de ambos se isso ainda
não ocorreu. A distribuição futura está proposta em `GIT.md`.

## Perguntas prováveis

**List impede valores repetidos?** Não. A lista mantém sequência. O HashSet
evita que o mesmo objeto seja adicionado mais de uma vez à lista de cards.

**O Set sempre mantém os elementos ordenados?** Não se deve depender da ordem
de um HashSet. A lista final recebe ordenação explícita.

**Herança só para mostrar o conceito?** A identidade e igualdade são usadas
por todas as entidades, e os repositórios compartilham operações reais. Não
foram inventados papéis de usuário para criar uma hierarquia.

**Por que não houve uma refatoração de código antigo?** Não havia código.
As abstrações foram feitas na criação da base. O trecho de duplicação na
arquitetura é didático e deixa isso explícito.

**Onde está o MySQL?** O SQL original está em `database/fecho.sql`. O objetivo
desta etapa é gerenciar os objetos em memória antes da persistência. A integração
MySQL exige um adaptador, atualizações e transações em uma etapa posterior.

**O que acontece com vários matches para a mesma câmera?** Eles podem existir
enquanto são propostas. Ao concluir uma troca, os outros matches que usam a
câmera são cancelados e seu contato é revogado.

**Já é uma aplicação web pronta?** É a base de domínio e serviços desta etapa.
API, interface, sessão de autenticação e persistência real serão construídas
sobre essa base conforme as próximas entregas.
