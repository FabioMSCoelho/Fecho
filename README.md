# Fechô — classes base e debate arquitetural

Projeto acadêmico de **Fábio Coelho e Giovana Almeida**. O Fechô conecta pessoas
que desejam trocar objetos: duas curtidas recíprocas em objetos específicos geram
um match e liberam o contato para combinar a troca.

Esta entrega contém as classes base do backend em **C# / .NET 10**, com herança,
polimorfismo, coleções em memória e uma demonstração por terminal. O modelo segue
as sete entidades dos slides e preserva o [SQL original](database/fecho.sql).

## Executar em Linux ou macOS

Instale o **SDK .NET 10**, não apenas o runtime, pelo
[site oficial da Microsoft](https://dotnet.microsoft.com/download/dotnet/10.0).
Na pasta do repositório:

```sh
bash scripts/fecho.sh demo
bash scripts/fecho.sh test
```

O script também reconhece um SDK instalado localmente em `.tools/dotnet`.
Após instalar o SDK, a compilação não baixa pacotes: todos os projetos usam
somente a biblioteca padrão. MySQL não precisa estar instalado para rodar esta etapa.

## Executar em Windows ou diretamente pelo .NET

No terminal, na raiz do projeto:

```sh
dotnet --version
dotnet build Fecho.slnx --configuration Release --disable-build-servers -m:1
dotnet src/Fecho.Console/bin/Release/net10.0/Fecho.Console.dll
dotnet tests/Fecho.Testes/bin/Release/net10.0/Fecho.Testes.dll
```

O arquivo `global.json` seleciona um SDK estável da série 10.0.
Também é possível abrir `Fecho.slnx` em um editor compatível com esse SDK.

## O que observar na demonstração

1. Uma `List<EntidadeBase>` chama implementações diferentes de `Resumir()`.
2. O mesmo serviço de descoberta recebe `PorCategoria` ou `PorPalavrasChave`.
   No cenário demonstrado, são três cards no primeiro critério e um no segundo.
3. Dois desejos compatíveis não repetem o card do violão, graças ao `HashSet`.
4. Um like sozinho gera **zero matches**. A reciprocidade gera o match correto.
5. Repetir o like mantém **um match e dois registros de interesse**.
6. Os participantes recebem o contato; um terceiro tem o acesso recusado.
7. Concluir a troca marca câmera e violão como `TROCADO` e cancela o match
   concorrente da câmera. Os objetos que não foram trocados continuam disponíveis.

Os nomes, e-mails e telefones da execução são fictícios. Os dados são recriados
a cada execução e não ficam gravados depois que o processo termina.

## Correspondência com a entrega

| Critério | Onde demonstrar |
| --- | --- |
| Herança de atributos e comportamento | [EntidadeBase](src/Fecho.Core/Entidades/EntidadeBase.cs), herdada pelas sete entidades |
| Reutilização de armazenamento | [RepositorioMemoria](src/Fecho.Infraestrutura/Memoria/RepositorioMemoria.cs) e subclasses com validações próprias |
| Polimorfismo nas regras | [CriterioCompatibilidade](src/Fecho.Core/Compatibilidade/CriterioCompatibilidade.cs) e [DescobertaService](src/Fecho.Core/Servicos/DescobertaService.cs) |
| `List<T>` | Ordem dos cards e do histórico de matches |
| `HashSet<T>` | Unicidade de cards, objetos avaliados e pares de objetos nos matches |
| Regras do negócio | [TrocaService](src/Fecho.Core/Servicos/TrocaService.cs) |
| Testes de comportamento | [41 cenários executáveis](tests/Fecho.Testes/Program.cs) |
| Commits e branches | `git log --graph --oneline --all --decorate` e [guia Git](docs/GIT.md) |
| Debate de até 10 minutos | [Roteiro com tempo e arquivos a abrir](docs/APRESENTACAO.md) |

## Organização

```text
src/
  Fecho.Core/             entidades, contratos, critérios e serviços de negócio
  Fecho.Infraestrutura/  repositórios em memória e composição dos serviços
  Fecho.Console/         demonstração reproduzível
tests/Fecho.Testes/       verificador dos cenários de negócio
database/                banco original e mapa entre tabelas e classes
docs/                    arquitetura, apresentação e versionamento
scripts/                 execução com o SDK global ou local
.github/workflows/       compilação, testes e demo no GitHub Actions
```

O Core não conhece o armazenamento concreto. A infraestrutura implementa
`IRepositorio<T>` e o console monta as dependências com `AmbienteMemoria`.
Veja as justificativas e os limites em [ARQUITETURA.md](docs/ARQUITETURA.md).

## Regras implementadas

- Exigir usuário ativo e pelo menos um objeto disponível para registrar interesse.
- Impedir like/dislike no próprio objeto e em objetos já trocados.
- Manter uma avaliação por usuário e objeto; mudar a avaliação atualiza o registro.
- Gerar match apenas quando há dois likes recíprocos para os objetos envolvidos.
- Liberar contato apenas a participantes de match ativo ou concluído.
- Revogar contato de match cancelado e bloquear acesso por terceiros.
- Registrar uma única conclusão, indisponibilizar os dois objetos e cancelar
  outros matches ativos que compartilhem qualquer um deles.
- Proteger unicidade de e-mail, categoria e par usuário/objeto nos repositórios.

## Testes e automação

O projeto `Fecho.Testes` é um verificador por console, sem framework externo:
cada cenário faz verificações e lança exceção se o resultado estiver incorreto.
O processo retorna **0** se todos passam e **1** se algum falha. Use os comandos
acima; `dotnet test` não executa essa suíte.

O resultado esperado é `41/41 cenários aprovados`. O workflow do GitHub compila,
executa a suíte e roda a demonstração em pushes e pull requests. Ele só será
executado online depois que o repositório for publicado.

## Escopo desta etapa

O banco foi modelado, mas a persistência real em MySQL pertence a uma próxima
integração. Esta entrega manipula os objetos em memória antes dessa persistência.
Não inclui API HTTP, interface web, sessão de login, envio de mensagens, OAuth,
galeria de fotos ou avaliações. A conferência de senha implementada é uma base
para autenticação; ainda não constitui um sistema de autenticação web.

O contato da demonstração é liberado como telefone, conforme a tela de detalhes
do match. A aplicação não envia mensagens nem acessa WhatsApp.

## Entrega do repositório

O código foi desenvolvido em branches e integrado à `main`, com commits reais
dos blocos implementados. As branches não comprovam contribuições individuais
dos integrantes: não há autoria ou colaboração simulada.

Siga [docs/GIT.md](docs/GIT.md) para publicar o histórico e obter o link que será
submetido. O roteiro traz uma proposta de divisão das falas da dupla.
