namespace BitirmeApi.Business.Settings
{
    public class JwtConfig
    {
        public string Issuer { get; set; } = "MudekApi";
        public string Audience { get; set; } = "MudekClient";
        public string SecretKey { get; set; } = string.Empty;
        public int ExpireMinutes { get; set; } = 60;
    }
}
