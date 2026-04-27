using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

// ────────────────────────────────────────────────────────────────────────────
// BuscaPreço — Simulador de Terminal Gertec (ferramenta de testes manual)
//
// Uso:
//   dotnet run -- --host 127.0.0.1 --porta 9000
//   dotnet run -- --host 192.168.1.10 --porta 9000 --tipo tc406 --versao "3.3.1 S"
//
// Após conectar, digite códigos de barras para consultar preços.
// Digite "sair" ou pressione Ctrl+C para encerrar.
// ────────────────────────────────────────────────────────────────────────────

string host = "127.0.0.1";
int porta = 9000;
string tipo = "tc406";
string versao = "3.3.1 S";

for (int i = 0; i < args.Length - 1; i++)
{
    switch (args[i].ToLowerInvariant())
    {
        case "--host":   host   = args[i + 1]; break;
        case "--porta":  int.TryParse(args[i + 1], out porta); break;
        case "--tipo":   tipo   = args[i + 1]; break;
        case "--versao": versao = args[i + 1]; break;
    }
}

Console.Title = "BuscaPreço — Simulador Gertec";
Cabecalho();
Console.WriteLine($"  Conectando em {host}:{porta}  (tipo={tipo} versao={versao})");
Console.WriteLine();

using var cts = new CancellationTokenSource();
Console.CancelKeyPress += (_, e) => { e.Cancel = true; cts.Cancel(); };

await using var sim = new Simulador(host, porta, tipo, versao);

try
{
    await sim.ConectarAsync(cts.Token);
    Cor(ConsoleColor.Green, $"  Conectado! Terminal pronto.");
    Console.WriteLine();
    Console.WriteLine("  Digite um código de barras e pressione ENTER para consultar.");
    Console.WriteLine("  Digite \"sair\" para encerrar.\n");

    while (!cts.IsCancellationRequested)
    {
        Cor(ConsoleColor.Cyan, "  > ");
        var linha = await LerLinhaAsync(cts.Token);

        if (linha is null || linha.Equals("sair", StringComparison.OrdinalIgnoreCase))
            break;

        if (string.IsNullOrWhiteSpace(linha))
            continue;

        try
        {
            var r = await sim.ConsultarAsync(linha.Trim(), cts.Token);

            if (r.Encontrado)
            {
                Cor(ConsoleColor.Green,   $"  ENCONTRADO   {r.Descricao,-24}  R$ {r.Preco}");
            }
            else
            {
                Cor(ConsoleColor.Yellow, "  NÃO CADASTRADO");
            }
        }
        catch (TimeoutException)
        {
            Cor(ConsoleColor.Red, "  TIMEOUT — sem resposta do servidor.");
        }

        Console.WriteLine();
    }
}
catch (OperationCanceledException) { /* Ctrl+C */ }
catch (Exception ex)
{
    Cor(ConsoleColor.Red, $"\n  ERRO: {ex.Message}");
    return 1;
}

Console.WriteLine("\n  Encerrando...");
return 0;

// ── Helpers ──────────────────────────────────────────────────────────────────

static void Cabecalho()
{
    Console.Clear();
    Console.ForegroundColor = ConsoleColor.White;
    Console.WriteLine("╔══════════════════════════════════════╗");
    Console.WriteLine("║   BuscaPreço — Simulador Gertec G2   ║");
    Console.WriteLine("╚══════════════════════════════════════╝");
    Console.ResetColor();
    Console.WriteLine();
}

static void Cor(ConsoleColor cor, string texto)
{
    Console.ForegroundColor = cor;
    Console.WriteLine(texto);
    Console.ResetColor();
}

static async Task<string?> LerLinhaAsync(CancellationToken ct)
{
    // Console.ReadLine() bloqueia — executa em thread pool para respeitar o CancellationToken.
    return await Task.Run(() =>
    {
        try { return Console.ReadLine(); }
        catch { return null; }
    }, ct);
}

// ── Simulador interno ─────────────────────────────────────────────────────────

sealed class Simulador : IAsyncDisposable
{
    private readonly string _host;
    private readonly int _porta;
    private readonly string _tipo;
    private readonly string _versao;

    private TcpClient? _tcp;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _lock = new(1, 1);
    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;

    private readonly Channel<string> _respostas = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    public Simulador(string host, int porta, string tipo, string versao)
    {
        _host = host;
        _porta = porta;
        _tipo = tipo;
        _versao = versao;
    }

    public async Task ConectarAsync(CancellationToken ct = default)
    {
        _tcp = new TcpClient();
        await _tcp.ConnectAsync(_host, _porta, ct);
        _stream = _tcp.GetStream();

        await HandshakeAsync(ct);
        IniciarLeitor();
    }

    public async Task<(bool Encontrado, string? Descricao, string? Preco)> ConsultarAsync(
        string codigo, CancellationToken ct = default)
    {
        await EnviarAsync($"#{codigo}", ct);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(TimeSpan.FromSeconds(5));

        try
        {
            var raw = await _respostas.Reader.ReadAsync(linked.Token);
            return Interpretar(raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException($"Timeout para código '{codigo}'.");
        }
    }

    private async Task HandshakeAsync(CancellationToken ct)
    {
        var ok = await LerAsync(TimeSpan.FromSeconds(5), ct);
        if (ok != "#ok") throw new Exception($"Handshake: esperado '#ok', recebido '{ok}'.");

        await EnviarAsync($"#{_tipo}|{_versao}", ct);

        var msg = await LerAsync(TimeSpan.FromSeconds(5), ct);
        if (msg == "#alwayslive")
        {
            await EnviarAsync("#alwayslive_ok", ct);
            msg = await LerAsync(TimeSpan.FromSeconds(6), ct);
        }

        if (msg == "#config02?")
        {
            await EnviarAsync(Config02(), ct);
            msg = await LerAsync(TimeSpan.FromSeconds(6), ct);
        }

        if (msg == "#paramconfig?")
        {
            await EnviarAsync(Paramconfig(), ct);
            msg = await LerAsync(TimeSpan.FromSeconds(6), ct);
        }

        if (msg == "#updconfig?")
            await EnviarAsync(Updconfig(), ct);
    }

    private void IniciarLeitor()
    {
        _readerCts = new CancellationTokenSource();
        var token = _readerCts.Token;
        _readerTask = Task.Run(async () =>
        {
            var buf = new byte[4096];
            try
            {
                while (!token.IsCancellationRequested)
                {
                    int n = await _stream!.ReadAsync(buf, token);
                    if (n == 0) break;
                    var msg = Encoding.ASCII.GetString(buf, 0, n).Trim('\0', '\r', '\n', ' ');
                    if (msg == "#live?") { await EnviarAsync("#live", token); continue; }
                    if (!string.IsNullOrWhiteSpace(msg)) await _respostas.Writer.WriteAsync(msg, token);
                }
            }
            catch (OperationCanceledException) { }
            catch { /* conexão encerrada */ }
            finally { _respostas.Writer.TryComplete(); }
        }, token);
    }

    private async Task EnviarAsync(string msg, CancellationToken ct)
    {
        await _lock.WaitAsync(ct);
        try { await _stream!.WriteAsync(Encoding.ASCII.GetBytes(msg), ct); }
        finally { _lock.Release(); }
    }

    private async Task<string> LerAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);
        var buf = new byte[512];
        int n = await _stream!.ReadAsync(buf, linked.Token);
        return n == 0 ? "" : Encoding.ASCII.GetString(buf, 0, n).Trim('\0', '\r', '\n', ' ');
    }

    private static (bool, string?, string?) Interpretar(string raw)
    {
        if (raw == "#nfound") return (false, null, null);
        if (raw.StartsWith('#') && raw.Contains('|'))
        {
            var body = raw[1..];
            var sep = body.IndexOf('|');
            return (true, body[..sep], body[(sep + 1)..]);
        }
        return (false, null, null);
    }

    // ── Builders de resposta do protocolo ────────────────────────────────────

    private static string F(string v) => ((char)(v.Length + 48)).ToString() + v;

    private string Config02()
        => "#config02"
         + F("192.168.1.1") + F("192.168.1.10") + F("255.255.255.0")
         + F("SIMULADOR") + F("GERTEC") + F(" ") + F(" ")
         + (char)(3 + 48);

    private static string Paramconfig()
        => "#paramconfig" + (char)(0 + 48) + (char)(0 + 48);

    private string Updconfig()
        => "#updconfig" + F("192.168.1.254") + F(" ") + F("SIMULADOR-01") + F(" ") + F(" ") + F(" ");

    public async ValueTask DisposeAsync()
    {
        _readerCts?.Cancel();
        if (_readerTask is not null)
            try { await _readerTask.ConfigureAwait(false); } catch { }
        _readerCts?.Dispose();
        _stream?.Dispose();
        _tcp?.Dispose();
        _lock.Dispose();
    }
}
