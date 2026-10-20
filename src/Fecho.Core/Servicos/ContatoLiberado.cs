namespace Fecho.Core.Servicos;

// Somente o método autorizado de TrocaService devolve este DTO.
public sealed record ContatoLiberado(int UsuarioId, string Nome, string Telefone);
