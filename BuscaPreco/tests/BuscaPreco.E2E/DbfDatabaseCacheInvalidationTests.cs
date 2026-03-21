using Xunit;
using System.Text;
using BuscaPreco.CrossCutting;
using BuscaPreco.Infrastructure.Data;
using Serilog;

namespace BuscaPreco.E2E;

public class DbfDatabaseCacheInvalidationTests
{
    [Fact]
    public void Deve_InvalidarCache_Quando_ArquivoDbfForAlterado()
    {
        Log.Logger = new LoggerConfiguration().CreateLogger();

        var sourceDbfPath = CriarDbfTemporario();
        var database = new DbfDatabase(sourceDbfPath, new Logger());

        var antes = database.BuscarPorCodigo("20001");
        Assert.Equal("PRODUTO TESTE E2E", antes.des);
        Assert.Equal(12.34m, antes.vlrVenda1);

        // Setup crítico: altera bytes em posição textual do registro DBF e atualiza timestamp.
        const string textoOriginal = "PRODUTO TESTE E2E";
        var textoAtualizado = "PRODUTO ALTERADO".PadRight(textoOriginal.Length);
        SubstituirTextoNoArquivo(sourceDbfPath, textoOriginal, textoAtualizado);
        Thread.Sleep(1200);
        File.SetLastWriteTime(sourceDbfPath, DateTime.Now.AddSeconds(1));

        var depois = database.BuscarPorCodigo("20001");
        Assert.Equal("PRODUTO ALTERADO", depois.des.Trim());
        Assert.Equal(12.34m, depois.vlrVenda1);
    }

    private static string CriarDbfTemporario()
    {
        var fixturesDir = Path.Combine(AppContext.BaseDirectory, "Fixtures");
        var base64Path = Path.Combine(fixturesDir, "produtos.dbf.base64.txt");

        var tempDir = Path.Combine(Path.GetTempPath(), "buscapreco-tests", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(tempDir);

        var dbfPath = Path.Combine(tempDir, $"produtos-{Guid.NewGuid():N}.dbf");
        var bytes = Convert.FromBase64String(File.ReadAllText(base64Path).Trim());
        File.WriteAllBytes(dbfPath, bytes);
        return dbfPath;
    }

    private static void SubstituirTextoNoArquivo(string filePath, string original, string novo)
    {
        if (original.Length != novo.Length)
        {
            throw new ArgumentException("Textos precisam ter o mesmo tamanho para manter layout do DBF.");
        }

        var bytes = File.ReadAllBytes(filePath);
        var source = Encoding.ASCII.GetBytes(original);
        var target = Encoding.ASCII.GetBytes(novo);

        var index = IndexOf(bytes, source);
        Assert.True(index >= 0, "Texto original não encontrado no fixture DBF.");

        Buffer.BlockCopy(target, 0, bytes, index, target.Length);
        File.WriteAllBytes(filePath, bytes);
    }

    private static int IndexOf(byte[] source, byte[] pattern)
    {
        for (var i = 0; i <= source.Length - pattern.Length; i++)
        {
            var matched = true;
            for (var j = 0; j < pattern.Length; j++)
            {
                if (source[i + j] != pattern[j])
                {
                    matched = false;
                    break;
                }
            }

            if (matched)
            {
                return i;
            }
        }

        return -1;
    }
}
