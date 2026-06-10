namespace BitirmeApi.Business.Settings
{
    public class KtunServiceAuthConfig
    {
        public string TokenLoginUrl { get; set; } = string.Empty;
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
        public int TokenExpireMinutes { get; set; } = 55;
    }
}
