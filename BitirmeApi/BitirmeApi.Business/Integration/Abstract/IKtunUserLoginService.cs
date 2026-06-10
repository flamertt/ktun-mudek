using BitirmeApi.Business.Integration.DTOs;

namespace BitirmeApi.Business.Integration.Abstract
{
    /// <summary>
    /// KTUN LoginKontrolWithUser endpoint'ine istek atar.
    /// Token dönmez; sadece kullanıcı doğrulama sonucu ve bilgisi döner.
    /// </summary>
    public interface IKtunUserLoginService
    {
        /// <summary>
        /// Kullanıcıyı KTUN LoginKontrolWithUser endpointine karşı doğrular.
        /// </summary>
        /// <param name="kullaniciAdi">Kullanıcı adı veya mail adresi (frontend'den gelen)</param>
        /// <param name="sifre">Kullanıcının okul şifresi</param>
        /// <param name="girisTipAdi">ogr (öğrenci) veya aka (akademik personel)</param>
        /// <param name="remoteIp">İsteği yapan kullanıcının IP adresi (opsiyonel)</param>
        /// <returns>Doğrulama yanıtı; servis erişilemezse null döner</returns>
        Task<KtunUserLoginResponseDto?> ValidateUserAsync(
            string kullaniciAdi,
            string sifre,
            string girisTipAdi,
            string? remoteIp = null);
    }
}
