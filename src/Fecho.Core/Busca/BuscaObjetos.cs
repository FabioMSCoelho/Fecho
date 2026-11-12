using Fecho.Core.Compatibilidade;
using Fecho.Core.Entidades;
using Fecho.Core.Enums;

namespace Fecho.Core.Busca;

public enum OrdemObjetos { Recentes, Nome, MenorValor, MaiorValor }

public sealed record FiltroObjetos(string? Texto = null, int? CategoriaId = null,
    EstadoConservacao? Estado = null, string? Cidade = null, decimal? ValorMaximo = null,
    OrdemObjetos Ordem = OrdemObjetos.Recentes, int Pagina = 1, bool ParaMim = false);

public sealed record ResultadoBusca(IReadOnlyList<Objeto> Itens, int Total, int Pagina, int TotalPaginas,
    IReadOnlyDictionary<int, int> QuantidadesPorCategoria);

/// <summary>Busca linear, deduplicação por HashSet e ordenação estável por intercalação.</summary>
public sealed class BuscaObjetos
{
    public const int TamanhoPagina = 9;

    public ResultadoBusca Executar(IEnumerable<Objeto> origem, FiltroObjetos filtro, int? usuarioId = null,
        IReadOnlyList<Desejo>? desejos = null, IReadOnlySet<int>? ignorados = null)
    {
        if (filtro.Texto?.Length > 150 || filtro.Cidade?.Length > 100)
            throw new ArgumentException("Busca ou cidade excede o tamanho permitido.");
        if (filtro.ValorMaximo is < 0 or > 99_999_999.99m)
            throw new ArgumentException("Informe um valor máximo válido.");
        Validacao.EnumValido(filtro.Ordem);
        if (filtro.Estado.HasValue) Validacao.EnumValido(filtro.Estado.Value);
        var palavras = TextoNormalizado.De(filtro.Texto?.Trim() ?? "")
            .Split(' ', StringSplitOptions.RemoveEmptyEntries);
        var cidade = TextoNormalizado.De(filtro.Cidade?.Trim() ?? "");
        var encontrados = new List<Objeto>();
        var ids = new HashSet<int>();
        var quantidades = new Dictionary<int, int>();
        var criterio = new PorPalavrasChave();
        foreach (var objeto in origem)
        {
            if (objeto.Status != StatusObjeto.DISPONIVEL || !objeto.Proprietario.Ativo
                || objeto.Proprietario.Id == usuarioId || ignorados?.Contains(objeto.Id) == true || !ids.Add(objeto.Id)) continue;
            if (filtro.Estado.HasValue && objeto.EstadoConservacao != filtro.Estado.Value) continue;
            if (filtro.ValorMaximo.HasValue && (!objeto.ValorEstimado.HasValue || objeto.ValorEstimado > filtro.ValorMaximo)) continue;
            if (!TextoNormalizado.De(objeto.Proprietario.Cidade ?? "").Contains(cidade, StringComparison.Ordinal)) continue;
            var texto = TextoNormalizado.De($"{objeto.Id} {objeto.Nome} {objeto.Descricao} {objeto.Categoria.Nome}");
            if (!palavras.All(p => texto.Contains(p, StringComparison.Ordinal))) continue;
            if (filtro.ParaMim && (desejos is null || !desejos.Any(d => criterio.Compativel(objeto, d)))) continue;
            quantidades[objeto.Categoria.Id] = quantidades.GetValueOrDefault(objeto.Categoria.Id) + 1;
            if (filtro.CategoriaId.HasValue && objeto.Categoria.Id != filtro.CategoriaId) continue;
            encontrados.Add(objeto);
        }
        var ordenados = encontrados.ToArray();
        var auxiliar = new Objeto[ordenados.Length];
        Comparer<Objeto> comparador = Comparer<Objeto>.Create((a, b) => Comparar(a, b, filtro.Ordem));
        Intercalar(ordenados, auxiliar, 0, ordenados.Length, comparador);
        var paginas = Math.Max(1, (ordenados.Length + TamanhoPagina - 1) / TamanhoPagina);
        var pagina = Math.Clamp(filtro.Pagina, 1, paginas);
        return new ResultadoBusca(ordenados.Skip((pagina - 1) * TamanhoPagina).Take(TamanhoPagina).ToList().AsReadOnly(),
            ordenados.Length, pagina, paginas, new System.Collections.ObjectModel.ReadOnlyDictionary<int, int>(quantidades));
    }

    private static int Comparar(Objeto a, Objeto b, OrdemObjetos ordem)
    {
        // Ausência de estimativa vem por último em ambas as ordenações por valor.
        var resultado = ordem switch
        {
            OrdemObjetos.Nome => string.Compare(TextoNormalizado.De(a.Nome), TextoNormalizado.De(b.Nome), StringComparison.Ordinal),
            OrdemObjetos.MenorValor => Nullable.Compare(a.ValorEstimado, b.ValorEstimado),
            OrdemObjetos.MaiorValor => Nullable.Compare(b.ValorEstimado, a.ValorEstimado),
            _ => b.DataCadastro.CompareTo(a.DataCadastro)
        };
        if (ordem is OrdemObjetos.MenorValor or OrdemObjetos.MaiorValor && a.ValorEstimado.HasValue != b.ValorEstimado.HasValue)
            resultado = a.ValorEstimado.HasValue ? -1 : 1;
        return resultado != 0 ? resultado : a.Id.CompareTo(b.Id);
    }

    private static void Intercalar(Objeto[] itens, Objeto[] auxiliar, int inicio, int fim, IComparer<Objeto> comparador)
    {
        if (fim - inicio < 2) return;
        var meio = inicio + (fim - inicio) / 2;
        Intercalar(itens, auxiliar, inicio, meio, comparador);
        Intercalar(itens, auxiliar, meio, fim, comparador);
        int esquerda = inicio, direita = meio;
        for (var i = inicio; i < fim; i++)
            auxiliar[i] = direita >= fim || esquerda < meio && comparador.Compare(itens[esquerda], itens[direita]) <= 0
                ? itens[esquerda++] : itens[direita++];
        Array.Copy(auxiliar, inicio, itens, inicio, fim - inicio);
    }
}
