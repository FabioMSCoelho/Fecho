using Fecho.Core.Entidades;
using Fecho.Core.Enums;
using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;
using MySqlConnector;

internal static class CadastrosPersistentes
{
    internal static void Verificar(ConexaoMySql conexao, Action<bool, string> exigir,
        Action<Action, string, string> falha)
    {
        var prefixo = "Cadastro-" + Guid.NewGuid().ToString("N")[..10];
        var repoUsuarios = new RepositorioUsuariosMySql(conexao);
        var repoCategorias = new RepositorioCategoriasMySql(conexao);
        var repoObjetos = new RepositorioObjetosMySql(conexao);
        var usuarios = new UsuarioService(repoUsuarios);
        var categorias = new CategoriaService(repoCategorias);
        var objetos = new ObjetoService(repoObjetos, repoUsuarios, repoCategorias);
        var usuariosIds = new List<int>();
        var objetosIds = new List<int>();
        var categoriasIds = new List<int>();
        int? matchId = null;

        Usuario CriarUsuario(string sufixo)
        {
            var novo = usuarios.Cadastrar(prefixo + sufixo, prefixo + sufixo.Replace(' ', '-') + "@example.com", "Cadastro@2026",
                "11999990001", "São Paulo", "foto.png", "Descrição com acentuação");
            usuariosIds.Add(novo.Id); return novo;
        }
        Objeto CriarObjeto(int usuarioId, int categoriaId, string sufixo)
        {
            var novo = objetos.Cadastrar(usuarioId, categoriaId, prefixo + sufixo, EstadoConservacao.BOM,
                "Descrição ' ; DROP TABLE objetos; --", 1234.56m, "imagem.png");
            objetosIds.Add(novo.Id); return novo;
        }
        object? Sql(string sql, params (string Nome, object? Valor)[] parametros)
        {
            using var db = conexao.Abrir();
            using var cmd = new MySqlCommand(sql, db);
            foreach (var (nome, valor) in parametros) cmd.Parameters.AddWithValue(nome, valor ?? DBNull.Value);
            return cmd.ExecuteScalar();
        }
        void Invalido<T>(Action acao, string caso) where T : Exception
        {
            try { acao(); }
            catch (T) { exigir(true, caso); return; }
            throw new Exception("FALHOU (não rejeitou): " + caso);
        }
        try
        {
            var categoria = categorias.Cadastrar(prefixo + " categoria"); categoriasIds.Add(categoria.Id);
            var outraCategoria = categorias.Cadastrar(prefixo + " outra"); categoriasIds.Add(outraCategoria.Id);
            var usuario = CriarUsuario(" principal");
            var outro = CriarUsuario(" outro");
            exigir(usuario.Id > 0 && usuario.Cidade == "São Paulo" && usuario.Email == usuario.Email.ToLowerInvariant(),
                "Usuário é inserido com ID do banco, e-mail normalizado e campos opcionais");
            var hash = (string)Sql("SELECT senha FROM usuarios WHERE id=@id", ("@id", usuario.Id))!;
            exigir(hash.StartsWith("PBKDF2-SHA256$600000$", StringComparison.Ordinal) && !hash.Contains("Cadastro@2026"),
                "MySQL guarda hash PBKDF2, sem senha em texto puro");
            exigir(usuarios.Buscar(usuario.Id)?.ConferirSenha("Cadastro@2026") == true,
                "Senha do usuário reidratado confere após nova conexão");
            exigir(usuarios.Listar().Any(u => u.Id == usuario.Id), "Listagem recupera usuários persistidos");
            exigir(ProcessoPersistencia.ConsultaConfere(usuario.Id, usuario.Nome, "usuarios"),
                "Outro processo recupera o usuário cadastrado");
            falha(() => usuarios.Cadastrar("Duplicado", usuario.Email.ToUpperInvariant(), "Cadastro@2026", "11999990002"),
                "já existe um usuário", "E-mail duplicado é rejeitado pelo MySQL");
            falha(() => usuarios.Atualizar(outro.Id, "Duplicado", usuario.Email, "11999990002"),
                "já existe um usuário", "Atualização para e-mail duplicado é rejeitada");
            exigir(usuarios.Buscar(outro.Id)?.Email == outro.Email, "Atualização rejeitada preserva o cadastro anterior");
            Invalido<ArgumentException>(() => usuarios.Cadastrar("Inválido", "sem-email", "Cadastro@2026", "11999990002"),
                "E-mail inválido é rejeitado antes da persistência");
            Invalido<ArgumentException>(() => usuarios.Cadastrar("Inválido", prefixo + "@example.com", "curta", "11999990002"),
                "Senha curta é rejeitada antes da persistência");
            Invalido<ArgumentException>(() => usuarios.Atualizar(usuario.Id, "Inválido", usuario.Email, "123"),
                "Telefone inválido é rejeitado antes da atualização");

            var dataUsuario = new DateTime(2025, 2, 3, 4, 5, 6, DateTimeKind.Utc);
            Sql("UPDATE usuarios SET data_cadastro=@data WHERE id=@id", ("@data", dataUsuario), ("@id", usuario.Id));
            var nomeNovo = prefixo + "' usuário atualizado";
            exigir(usuarios.Atualizar(usuario.Id, nomeNovo, usuario.Email, "11999990003", ativo: false),
                "UPDATE de usuário altera perfil e situação");
            var atualizado = usuarios.Buscar(usuario.Id)!;
            exigir(atualizado.Nome == nomeNovo && !atualizado.Ativo && atualizado.Cidade is null
                && atualizado.Foto is null && atualizado.Descricao is null, "Consulta mantém situação inativa e campos NULL");
            exigir(atualizado.DataCadastro == dataUsuario && (string)Sql("SELECT senha FROM usuarios WHERE id=@id", ("@id", usuario.Id))! == hash,
                "Edição do perfil preserva data de cadastro e hash original");
            exigir((string)Sql("SELECT telefone FROM usuarios WHERE id=@id", ("@id", usuario.Id))! == "11999990003",
                "Telefone atualizado fica gravado no banco");
            exigir(usuarios.Atualizar(usuario.Id, nomeNovo, usuario.Email, "11999990003", ativo: false),
                "UPDATE idêntico de usuário reconhece registro existente");
            exigir(ProcessoPersistencia.ConsultaConfere(usuario.Id, nomeNovo, "usuarios"),
                "Outro processo recupera a edição do usuário");
            Invalido<InvalidOperationException>(() => objetos.Cadastrar(usuario.Id, categoria.Id, "Inválido", EstadoConservacao.BOM),
                "Usuário inativo não cadastra objetos");
            exigir(usuarios.AlterarSenha(usuario.Id, "NovaSenha@2026"), "Alteração de senha persiste novo hash");
            var comSenhaNova = usuarios.Buscar(usuario.Id)!;
            exigir(comSenhaNova.ConferirSenha("NovaSenha@2026") && !comSenhaNova.ConferirSenha("Cadastro@2026"),
                "Hash reidratado confere somente a senha nova");
            usuarios.Atualizar(usuario.Id, nomeNovo, usuario.Email, "11999990003");

            var objeto = CriarObjeto(usuario.Id, categoria.Id, " objeto");
            exigir(objeto.Id > 0 && objeto.Proprietario.Id == usuario.Id && objeto.Categoria.Id == categoria.Id
                && objeto.ValorEstimado == 1234.56m && objeto.Status == StatusObjeto.DISPONIVEL,
                "Objeto é inserido com IDs relacionados, valor decimal e status disponível");
            exigir(objetos.Listar().Any(o => o.Id == objeto.Id), "Listagem de objetos reconstrói proprietário e categoria por JOIN");
            exigir(objetos.Buscar(objeto.Id)?.Descricao == objeto.Descricao, "Descrição do objeto recebe SQL como texto literal");
            exigir(ProcessoPersistencia.ConsultaConfere(objeto.Id, objeto.Nome, "objetos"),
                "Outro processo recupera o objeto cadastrado");
            Invalido<ArgumentException>(() => objetos.Cadastrar(int.MaxValue, categoria.Id, "Inválido", EstadoConservacao.BOM),
                "Cadastro rejeita proprietário inexistente");
            Invalido<ArgumentException>(() => objetos.Cadastrar(usuario.Id, int.MaxValue, "Inválido", EstadoConservacao.BOM),
                "Cadastro rejeita categoria inexistente");
            Invalido<ArgumentException>(() => objetos.Cadastrar(usuario.Id, categoria.Id, "Inválido", (EstadoConservacao)99),
                "Cadastro rejeita enum de conservação desconhecido");
            Invalido<ArgumentException>(() => objetos.Cadastrar(usuario.Id, categoria.Id, "Inválido", EstadoConservacao.BOM, valorEstimado: 1.001m),
                "Objeto rejeita valor com mais de duas casas decimais");
            falha(() => repoObjetos.Inserir(new Objeto(1, usuario, new Categoria(int.MaxValue, "Ausente"), "Inválido", EstadoConservacao.BOM)),
                "referenciado não existe", "Chave estrangeira protege IDs inválidos mesmo no repositório");

            var dataObjeto = new DateTime(2025, 3, 4, 5, 6, 7, DateTimeKind.Utc);
            Sql("UPDATE objetos SET data_cadastro=@data, status='TROCADO' WHERE id=@id", ("@data", dataObjeto), ("@id", objeto.Id));
            var nomeObjetoNovo = prefixo + " objeto atualizado";
            exigir(objetos.Atualizar(objeto.Id, outraCategoria.Id, nomeObjetoNovo, EstadoConservacao.MUITO_BOM),
                "UPDATE do objeto altera categoria, nome e conservação");
            var objetoAtualizado = objetos.Buscar(objeto.Id)!;
            exigir(objetoAtualizado.Categoria.Id == outraCategoria.Id && objetoAtualizado.EstadoConservacao == EstadoConservacao.MUITO_BOM
                && objetoAtualizado.Descricao is null && objetoAtualizado.Imagem is null && objetoAtualizado.ValorEstimado is null,
                "Objeto atualizado preserva DECIMAL/NULL e suas relações");
            exigir(objetoAtualizado.Proprietario.Id == usuario.Id && objetoAtualizado.Status == StatusObjeto.TROCADO
                && objetoAtualizado.DataCadastro == dataObjeto, "Edição preserva proprietário, status e data originais do objeto");
            exigir(objetos.Atualizar(objeto.Id, outraCategoria.Id, nomeObjetoNovo, EstadoConservacao.MUITO_BOM),
                "UPDATE idêntico de objeto reconhece registro existente");
            exigir(ProcessoPersistencia.ConsultaConfere(objeto.Id, nomeObjetoNovo, "objetos"),
                "Outro processo recupera o objeto atualizado");
            Invalido<ArgumentException>(() => objetos.Atualizar(objeto.Id, categoria.Id, new string('x', 101), EstadoConservacao.BOM),
                "Nome de objeto excessivo é rejeitado sem alterar registro");
            falha(() => usuarios.Excluir(usuario.Id), "vinculado", "FK impede excluir usuário proprietário de objeto");
            falha(() => categorias.Excluir(outraCategoria.Id), "vinculada", "FK impede excluir categoria usada pelo objeto");

            var segundoObjeto = CriarObjeto(outro.Id, categoria.Id, " segundo objeto");
            Sql("INSERT INTO matches(usuario1_id,usuario2_id,objeto1_id,objeto2_id) VALUES (@u1,@u2,@o1,@o2)",
                ("@u1", usuario.Id), ("@u2", outro.Id), ("@o1", objeto.Id), ("@o2", segundoObjeto.Id));
            matchId = Convert.ToInt32(Sql("SELECT id FROM matches WHERE objeto1_id=@id", ("@id", objeto.Id)));
            falha(() => objetos.Excluir(objeto.Id), "vinculado", "FK impede excluir objeto associado a match");
            exigir(objetos.Buscar(objeto.Id) is not null, "Exclusão rejeitada preserva objeto");
            Sql("DELETE FROM matches WHERE id=@id", ("@id", matchId)); matchId = null;
            exigir(objetos.Excluir(objeto.Id) && objetos.Buscar(objeto.Id) is null, "DELETE remove objeto sem matches");
            exigir(ProcessoPersistencia.ConsultaConfere(objeto.Id, null, "objetos"), "Outro processo confirma exclusão do objeto");
            exigir(!objetos.Excluir(objeto.Id) && !objetos.Atualizar(objeto.Id, categoria.Id, "Ausente", EstadoConservacao.BOM),
                "CRUD de objeto ausente retorna false");
            exigir(usuarios.Excluir(usuario.Id) && usuarios.Buscar(usuario.Id) is null, "DELETE remove usuário sem vínculos impeditivos");
            exigir(ProcessoPersistencia.ConsultaConfere(usuario.Id, null, "usuarios"), "Outro processo confirma exclusão do usuário");
            exigir(!usuarios.Excluir(usuario.Id) && !usuarios.AlterarSenha(usuario.Id, "Ausente@2026")
                && !usuarios.Atualizar(usuario.Id, "Ausente", usuario.Email, "11999990001"), "CRUD de usuário ausente retorna false");
        }
        finally
        {
            if (matchId.HasValue) Sql("DELETE FROM matches WHERE id=@id", ("@id", matchId));
            foreach (var id in objetosIds) objetos.Excluir(id);
            foreach (var id in usuariosIds) usuarios.Excluir(id);
            foreach (var id in categoriasIds) categorias.Excluir(id);
        }
    }
}
