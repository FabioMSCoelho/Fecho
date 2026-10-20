using Fecho.Core.Contratos;
using Fecho.Core.Entidades;

namespace Fecho.Infraestrutura.Memoria;

/// <summary>Implementação compartilhada; subclasses só acrescentam suas restrições.</summary>
public class RepositorioMemoria<T> : IRepositorio<T> where T : EntidadeBase
{
    private readonly Dictionary<int, T> _dados = new();
    private int _proximoId = 1;

    public int ProximoId() => checked(_proximoId++);

    public void Adicionar(T entidade)
    {
        ArgumentNullException.ThrowIfNull(entidade);
        if (_dados.ContainsKey(entidade.Id))
            throw new InvalidOperationException($"Id {entidade.Id} já cadastrado em {typeof(T).Name}.");
        ValidarNovo(entidade); // Despacho polimórfico para a regra específica da subclasse.
        _dados.Add(entidade.Id, entidade);
        _proximoId = Math.Max(_proximoId, checked(entidade.Id + 1));
    }

    protected virtual void ValidarNovo(T entidade) { }

    public T? Buscar(int id) => _dados.GetValueOrDefault(id);

    public T Obter(int id) => Buscar(id)
        ?? throw new KeyNotFoundException($"{typeof(T).Name} #{id} não encontrado.");

    // Snapshot da estrutura: quem recebe não consegue adicionar/remover no repositório.
    // As entidades continuam compartilhadas para refletir as transições de negócio.
    public IReadOnlyList<T> Listar() => _dados.Values.OrderBy(e => e.Id).ToList().AsReadOnly();
}
