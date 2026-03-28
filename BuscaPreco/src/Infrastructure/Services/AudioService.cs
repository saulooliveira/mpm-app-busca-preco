using System;
using System.IO;
using BuscaPreco.Application.Configurations;
using BuscaPreco.CrossCutting;
using Microsoft.Extensions.Options;

namespace BuscaPreco.Infrastructure.Services
{
    public class AudioService
    {
        private readonly AudioConfig _config;
        private readonly Logger _logger;
        private byte[] _cachedWav;
        private readonly object _lock = new object();

        public AudioService(IOptions<AudioConfig> options, Logger logger)
        {
            _config = options.Value;
            _logger = logger;
        }

        /// <summary>
        /// Retorna true se o áudio está configurado e o arquivo existe.
        /// </summary>
        public bool IsEnabled =>
            !string.IsNullOrWhiteSpace(_config.WavFilePath) &&
            File.Exists(ResolveFilePath());

        public int Volume => _config.Volume;
        public int DuracaoSegundos => _config.DuracaoSegundos;

        /// <summary>
        /// Retorna os bytes do WAV, carregando do disco na primeira chamada e cacheando.
        /// Retorna null se não configurado ou arquivo inválido.
        /// </summary>
        public byte[] GetWavBytes()
        {
            if (!IsEnabled) return null;

            lock (_lock)
            {
                if (_cachedWav != null) return _cachedWav;

                try
                {
                    var path = ResolveFilePath();
                    var bytes = File.ReadAllBytes(path);

                    // Validações do manual p.33: 16KB-68KB
                    const int minBytes = 16 * 1024;
                    const int maxBytes = 68 * 1024;
                    if (bytes.Length < minBytes || bytes.Length > maxBytes)
                    {
                        _logger.Warning(
                            "AudioService: arquivo WAV fora do tamanho permitido ({Size} bytes). " +
                            "Permitido: {Min}-{Max} bytes. Áudio desabilitado.",
                            bytes.Length, minBytes, maxBytes);
                        return null;
                    }

                    _cachedWav = bytes;
                    _logger.Info("AudioService: WAV carregado ({Size} bytes) de {Path}",
                        bytes.Length, path);
                    return _cachedWav;
                }
                catch (Exception ex)
                {
                    _logger.Warning("AudioService: falha ao carregar WAV: {Erro}", ex.Message);
                    return null;
                }
            }
        }

        private string ResolveFilePath()
        {
            var path = _config.WavFilePath;
            if (!Path.IsPathRooted(path))
                path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, path);
            return path;
        }
    }
}
