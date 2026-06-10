namespace BitirmeApi.Business.Settings
{
    /// <summary>
    /// KTUN LoginKontrolWithUser endpointine gönderilecek Basic Auth bilgileri.
    /// usrMudek servis hesabına aittir; kullanıcının kendi okul bilgileriyle karıştırılmamalıdır.
    ///
    /// Development: dotnet user-secrets ile ayarlanır.
    /// Production : ortam değişkeni veya sunucu konfigürasyon yönetimiyle sağlanır.
    /// Gerçek değerler appsettings.json'a veya Git'e yazılmaz.
    /// </summary>
    public class KtunBasicAuthConfig
    {
        public string Username { get; set; } = string.Empty;
        public string Password { get; set; } = string.Empty;
    }
}
