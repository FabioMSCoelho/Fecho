# Git, publicação e colaboração

Repositório para submissão: [FabioMSCoelho/Fecho](https://github.com/FabioMSCoelho/Fecho).
A demonstração funcional atual está em [MVP-WEB.md](MVP-WEB.md).

## Blocos técnicos da entrega Web

A ampliação aproveita o CRUD local de categorias, usuários e objetos e acrescenta
as camadas visuais, o algoritmo e a persistência da negociação.

| Branch | Conteúdo |
| --- | --- |
| `feature/persistencia-mvp` | Entidades, contratos, CRUDs, busca, regras e transações MySQL |
| `feature/mvp-web` | Interface MVC, sessão, formulários, imagens e verificações Web |
| `docs/entrega-mvp-web` | Preparação local, roteiro de até 15 minutos, documentação e CI |
| `main` | Versão integrada para executar e entregar |

As branches são dependentes: a interface utiliza os serviços do primeiro bloco,
e a documentação descreve o resultado completo. Os nomes identificam áreas de
implementação; não representam, sozinhos, contribuições de pessoas diferentes.

```sh
git status
git log --graph --oneline --all --decorate
git show --stat feature/persistencia-mvp
git show --stat feature/mvp-web
git show --stat docs/entrega-mvp-web
```

Os commits usam a identidade Git da conta responsável, descrevem alterações
reais e não recebem datas anteriores ao desenvolvimento. A colaboração da equipe
deve ser registrada pelas contribuições e revisões efetivamente realizadas.
Não foi fabricada autoria de outro integrante, revisão ou pull request.

## Trabalhar em dupla

Cada pessoa configura a própria identidade na sua cópia:

```sh
git config user.name "Seu nome"
git config user.email "Seu e-mail do GitHub"
git switch main
git pull --ff-only origin main
git switch -c feature/nome-da-tarefa
```

Depois de implementar e validar, selecione os arquivos da alteração:

```sh
git add CAMINHOS_DOS_ARQUIVOS
git commit -m "feat: descreve a mudança real"
git push -u origin feature/nome-da-tarefa
```

Abram um pull request e registrem a revisão da outra pessoa antes da integração.
Sugestões de trabalho futuro: aprimorar a interface com os wireframes originais,
melhorar mensagens/validações e ampliar cenários de teste conforme as alterações.
Os nomes dos integrantes só devem constar como autores ou revisores do trabalho
que realmente realizaram.

## Publicar e conferir

```sh
git remote -v
git push origin main
git push origin feature/persistencia-mvp feature/mvp-web docs/entrega-mvp-web
```

Use autenticação por gerenciador de credenciais ou SSH; não coloque tokens em
URLs ou arquivos versionados. `.env`, imagens enviadas, chaves e caches ficam
fora do Git. Confira os arquivos antes de adicionar novos caminhos.

O workflow `Validar entrega` compila a solução, executa os testes de domínio,
a demonstração anterior, a integração MySQL e a suíte Web com um servidor próprio.
O link para entrega é a página principal do repositório, não o endereço localhost.

`git status` compara com a referência remota guardada localmente. Antes de
investigar divergência com a página do GitHub, execute `git fetch origin`.
O fetch atualiza referências sem modificar seus arquivos de trabalho.

## Entregas anteriores

As branches anteriores de classes base, regras/coleções e CRUD foram mantidas.
`docs/APRESENTACAO.md` registra o seminário de POO e `docs/CRUD-MYSQL.md` registra
a demonstração do CRUD no console. Para esta etapa Web, use `docs/MVP-WEB.md`.
