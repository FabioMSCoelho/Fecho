# Fechô — MVP Web de troca de objetos

Projeto acadêmico de **Fábio Coelho e Giovana Almeida**, em C# / .NET 10,
ASP.NET Core MVC e MySQL 8.4. Um objeto parado pode ser o próximo achado de
alguém: dois interesses recíprocos geram um match e liberam o contato para a troca.

## Executar localmente

Pré-requisitos: **SDK .NET 10** e **Docker com Compose**. No Linux ou macOS,
na raiz do repositório:

```sh
bash scripts/preparar-local.sh --demo
bash scripts/fecho.sh web
```

Abra **http://localhost:5070**. O primeiro comando cria um `.env` com senhas
locais aleatórias se o arquivo ainda não existir, inicia o MySQL e prepara dados
fictícios. Arquivos de configuração existentes são preservados; o banco não é
apagado nem reimportado quando o volume já está inicializado.

As contas de demonstração são:

- `ana.demo@fecho.local`
- `bruno.demo@fecho.local`
- `clara.demo@fecho.local`

A senha está na chave **`FECHO_DEMO_PASSWORD` do seu `.env`**. Contas de exemplo
já existentes mantêm a senha anterior. Você também pode criar uma conta pela tela.
Para preparar somente o banco, use `bash scripts/preparar-local.sh` sem `--demo`.
A primeira compilação/restauração e o primeiro download da imagem MySQL exigem Internet.

Se já houver um MySQL na porta 3306 do computador, configure **`FECHO_DB_PORT=3307`**
no `.env` (se essa porta estiver livre) e repita a preparação. O Docker continua
usando o mesmo volume. Essa é a porta do banco; o site permanece em `localhost:5070`.

### Usar o MySQL da etapa anterior

Copie `.env.example` para `.env`, preencha a conexão `FECHO_DB_*` do banco existente
e execute `bash scripts/fecho.sh web`. A aplicação lê esse arquivo automaticamente;
variáveis já exportadas têm prioridade. **Não reimporte o SQL em um banco existente.**
O esquema original em [database/fecho.sql](database/fecho.sql) foi preservado.
O usuário da aplicação precisa de SELECT, INSERT, UPDATE e DELETE em `fecho.*`.

Em Windows/PowerShell, com o SDK e o MySQL preparados:

```powershell
dotnet build Fecho.slnx --configuration Release --disable-build-servers -m:1
Set-Location src/Fecho.Web
dotnet bin/Release/net10.0/Fecho.Web.dll
```

Opcionalmente, defina `FECHO_DEMO_PASSWORD` no `.env` e execute o mesmo DLL com
`--seed-demo` antes de iniciar o servidor. Instruções completas e roteiro de
até 15 minutos: **[docs/MVP-WEB.md](docs/MVP-WEB.md)**.

## O que funciona no MVP

| Fluxo | Comportamento |
| --- | --- |
| Conta e perfil | Cadastro, login, logout, edição do próprio perfil e alteração de senha |
| Meus objetos | Cadastrar, consultar, editar e excluir os próprios objetos; upload PNG/JPEG/WebP |
| Explorar | Busca sem diferenciar acentos, filtros por categoria, cidade, conservação e estimativa |
| Ordenação e paginação | Merge sort por nome, valor ou data; páginas com até nove objetos |
| Meus desejos | Desejos persistidos e recomendações que usam o critério de palavras-chave |
| Interesses e matches | Like/dislike, reciprocidade entre objetos específicos e contato restrito aos participantes |
| Conclusão | Transação registra a troca, marca os dois objetos como trocados e cancela matches concorrentes |

Todos esses dados usam as **sete tabelas do MySQL original**. Reiniciar o
servidor não recria os dados. As imagens enviadas e as chaves de sessão ficam em
`.tools/web`, fora do Git; mantenha essa pasta para preservar as imagens entre execuções.

## Correspondência com a entrega

| Requisito | Onde conferir |
| --- | --- |
| Interface Web integrada ao C# | [Controllers](src/Fecho.Web/Controllers), [views Razor](src/Fecho.Web/Views) e [Program.cs](src/Fecho.Web/Program.cs) |
| Formulários e listagens com MySQL | [Repositórios MySQL](src/Fecho.Infraestrutura/MySql) e [serviços de domínio](src/Fecho.Core/Servicos) |
| Algoritmos e estruturas de dados | [BuscaObjetos](src/Fecho.Core/Busca/BuscaObjetos.cs): busca linear, `List`, `HashSet`, `Dictionary` e merge sort |
| Validação e autorização | Validação no servidor, e-mail único, proprietário vindo da sessão, CSRF e conferência de participação no match |
| Consistência da negociação | [NegociacaoPersistenteService](src/Fecho.Core/Servicos/NegociacaoPersistenteService.cs) e [UnidadeNegociacaoMySql](src/Fecho.Infraestrutura/MySql/UnidadeNegociacaoMySql.cs) |
| Demonstração de até 15 minutos | [Roteiro do MVP](docs/MVP-WEB.md) |

O banco não define perfis Cliente/Administrador. As restrições Web são por
**usuário autenticado, propriedade do objeto e participação no match**.
O telefone só aparece ao titular do perfil e aos participantes autorizados;
hashes e senhas não são exibidos nas listagens.

## Testes

```sh
bash scripts/fecho.sh build
bash scripts/fecho.sh test        # 51 cenários: domínio, configuração, busca e ordenação
bash scripts/fecho.sh integration # 68 verificações dos CRUDs em MySQL real
bash scripts/validar-web.sh       # inicia servidor isolado na porta 5071 e valida o fluxo Web
```

As suítes que usam banco devem apontar para um **banco de teste**, com o esquema
completo. Criam registros com nomes únicos e removem apenas suas próprias fixtures.
A suíte Web verifica 41 cenários HTTP, incluindo acessos indevidos, CSRF, uploads,
recomendações, interesses simultâneos, conclusão concorrente e revogação de sessões.
`dotnet test` não executa esses verificadores por console.
O [workflow de CI](.github/workflows/ci.yml) compila e executa todas as suítes com
um MySQL separado em pushes e pull requests.

## Organização

```text
src/Fecho.Core/             entidades, contratos, regras e algoritmos
src/Fecho.Infraestrutura/  SQL parametrizado, repositórios e transações
src/Fecho.Web/             controllers, formulários, views e arquivos visuais
src/Fecho.Console/         CRUD local e demonstração anterior de POO
tests/Fecho.Testes/        domínio, hashes e algoritmos
tests/Fecho.Integracao/    persistência dos três CRUDs
tests/Fecho.Web.Testes/    navegação HTTP e regras do MVP
database/                 esquema original da equipe
docs/                     arquitetura, roteiro e entregas anteriores
scripts/                  preparação, execução e validação
```

O console anterior continua disponível: `bash scripts/fecho.sh crud` abre os
cadastros locais e `bash scripts/fecho.sh demo` executa o cenário de POO em memória.
A Web usa a persistência MySQL; a demonstração anterior continua independente.

## Escopo e documentação

Este MVP roda localmente. Oferece uma imagem por objeto, conforme o campo
`imagem` do esquema original; não inclui chat interno, pagamentos, OAuth,
avaliações ou confirmação dupla de entrega. Os participantes combinam a troca
pelo contato liberado após o match. Qualquer participante pode registrar a conclusão.

As telas seguem os fluxos registrados nos documentos anteriores. Os PDFs dos
wireframes não estavam mais acessíveis nos caminhos fornecidos; a conferência
visual com o material original permanece pendente.

- [Execução, demonstração e algoritmo](docs/MVP-WEB.md)
- [Escolhas arquiteturais](docs/ARQUITETURA.md)
- [Histórico e trabalho em equipe](docs/GIT.md)
- [Entrega anterior: CRUD no console](docs/CRUD-MYSQL.md)
- [Entrega anterior: POO](docs/APRESENTACAO.md)

Repositório para submissão: **[FabioMSCoelho/Fecho](https://github.com/FabioMSCoelho/Fecho)**.
O GitHub hospeda o código; o servidor C# e o MySQL são executados localmente para a demonstração.
