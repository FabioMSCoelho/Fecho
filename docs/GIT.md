# Git, publicação e colaboração

## Histórico real desta base

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

1. Na conta escolhida pela dupla, criem um repositório vazio, por exemplo `fecho`.
   Não inicializem outro README ou `.gitignore`, pois eles já existem aqui.
2. Escolham a visibilidade e garantam que o professor terá acesso. Em repositório
   privado, o link sozinho não concede acesso.
3. Copiem a URL de clonagem mostrada pelo GitHub. Na pasta local, substituam
   `URL_DO_REPOSITORIO` pela URL real:

```sh
git remote add origin URL_DO_REPOSITORIO
git push -u origin main
git push origin feature/classes-base feature/regras-e-colecoes docs/entrega
```

Se já existir um `origin`, confiram `git remote -v` antes de adicionar outro.
Usem a autenticação oferecida pelo GitHub/gerenciador de credenciais ou SSH.
Não coloquem token de acesso dentro de uma URL versionada, arquivo do projeto
ou mensagem de commit.

O **link para submissão** é a página do repositório no navegador. Este documento
não contém uma URL inventada: a URL real só existe após a publicação na conta
escolhida e o envio dos commits. O mesmo histórico pode ser enviado ao GitLab;
o workflow em `.github/` é específico do GitHub.

Após publicar, confiram a aba de commits, as quatro branches e o workflow
`Validar entrega`. O workflow foi preparado localmente; a execução online
depende do push e da disponibilidade do serviço.

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
