# Git, publicação e colaboração

Repositório para submissão: [FabioMSCoelho/Fecho](https://github.com/FabioMSCoelho/Fecho).

## Sprint atual — CRUD MySQL

A implementação inicial do CRUD está na branch `feature/crud-categorias-mysql`.
Os testes adicionais e a documentação foram registrados em commits próprios.

| Branch | Conteúdo |
| --- | --- |
| `feature/crud-categorias-mysql` | Implementação inicial: conexão, driver, repositório, serviço, menu e configuração MySQL |
| `feature/validacao-crud-mysql` | Testes adicionais de persistência em processos independentes |
| `docs/entrega-crud` | Roteiro prático para o professor, README e documentação da sprint |
| `main` | Integração da implementação, validação e documentação |

As branches desta sprint são dependentes: validação parte do CRUD e documentação
parte da validação. A `main` integra validação e documentação com merges explícitos.

```sh
git log --graph --oneline --all --decorate
git show --stat feature/crud-categorias-mysql
git show --stat feature/validacao-crud-mysql
git show --stat docs/entrega-crud
```

Essa organização identifica blocos técnicos do trabalho. A colaboração entre
integrantes deve ser demonstrada pelas contribuições e revisões que realmente
realizaram; nomes de branches não representam, sozinhos, autoria individual.

## Histórico da etapa anterior de POO

O repositório local foi iniciado na criação do código. Foram usados blocos
de trabalho com commits e branches reais, sem alterar datas ou simular autores:

| Branch | Conteúdo |
| --- | --- |
| `main` | Integração dos blocos da entrega |
| `feature/classes-base` | Entidades, identidade compartilhada, critérios e repositórios |
| `feature/regras-e-colecoes` | Serviços, coleções, demo e testes; depende das classes base |
| `docs/entrega` | README, arquitetura, roteiro, publicação e CI |

As branches da implementação são dependentes: regras/coleções utiliza as classes
criadas antes. As duas são integradas à principal com merges explícitos.
As branches são preservadas para a demonstração.

```sh
git status
git branch --list
git log --graph --oneline --all --decorate
git show --stat feature/classes-base
git show --stat feature/regras-e-colecoes
```

Não havia código anterior a refatorar. Não foi fabricado histórico de semanas
anteriores, autoria de Giovana, revisão por outra pessoa ou pull request.
Esses registros só devem ser apresentados quando efetivamente existirem.

## Publicar no GitHub

O repositório já existe. Confira `git remote -v`: a URL atual é
`https://github.com/FabioMSCoelho/Fecho.git`. Para enviar novos commits e as
branches desta etapa:

```sh
git push -u origin main
git push origin feature/crud-categorias-mysql feature/validacao-crud-mysql docs/entrega-crud
```

Usem a autenticação oferecida pelo GitHub/gerenciador de credenciais ou SSH.
Não coloquem token de acesso dentro de uma URL versionada, arquivo do projeto
ou mensagem de commit.

O **link para submissão** é a página principal do repositório, indicada no início
deste documento. Confiram o acesso do professor e a aba de commits.

Depois do push, acompanhem o workflow `Validar entrega`: ele compila, executa
os 41 testes de domínio, a demonstração anterior e 23 verificações do CRUD em
MySQL real. A execução online depende do push e da disponibilidade do serviço.

`git status` compara a branch com a referência remota guardada localmente.
Depois de uma alteração feita diretamente no GitHub, executem `git fetch origin`
antes de analisar se estão à frente ou atrás. O fetch atualiza referências e
não modifica os arquivos de trabalho.

## Próximas alterações em dupla

Proposta de divisão a combinar e executar pela equipe:

| Responsável sugerido | Área | Exemplo de branch futura |
| --- | --- | --- |
| Fábio | Persistência e serviços de troca | `feature/persistencia-mysql` |
| Giovana | Interface e integração dos cards | `feature/interface-descoberta` |
| Ambos | Revisão cruzada e preparação das entregas | Pull requests para `main` |

Cada pessoa deve configurar sua própria identidade na sua cópia local:

```sh
git config user.name "Seu nome"
git config user.email "Seu e-mail do GitHub"
git switch main
git pull --ff-only origin main
git switch -c feature/nome-da-tarefa
```

Depois de implementar e validar:

```sh
git add CAMINHOS_DOS_ARQUIVOS_ALTERADOS
git commit -m "feat: descreve a mudança feita"
git push -u origin feature/nome-da-tarefa
```

Abram um pull request para `main`; a outra pessoa verifica os arquivos e os
testes antes de integrar. Commits devem descrever alterações reais. Usem a
identidade de quem fez o trabalho e não alterem a autoria para aparentar uma
divisão que não ocorreu.
