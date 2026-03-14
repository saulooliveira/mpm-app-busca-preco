using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BuscaPreco
{
    using System;
    using System.IO;

    public class Logger
    {
        private readonly string _logDirectory;
        private string _logFilePath;

        public Logger()
        {
            // Define o diretório de execução do aplicativo
            _logDirectory = AppDomain.CurrentDomain.BaseDirectory;

            // Inicializa o arquivo de log para o dia atual
            UpdateLogFilePath();
        }

        // Método para atualizar o caminho do arquivo de log com a data atual
        private void UpdateLogFilePath()
        {
            string currentDate = DateTime.Now.ToString("yyyy-MM-dd");
            _logFilePath = Path.Combine(_logDirectory, $"app_{currentDate}.log");
        }

        // Método para registrar informações
        public void Info(string message)
        {
            Log("INFO", message);
        }

        // Método para registrar avisos
        public void Warning(string message)
        {
            Log("WARNING", message);
        }

        // Método para registrar erros
        public void Error(string message)
        {
            Log("ERROR", message);
        }

        // Método privado para escrever no log
        private void Log(string logLevel, string message)
        {
            // Atualiza o caminho do arquivo de log se a data mudou
            UpdateLogFilePath();

            // Cria uma entrada de log com timestamp
            string logEntry = $"{DateTime.Now:yyyy-MM-dd HH:mm:ss} [{logLevel}] {message}";

            // Escreve a entrada de log no arquivo
            File.AppendAllText(_logFilePath, logEntry + Environment.NewLine);
        }
    }

}
