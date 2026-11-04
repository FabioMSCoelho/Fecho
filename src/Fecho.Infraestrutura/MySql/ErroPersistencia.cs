namespace Fecho.Infraestrutura.MySql;

// Mensagens seguras para o usuário; nunca incluem credenciais ou SQL.
public sealed class ErroPersistencia(string mensagem) : Exception(mensagem);
