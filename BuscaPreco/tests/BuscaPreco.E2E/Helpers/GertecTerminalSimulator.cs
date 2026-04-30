using System.Net.Sockets;
using System.Text;
using System.Threading.Channels;

namespace BuscaPreco.E2E;

/// <summary>
/// Simula um terminal Gertec BuscaPreço conectando via TCP ao servidor.
/// Implementa o handshake completo e o protocolo de consulta de preços.
/// </summary>
public sealed class GertecTerminalSimulator : IAsyncDisposable
{
    private TcpClient? _tcpClient;
    private NetworkStream? _stream;
    private readonly SemaphoreSlim _writeLock = new(1, 1);
    private CancellationTokenSource? _readerCts;
    private Task? _readerTask;

    // Todas as mensagens do servidor (exceto #live?) são enfileiradas aqui.
    private readonly Channel<string> _respostas = Channel.CreateUnbounded<string>(
        new UnboundedChannelOptions { SingleReader = false, SingleWriter = true });

    // ── Identidade do terminal ─────────────────────────────────────────────────

    public string Host { get; init; } = "127.0.0.1";
    public int Porta { get; init; }

    /// <summary>Tipo do terminal, ex.: "tc406", "tc502".</summary>
    public string Tipo { get; init; } = "tc406";

    /// <summary>Versão do firmware. "3.*" habilita modo G2 S (áudio).</summary>
    public string Versao { get; init; } = "3.3.1 S";

    // ── Config enviada no handshake ────────────────────────────────────────────

    public string IpServidor { get; init; } = "192.168.1.1";
    public string IpCliente { get; init; } = "192.168.1.10";
    public string Mascara { get; init; } = "255.255.255.0";
    public string Linha1 { get; init; } = "SIMULADOR";
    public string Linha2 { get; init; } = "GERTEC";
    public string Gateway { get; init; } = "192.168.1.254";
    public string NomeTerminal { get; init; } = "SIMULADOR-01";
    public int Tempo { get; init; } = 3;

    public bool Conectado { get; private set; }

    public GertecTerminalSimulator(int porta, string host = "127.0.0.1")
    {
        Porta = porta;
        Host = host;
    }

    // ── Conexão ────────────────────────────────────────────────────────────────

    /// <summary>
    /// Conecta ao servidor e realiza o handshake completo do protocolo Gertec.
    /// </summary>
    public async Task ConectarAsync(CancellationToken ct = default)
    {
        _tcpClient = new TcpClient();
        await _tcpClient.ConnectAsync(Host, Porta, ct);
        _stream = _tcpClient.GetStream();

        // Handshake usa leitura direta; background reader só sobe após o handshake.
        await RealizarHandshakeAsync(ct);

        // Terminal.cs faz Thread.Sleep(500) antes de ler a resposta do updconfig.
        // Se enviarmos um código imediatamente, ele chegará no buffer junto com a
        // resposta do updconfig e será consumido por config.ProcessaUpdate(),
        // fazendo o servidor entrar no loop principal com buffer vazio (timeout de 5 s).
        await Task.Delay(700, ct);

        IniciarBackgroundReader();
        Conectado = true;
    }

    // ── Consulta de preço ──────────────────────────────────────────────────────

    /// <summary>
    /// Envia um código de barras ao servidor e aguarda a resposta de preço.
    /// </summary>
    /// <param name="codigoBarras">Código sem o prefixo '#'.</param>
    /// <param name="timeout">Timeout para aguardar a resposta (padrão: 5 s).</param>
    public async Task<RespostaConsulta> ConsultarPrecoAsync(
        string codigoBarras,
        TimeSpan? timeout = null,
        CancellationToken ct = default)
    {
        await EnviarRawAsync($"#{codigoBarras}", ct);

        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout ?? TimeSpan.FromSeconds(5));

        try
        {
            var raw = await _respostas.Reader.ReadAsync(linked.Token);
            return InterpretarResposta(codigoBarras, raw);
        }
        catch (OperationCanceledException) when (!ct.IsCancellationRequested)
        {
            throw new TimeoutException(
                $"Timeout aguardando resposta do servidor para o código '{codigoBarras}'.");
        }
    }

    /// <summary>Envia uma mensagem raw ao servidor (para testes avançados de protocolo).</summary>
    public async Task EnviarRawAsync(string mensagem, CancellationToken ct = default)
    {
        await _writeLock.WaitAsync(ct);
        try
        {
            var bytes = Encoding.ASCII.GetBytes(mensagem);
            await _stream!.WriteAsync(bytes, ct);
        }
        finally { _writeLock.Release(); }
    }

    // ── Background reader ──────────────────────────────────────────────────────

    private void IniciarBackgroundReader()
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

                    // Responde ao keepalive silenciosamente
                    if (msg == "#live?")
                    {
                        await EnviarRawAsync("#live", token);
                        continue;
                    }

                    if (!string.IsNullOrWhiteSpace(msg))
                        await _respostas.Writer.WriteAsync(msg, token);
                }
            }
            catch (OperationCanceledException) { }
            catch { /* conexão encerrada */ }
            finally
            {
                _respostas.Writer.TryComplete();
                Conectado = false;
            }
        }, token);
    }

    // ── Handshake ──────────────────────────────────────────────────────────────

    private async Task RealizarHandshakeAsync(CancellationToken ct)
    {
        // 1. Servidor → "#ok"
        var ok = await LerDiretoAsync(TimeSpan.FromSeconds(5), ct);
        if (ok != "#ok")
            throw new InvalidOperationException($"Handshake: esperado '#ok', recebido '{ok}'.");

        // 2. Simulador → "#<tipo>|<versao>"
        await EnviarRawAsync($"#{Tipo}|{Versao}", ct);

        // 3. Servidor → "#alwayslive"
        var msg = await LerDiretoAsync(TimeSpan.FromSeconds(5), ct);
        if (msg == "#alwayslive")
        {
            await EnviarRawAsync("#alwayslive_ok", ct);
            msg = await LerDiretoAsync(TimeSpan.FromSeconds(6), ct);
        }

        // 4. Servidor → "#config02?"
        if (msg == "#config02?")
        {
            await EnviarRawAsync(MontarRespostaConfig02(), ct);
            msg = await LerDiretoAsync(TimeSpan.FromSeconds(6), ct);
        }

        // 5. Servidor → "#paramconfig?"
        if (msg == "#paramconfig?")
        {
            await EnviarRawAsync(MontarRespostaParamconfig(), ct);
            msg = await LerDiretoAsync(TimeSpan.FromSeconds(6), ct);
        }

        // 6. Servidor → "#updconfig?"
        if (msg == "#updconfig?")
            await EnviarRawAsync(MontarRespostaUpdconfig(), ct);

        // Handshake concluído — background reader assume o controle do stream.
    }

    // ── Leitura direta (apenas durante o handshake) ────────────────────────────

    private async Task<string> LerDiretoAsync(TimeSpan timeout, CancellationToken ct)
    {
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(ct);
        linked.CancelAfter(timeout);

        var buf = new byte[512];
        int n = await _stream!.ReadAsync(buf, linked.Token);
        return n == 0 ? string.Empty : Encoding.ASCII.GetString(buf, 0, n).Trim('\0', '\r', '\n', ' ');
    }

    // ── Builders de resposta do protocolo ─────────────────────────────────────

    private string MontarRespostaConfig02()
    {
        // Cada campo: (char)(len + 48) + valor
        static string F(string v) => ((char)(v.Length + 48)).ToString() + v;
        return "#config02"
             + F(IpServidor) + F(IpCliente) + F(Mascara)
             + F(Linha1) + F(Linha2) + F(" ") + F(" ")
             + (char)(Tempo + 48);
    }

    private static string MontarRespostaParamconfig()
        => "#paramconfig" + (char)(0 + 48) + (char)(0 + 48);

    private string MontarRespostaUpdconfig()
    {
        static string F(string v) => ((char)(v.Length + 48)).ToString() + v;
        return "#updconfig" + F(Gateway) + F(" ") + F(NomeTerminal) + F(" ") + F(" ") + F(" ");
    }

    // ── Interpretação da resposta ─────────────────────────────────────────────

    private static RespostaConsulta InterpretarResposta(string codigoBarras, string raw)
    {
        if (raw == "#nfound")
            return new RespostaConsulta(codigoBarras, raw, null, null, false);

        if (raw.StartsWith("#") && raw.Contains('|'))
        {
            var body = raw[1..];
            var sep = body.IndexOf('|');
            return new RespostaConsulta(
                codigoBarras, raw,
                Descricao: body[..sep],
                Preco: body[(sep + 1)..],
                Encontrado: true);
        }

        return new RespostaConsulta(codigoBarras, raw, null, null, false);
    }

    // ── Disposal ───────────────────────────────────────────────────────────────

    public async ValueTask DisposeAsync()
    {
        Conectado = false;
        _readerCts?.Cancel();

        if (_readerTask is not null)
        {
            try { await _readerTask.ConfigureAwait(false); }
            catch { /* ignore */ }
        }

        _readerCts?.Dispose();
        _stream?.Dispose();
        _tcpClient?.Dispose();
        _writeLock.Dispose();
    }
}

/// <summary>Resultado de uma consulta de preço enviada ao servidor.</summary>
public sealed record RespostaConsulta(
    string CodigoBarras,
    string RespostaBruta,
    string? Descricao,
    string? Preco,
    bool Encontrado);
