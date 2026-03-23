using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using BuscaPreco.CrossCutting;

namespace BuscaPreco.Infrastructure.Config
{
    public class YamlConfigWriter
    {
        private readonly string _configFilePath;
        private readonly Logger _logger;

        public YamlConfigWriter(Logger logger)
        {
            _configFilePath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "config.yaml");
            _logger = logger;
        }

        public void SaveProdutosFixados(IEnumerable<string> codes)
        {
            try
            {
                var lines = File.Exists(_configFilePath)
                    ? File.ReadAllLines(_configFilePath).ToList()
                    : new List<string>();

                // Remove existing ProdutosFixados block (section header + all indented lines below it)
                var startIndex = lines.FindIndex(l => l.TrimEnd() == "ProdutosFixados:");
                if (startIndex >= 0)
                {
                    var endIndex = startIndex + 1;
                    while (endIndex < lines.Count &&
                           (lines[endIndex].StartsWith(" ") || lines[endIndex].StartsWith("\t") || string.IsNullOrWhiteSpace(lines[endIndex])))
                    {
                        endIndex++;
                    }
                    lines.RemoveRange(startIndex, endIndex - startIndex);
                }

                // Build new ProdutosFixados block
                var newBlock = new List<string> { "ProdutosFixados:" };
                var codeList = codes.ToList();
                if (codeList.Count == 0)
                {
                    newBlock.Add("  Codigos: []");
                }
                else
                {
                    newBlock.Add("  Codigos:");
                    foreach (var code in codeList)
                    {
                        newBlock.Add("    - \"" + code.Replace("\"", "\\\"") + "\"");
                    }
                }

                // Append block at end of file (with blank separator if file is non-empty)
                if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines.LastOrDefault()))
                    lines.Add(string.Empty);

                lines.AddRange(newBlock);

                File.WriteAllLines(_configFilePath, lines);
                _logger.Info("ProdutosFixados gravados no config.yaml: {Count} cÃ³digo(s).", codeList.Count);
            }
            catch (Exception ex)
            {
                _logger.Warning("Falha ao gravar ProdutosFixados no config.yaml: {Erro}", ex.Message);
                throw;
            }
        }
    }
}
