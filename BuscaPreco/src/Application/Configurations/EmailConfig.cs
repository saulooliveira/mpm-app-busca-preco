namespace BuscaPreco.Application.Configurations
{
    public class EmailConfig
    {
        public string SmtpHost { get; set; }
        public int SmtpPort { get; set; } = 587;
        public bool EnableSsl { get; set; } = true;
        public string Username { get; set; }
        public string Password { get; set; }
        public string Remetente { get; set; }
        public string Destinatario { get; set; }
        public string DailyReportTime { get; set; } = "23:55";
        public string LogDirectory { get; set; } = "logs";
    }
}
