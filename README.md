# Fechô — CRUD MySQL e classes de domínio

Projeto acadêmico de **Fábio Coelho e Giovana Almeida**. O Fechô conecta pessoas
que desejam trocar objetos: duas curtidas recíprocas em objetos específicos geram
um match e liberam o contato para combinar a troca.

O backend usa **C# / .NET 10** e preserva o [SQL original](database/fecho.sql).
A entrega atual acrescenta persistência MySQL ao CRUD de categorias. As classes,
a herança, o polimorfismo e a demonstração em memória da etapa anterior continuam disponíveis.

## Entrega atual: CRUD funcional no MySQL

O console agora oferece cadastro, listagem, busca, atualização e exclusão de
**categorias** no banco original. Acesso a dados separado em repositório,
MySqlConnector 2.6.2, consultas parametrizadas e tratamento de falhas de conexão,
duplicidade e vínculos por chave estrangeira.

Siga [a preparação local e o roteiro para o professor](docs/CRUD-MYSQL.md).
Com o MySQL preparado e as variáveis `FECHO_DB_*` exportadas:

```sh
bash scripts/fecho.sh crud
bash scripts/fecho.sh integration
```

A demonstração e os 41 testes da etapa anterior continuam disponíveis abaixo.
O CRUD de categorias persiste em MySQL; o cenário de negociação permanece em memória.

Esta entrega é demonstrada diretamente ao professor: cadastrar, consultar,
reiniciar o programa, atualizar e excluir um registro. A suíte de integração
executa **23 verificações em MySQL real**, incluindo leitura em processos separados.

| Requisito da entrega | Implementação |
| --- | --- |
| Conexão e erros iniciais | [ConexaoMySql](src/Fecho.Infraestrutura/MySql/ConexaoMySql.cs) |
| Inserir, listar, buscar, atualizar e excluir | [RepositorioCategoriasMySql](src/Fecho.Infraestrutura/MySql/RepositorioCategoriasMySql.cs) |
| Separação de responsabilidades | [CategoriaService](src/Fecho.Core/Servicos/CategoriaService.cs), [ICrudCategorias](src/Fecho.Core/Contratos/ICrudCategorias.cs) e [MenuCategorias](src/Fecho.Console/MenuCategorias.cs) |
| Persistência após encerrar o processo | [Suíte de integração](tests/Fecho.Integracao/Program.cs) e [consulta em outro processo](tests/Fecho.Integracao/ProcessoPersistencia.cs) |
| Histórico e branches desta sprint | [Guia Git](docs/GIT.md) |

## SDK e execução em Linux ou macOS

Instale o **SDK .NET 10**, não apenas o runtime, pelo
[site oficial da Microsoft](https://dotnet.microsoft.com/download/dotnet/10.0).
O CRUD exige o preparo do MySQL e das variáveis descrito no roteiro acima.
Para executar a demonstração anterior ou seus testes, na pasta do repositório:

```sh
bash scripts/fecho.sh demo
bash scripts/fecho.sh test
```

O script também reconhece um SDK instalado localmente em `.tools/dotnet`.
A primeira compilação restaura o driver MySqlConnector pelo NuGet. MySQL só é
necessário para `crud` e `integration`; a demo anterior e seus testes usam memória.

## Executar em Windows ou diretamente pelo .NET

No terminal, na raiz do projeto:

```sh
dotnet --version
dotnet build Fecho.slnx --configuration Release --disable-build-servers -m:1
dotnet src/Fecho.Console/bin/Release/net10.0/Fecho.Console.dll --crud
dotnet tests/Fecho.Testes/bin/Release/net10.0/Fecho.Testes.dll
```

O arquivo `global.json` seleciona um SDK estável da série 10.0.
Também é possível abrir `Fecho.slnx` em um editor compatível com esse SDK.

O comando `--crud` exige as variáveis `FECHO_DB_*` configuradas. Sem argumentos,
o console executa a demonstração anterior de negociação em memória.

## O que observar na demonstração anterior de POO

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

## Correspondência com a etapa anterior de POO

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
.github/workflows/       compilação, testes, demo e integração MySQL no GitHub Actions
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

O resultado esperado da suíte de domínio é `41/41 cenários aprovados`.
Com o banco preparado, a integração retorna `23/23 verificações de integração aprovadas`.
O workflow do GitHub compila, executa as duas suítes, roda a demonstração e inicia
um MySQL separado para validar a persistência em pushes e pull requests.

## Escopo desta etapa

O banco original agora é usado no CRUD de categorias. Os demais fluxos da
demonstração de POO ainda manipulam objetos em memória.
O CRUD de categorias desta entrega está descrito acima. Não inclui API HTTP,
interface web, sessão de login, envio de mensagens, OAuth,
galeria de fotos ou avaliações. A conferência de senha implementada é uma base
para autenticação; ainda não constitui um sistema de autenticação web.

O contato da demonstração é liberado como telefone, conforme a tela de detalhes
do match. A aplicação não envia mensagens nem acessa WhatsApp.

## Entrega do repositório

O código foi desenvolvido em branches e integrado à `main`, com commits reais
dos blocos implementados. As branches não comprovam contribuições individuais
dos integrantes: não há autoria ou colaboração simulada.

Link para submissão: [FabioMSCoelho/Fecho](https://github.com/FabioMSCoelho/Fecho).
O histórico desta sprint e os comandos de publicação estão em [docs/GIT.md](docs/GIT.md).
Para a demonstração prática atual, siga [docs/CRUD-MYSQL.md](docs/CRUD-MYSQL.md).
O roteiro `APRESENTACAO.md` registra a etapa anterior de POO.
