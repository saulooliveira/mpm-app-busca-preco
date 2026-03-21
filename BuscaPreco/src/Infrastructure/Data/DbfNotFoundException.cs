using System;

namespace BuscaPreco.Infrastructure.Data
{
    public class DbfNotFoundException : Exception
    {
        public string FilePath { get; }

        public DbfNotFoundException(string filePath)
            : base($"Arquivo DBF não encontrado: {filePath}")
        {
            FilePath = filePath;
        }

        public DbfNotFoundException(string filePath, Exception innerException)
            : base($"Arquivo DBF não encontrado: {filePath}", innerException)
        {
            FilePath = filePath;
        }
    }
}
