using System.Text.RegularExpressions;

namespace BuscaPreco.CrossCutting
{
    public static class Validators
    {
        public static string SomenteDigitos(string value)
        {
            if (string.IsNullOrEmpty(value))
            {
                return string.Empty;
            }

            return Regex.Replace(value, @"\D", string.Empty);
        }
    }
}
