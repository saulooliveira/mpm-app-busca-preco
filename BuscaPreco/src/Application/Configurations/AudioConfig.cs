namespace BuscaPreco.Application.Configurations
{
    public class AudioConfig
    {
        /// <summary>
        /// Caminho absoluto ou relativo ao diretório da aplicação para o arquivo WAV.
        /// Formato exigido pelo terminal: WAV 8kHz, Mono, 8-bit PCM_U8, 16-68KB, 2-7 segundos.
        /// Deixar vazio para desabilitar o áudio.
        /// </summary>
        public string WavFilePath { get; set; } = string.Empty;

        /// <summary>
        /// Volume do áudio: 0=baixo, 1=médio, 2=alto, 3=muito alto. Padrão: 2.
        /// </summary>
        public int Volume { get; set; } = 2;

        /// <summary>
        /// Duração do áudio em segundos (2-7). Padrão: 3.
        /// </summary>
        public int DuracaoSegundos { get; set; } = 3;
    }
}
