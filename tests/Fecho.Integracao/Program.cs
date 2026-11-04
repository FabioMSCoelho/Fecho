using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using MySqlConnector;

// Execute apenas em banco de teste. As fixtures têm nomes únicos e são removidas no finally.
var conexao = ConexaoMySql.DoAmbiente();
conexao.Verificar();
var app = new CategoriaService(new RepositorioCategoriasMySql(conexao));
var prefixo = "Teste-" + Guid.NewGuid().ToString("N")[..12];
var ids = new List<int>();
int? usuarioId = null, objetoId = null;
var aprovados = 0;
void Exigir(bool condicao, string caso)
{
    if (!condicao) throw new Exception("FALHOU: " + caso);
    Console.WriteLine("OK " + caso); aprovados++;
}
void Falha(Action acao, string trecho, string caso)
{
    try { acao(); }
    catch (ErroPersistencia ex) { Exigir(ex.Message.Contains(trecho, StringComparison.OrdinalIgnoreCase), caso); return; }
    throw new Exception("FALHOU (não rejeitou): " + caso);
}
try
{
    var c = app.Cadastrar(prefixo, "Descrição com acentuação"); ids.Add(c.Id);
    Exigir(c.Id > 0, "INSERT gera ID no MySQL");
    Exigir(app.Buscar(c.Id)?.Descricao == "Descrição com acentuação", "Busca por ID recupera texto UTF-8");
    Exigir(app.Listar().Any(x => x.Id == c.Id), "Listagem lê o registro inserido");
    var outraInstancia = new CategoriaService(new RepositorioCategoriasMySql(ConexaoMySql.DoAmbiente()));
    Exigir(outraInstancia.Buscar(c.Id)?.Nome == prefixo, "Nova conexão recupera o registro persistido");
    Falha(() => app.Cadastrar(prefixo.ToUpperInvariant()), "já existe", "Nome duplicado respeita collation do MySQL");
    Exigir(app.Atualizar(c.Id, prefixo + " atualizado", null), "UPDATE altera registro");
    Exigir(outraInstancia.Buscar(c.Id) is { Descricao: null } atualizado && atualizado.Nome.EndsWith(" atualizado"),
        "Nova conexão lê atualização e descrição NULL");
    Exigir(app.Atualizar(c.Id, prefixo + " atualizado", null), "UPDATE idêntico reconhece registro existente");
    var segunda = app.Cadastrar(prefixo + " segunda"); ids.Add(segunda.Id);
    Falha(() => app.Atualizar(segunda.Id, prefixo + " atualizado"), "já existe", "UPDATE duplicado é rejeitado");
    Exigir(app.Buscar(segunda.Id)?.Nome == segunda.Nome, "UPDATE rejeitado preserva registro anterior");
    var literal = app.Cadastrar(prefixo + "' OR 1=1; --", "Texto ' ; DROP TABLE categorias; --"); ids.Add(literal.Id);
    Exigir(app.Buscar(literal.Id)?.Nome == literal.Nome && app.Buscar(c.Id) is not null, "SQL recebido como dado literal");
    try { app.Cadastrar(" "); throw new Exception("Validação não executou"); }
    catch (ArgumentException) { Exigir(true, "Nome vazio é rejeitado pela entidade"); }
    try { app.Atualizar(c.Id, new string('x', 51)); throw new Exception("Validação não executou"); }
    catch (ArgumentException) { Exigir(true, "Limite de tamanho é validado"); }

    using (var db = conexao.Abrir())
    {
        using var usuario = new MySqlCommand("INSERT INTO usuarios(nome,email,senha,telefone) VALUES (@nome,@email,'fixture-sem-login','11999990000')", db);
        usuario.Parameters.AddWithValue("@nome", prefixo);
        usuario.Parameters.AddWithValue("@email", prefixo + "@example.com");
        usuario.ExecuteNonQuery(); usuarioId = checked((int)usuario.LastInsertedId);
        using var objeto = new MySqlCommand("INSERT INTO objetos(usuario_id,categoria_id,nome,estado_conservacao) VALUES (@u,@c,'Fixture','BOM')", db);
        objeto.Parameters.AddWithValue("@u", usuarioId); objeto.Parameters.AddWithValue("@c", c.Id);
        objeto.ExecuteNonQuery(); objetoId = checked((int)objeto.LastInsertedId);
    }
    Falha(() => app.Excluir(c.Id), "vinculada", "DELETE protege categoria referenciada por FK");
    Exigir(app.Buscar(c.Id) is not null, "DELETE rejeitado preserva categoria");
    Exigir(app.Excluir(segunda.Id), "DELETE remove registro sem vínculos");
    Exigir(outraInstancia.Buscar(segunda.Id) is null, "Nova conexão confirma exclusão persistida");
    Exigir(!app.Excluir(segunda.Id) && !app.Atualizar(segunda.Id, "Ausente"), "Operações em ID ausente retornam false");
    using var existente = conexao.Abrir();
    var opcoes = new MySqlConnectionStringBuilder(existente.ConnectionString)
    { Password = Guid.NewGuid().ToString("N"), Pooling = false };
    Falha(() => new ConexaoMySql(opcoes.ConnectionString).Verificar(), "acesso negado", "Credenciais inválidas têm mensagem segura");
    opcoes.Server = "127.0.0.1"; opcoes.Port = 1; opcoes.ConnectionTimeout = 1;
    Falha(() => new ConexaoMySql(opcoes.ConnectionString).Verificar(), "confira servidor", "Servidor indisponível tem mensagem segura");
    Console.WriteLine($"{aprovados}/{aprovados} verificações de integração aprovadas.");
}
catch (Exception ex)
{
    Console.Error.WriteLine(ex is ErroPersistencia ? ex.Message : ex.ToString());
    Environment.ExitCode = 1;
}
finally
{
    using var db = conexao.Abrir();
    foreach (var (tabela, id) in new[] { ("objetos", objetoId), ("usuarios", usuarioId) })
    {
        if (id is null) continue;
        using var cmd = new MySqlCommand($"DELETE FROM {tabela} WHERE id=@id", db);
        cmd.Parameters.AddWithValue("@id", id); cmd.ExecuteNonQuery();
    }
    foreach (var id in ids) app.Excluir(id);
}
