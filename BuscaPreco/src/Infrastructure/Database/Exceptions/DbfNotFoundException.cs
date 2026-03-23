using System;

namespace BuscaPreco.Infrastructure.Database.Exceptions
{
    public class DbfNotFoundException : Exception
    {
        public string FilePath { get; }

        public DbfNotFoundException(string filePath)
            : base($"Arquivo DBF nÃ£o encontrado: {filePath}")
        {
            FilePath = filePath;
        }

        public DbfNotFoundException(string filePath, Exception innerException)
            : base($"Arquivo DBF nÃ£o encontrado: {filePath}", innerException)
        {
            FilePath = filePath;
        }
    }
}
