using Serilog;

namespace BuscaPreco.CrossCutting
{
    public class Logger
    {
        private readonly global::Serilog.ILogger _logger;

        public Logger()
        {
            _logger = global::Serilog.Log.Logger;
        }

        public void Info(string messageTemplate, params object[] propertyValues)
        {
            _logger.Information(messageTemplate, propertyValues);
        }

        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            _logger.Warning(messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            _logger.Error(messageTemplate, propertyValues);
        }
    }
}
