using Xunit;

namespace BuscaPreco.E2E;

/// <summary>
/// Testes E2E do fluxo completo usando o GertecTerminalSimulator.
/// Cada teste levanta um servidor real na porta TCP e um simulador de terminal.
/// </summary>
public class TerminalSimuladorTests : IAsyncLifetime
{
    private ServidorDeTestesContext _ctx = null!;

    public async Task InitializeAsync()
        => _ctx = await ServidorDeTestesContext.CriarAsync();

    public async Task DisposeAsync()
        => await _ctx.DisposeAsync();

    // ── CT-01: Handshake ──────────────────────────────────────────────────────

    [Fact]
    public async Task Deve_ConectarComSucesso_Quando_TerminalRealizarHandshake()
    {
        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);

        await terminal.ConectarAsync();

        Assert.True(terminal.Conectado);
    }

    // ── CT-02: Produto encontrado ─────────────────────────────────────────────

    [Fact]
    public async Task Deve_RetornarDescricaoEPreco_Quando_CodigoExistirNoDbf()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();
        var (descricaoEsperada, precoEsperado) = _ctx.BuscaPrecosService.BuscarPorCodigo(produto.CodigoItem);

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        var resposta = await terminal.ConsultarPrecoAsync(produto.CodigoItem);

        Assert.True(resposta.Encontrado);
        Assert.False(string.IsNullOrWhiteSpace(resposta.Descricao));
        Assert.Equal(descricaoEsperada.Trim(), resposta.Descricao!.Trim());
        Assert.Equal(precoEsperado.ToString("0.00", System.Globalization.CultureInfo.InvariantCulture), resposta.Preco);
    }

    // ── CT-03: Produto não encontrado ─────────────────────────────────────────

    [Fact]
    public async Task Deve_RetornarNfound_Quando_CodigoNaoExistirNoDbf()
    {
        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        var resposta = await terminal.ConsultarPrecoAsync("CODIGO_INEXISTENTE_99999");

        Assert.False(resposta.Encontrado);
        Assert.Equal("#nfound", resposta.RespostaBruta);
        Assert.Null(resposta.Descricao);
        Assert.Null(resposta.Preco);
    }

    // ── CT-04: Múltiplas consultas na mesma conexão ───────────────────────────

    [Fact]
    public async Task Deve_ResponderMultiplasConsultas_Na_MesmaConexao()
    {
        var produtos = _ctx.BuscaPrecosService.ListarTudo().Take(3).ToList();
        Assert.NotEmpty(produtos);

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        foreach (var produto in produtos)
        {
            var resposta = await terminal.ConsultarPrecoAsync(produto.CodigoItem);
            Assert.True(resposta.Encontrado, $"Produto '{produto.CodigoItem}' deveria ser encontrado.");
        }
    }

    // ── CT-05: Consulta intercalada encontrado / não encontrado ───────────────

    [Fact]
    public async Task Deve_AlternarEncontradoENaoEncontrado_Na_MesmaConexao()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        var encontrado = await terminal.ConsultarPrecoAsync(produto.CodigoItem);
        var naoEncontrado = await terminal.ConsultarPrecoAsync("INEXISTENTE_123");
        var encontrado2 = await terminal.ConsultarPrecoAsync(produto.CodigoItem);

        Assert.True(encontrado.Encontrado);
        Assert.False(naoEncontrado.Encontrado);
        Assert.True(encontrado2.Encontrado);
        Assert.Equal(encontrado.Preco, encontrado2.Preco);
    }

    // ── CT-06: Terminal G2 S identificado pelo firmware ───────────────────────

    [Fact]
    public async Task Deve_ConectarEConsultar_Quando_TerminalForG2SComFirmware3()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta)
        {
            Tipo = "tc406",
            Versao = "3.3.1 S"
        };
        await terminal.ConectarAsync();

        var resposta = await terminal.ConsultarPrecoAsync(produto.CodigoItem);

        Assert.True(resposta.Encontrado);
    }

    // ── CT-07: Múltiplos terminais simultâneos ────────────────────────────────

    [Fact]
    public async Task Deve_AtenderMultiplosTerminaisSimultaneos_RetornandoMesmosPrecos()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var t1 = new GertecTerminalSimulator(_ctx.Porta);
        await using var t2 = new GertecTerminalSimulator(_ctx.Porta);

        await t1.ConectarAsync();
        await t2.ConectarAsync();

        Assert.True(t1.Conectado);
        Assert.True(t2.Conectado);

        var tarefaT1 = t1.ConsultarPrecoAsync(produto.CodigoItem);
        var tarefaT2 = t2.ConsultarPrecoAsync(produto.CodigoItem);

        var (r1, r2) = (await tarefaT1, await tarefaT2);

        Assert.True(r1.Encontrado);
        Assert.True(r2.Encontrado);
        Assert.Equal(r1.Preco, r2.Preco);
    }

    // ── CT-08: Código com prefixo '#' enviado pelo simulador ──────────────────

    [Fact]
    public async Task Deve_IgnorarPrefixoHash_Quando_CodigoForEnviadoComHash()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        // ConsultarPrecoAsync internally prepends '#' — the server strips it
        var semHash = await terminal.ConsultarPrecoAsync(produto.CodigoItem);
        Assert.True(semHash.Encontrado);
    }

    // ── CT-09: Resposta dentro do timeout padrão ──────────────────────────────

    [Fact]
    public async Task Deve_ResponderDentroDoTimeout_Quando_ServidorEstiverOperacional()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        var sw = System.Diagnostics.Stopwatch.StartNew();
        var resposta = await terminal.ConsultarPrecoAsync(produto.CodigoItem, TimeSpan.FromSeconds(3));
        sw.Stop();

        Assert.True(resposta.Encontrado);
        Assert.True(sw.Elapsed < TimeSpan.FromSeconds(3),
            $"Resposta demorou {sw.Elapsed.TotalMilliseconds:0}ms, esperado < 3000ms.");
    }

    // ── CT-10: Formato da resposta — separador '|' ────────────────────────────

    [Fact]
    public async Task Deve_RetornarRespostaNoFormatoCorreto_HashDescricaoPipePrecso()
    {
        var produto = _ctx.BuscaPrecosService.ListarTudo().First();

        await using var terminal = new GertecTerminalSimulator(_ctx.Porta);
        await terminal.ConectarAsync();

        var resposta = await terminal.ConsultarPrecoAsync(produto.CodigoItem);

        Assert.True(resposta.Encontrado);
        Assert.StartsWith("#", resposta.RespostaBruta);
        Assert.Contains("|", resposta.RespostaBruta);
        // Formato: #<descricao>|<preco>  — preco com ponto decimal (InvariantCulture)
        Assert.Matches(@"^#.+\|\d+\.\d{2}$", resposta.RespostaBruta);
    }
}
