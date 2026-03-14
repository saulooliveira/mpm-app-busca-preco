namespace BuscaPreco.Application.Configurations
{
    public class FeatureConfig
    {
        public int CacheTTLMinutes { get; set; } = 10;
        public string WebhookUrl { get; set; }
        public int IdleTimeoutSeconds { get; set; } = 30;
        public int ScreensaverRotationSeconds { get; set; } = 5;
    }
}
