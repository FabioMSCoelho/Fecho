using Fecho.Core.Servicos;
using Fecho.Infraestrutura.MySql;

namespace Fecho.ConsoleApp;

public static class MenuCrud
{
    public static int Executar()
    {
        try
        {
            var conexao = ConexaoMySql.DoAmbiente();
            conexao.Verificar();
            var usuarios = new RepositorioUsuariosMySql(conexao);
            var categorias = new RepositorioCategoriasMySql(conexao);
            var objetos = new RepositorioObjetosMySql(conexao);
            var usuariosService = new UsuarioService(usuarios);
            var categoriasService = new CategoriaService(categorias);
            var objetosService = new ObjetoService(objetos, usuarios, categorias);
            Console.WriteLine("FECHÔ — cadastros conectados ao MySQL");
            while (true)
            {
                Console.WriteLine("\n1 Categorias | 2 Usuários | 3 Objetos | 0 Sair");
                Console.Write("Cadastro: ");
                switch (Console.ReadLine())
                {
                    case null or "0": return 0;
                    case "1": MenuCategorias.Executar(categoriasService); break;
                    case "2": MenuUsuarios.Executar(usuariosService); break;
                    case "3": MenuObjetos.Executar(objetosService); break;
                    default: Console.WriteLine("Opção inválida."); break;
                }
            }
        }
        catch (ErroPersistencia ex) { Console.Error.WriteLine(ex.Message); return 1; }
    }
}
