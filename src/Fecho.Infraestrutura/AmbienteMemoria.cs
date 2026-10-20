using Fecho.Core.Contratos;
using Fecho.Core.Entidades;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.Memoria;

namespace Fecho.Infraestrutura;

/// <summary>Composição explícita, sem framework de injeção de dependências.</summary>
public sealed class AmbienteMemoria
{
    public IRepositorio<Usuario> Usuarios { get; } = new RepositorioUsuarios();
    public IRepositorio<Categoria> Categorias { get; } = new RepositorioCategorias();
    public IRepositorio<Objeto> Objetos { get; } = new RepositorioMemoria<Objeto>();
    public IRepositorio<Desejo> Desejos { get; } = new RepositorioMemoria<Desejo>();
    public IRepositorio<Interesse> Interesses { get; } = new RepositorioInteresses();
    public IRepositorio<Match> Matches { get; } = new RepositorioMemoria<Match>();
    public IRepositorio<Troca> Trocas { get; } = new RepositorioTrocas();
    public CadastroService Cadastro { get; }
    public DescobertaService Descoberta { get; }
    public TrocaService Negociacao { get; }

    public AmbienteMemoria()
    {
        Cadastro = new CadastroService(Usuarios, Categorias, Objetos, Desejos);
        Descoberta = new DescobertaService(Usuarios, Objetos, Desejos, Interesses);
        Negociacao = new TrocaService(Usuarios, Objetos, Interesses, Matches, Trocas);
    }
}
