namespace BuscaPreco.CrossCutting
{
    internal static class GertecProtocol
    {
        // Gertec protocol encodes field lengths as (char)(len + 48)
        public static char LenChar(string s) => (char)(s.Length + 48);

        // Terminal display is 20 chars wide per line
        public static string Truncate(string s, int max = 20) =>
            s.Length > max ? s[..max] : s;

        // Reads one length-prefixed field from buf and advances buf past it
        public static string ParseField(ref string buf)
        {
            if (string.IsNullOrEmpty(buf)) return string.Empty;
            int len = buf[0] - 48;
            if (len <= 0 || len >= buf.Length) return string.Empty;
            var value = buf.Substring(1, len);
            buf = buf.Length > len + 1 ? buf.Substring(len + 1) : string.Empty;
            return value;
        }
    }
}
