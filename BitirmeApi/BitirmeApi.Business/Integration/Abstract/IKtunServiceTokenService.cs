namespace BitirmeApi.Business.Integration.Abstract
{
    /// <summary>
    /// usrMudek servis hesabıyla KTUN API için token alır ve cache'ler.
    /// Bu token SADECE backend içinde kullanılır; frontend'e kesinlikle gönderilmez.
    /// </summary>
    public interface IKtunServiceTokenService
    {
        /// <summary>
        /// Geçerli KTUN servis tokenını döner.
        /// Token cache'deyse ve süresi dolmamışsa cache'den döner.
        /// Token yoksa veya süresi dolmuşsa usrMudek ile yeni token alır.
        /// </summary>
        Task<string> GetTokenAsync();

        /// <summary>
        /// Cache'deki tokenı geçersiz kılar.
        /// KTUN API 401 döndüğünde çağrılır.
        /// </summary>
        void InvalidateToken();
    }
}
