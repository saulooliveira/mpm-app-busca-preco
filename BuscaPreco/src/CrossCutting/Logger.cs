using Serilog;

namespace BuscaPreco.CrossCutting
{
    public class Logger
    {
        public void Info(string messageTemplate, params object[] propertyValues)
        {
            global::Serilog.Log.Information(messageTemplate, propertyValues);
        }

        public void Warning(string messageTemplate, params object[] propertyValues)
        {
            global::Serilog.Log.Warning(messageTemplate, propertyValues);
        }

        public void Error(string messageTemplate, params object[] propertyValues)
        {
            global::Serilog.Log.Error(messageTemplate, propertyValues);
        }
    }
}
