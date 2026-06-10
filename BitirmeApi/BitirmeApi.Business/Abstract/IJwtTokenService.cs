using BitirmeApi.Business.Integration.DTOs;

namespace BitirmeApi.Business.Abstract
{
    /// <summary>
    /// MÜDEK kullanıcı JWT token'ı üretir.
    /// Bu token frontend → backend iletişimi için kullanılır.
    /// KTUN API çağrılarında KULLANILMAMALIdır.
    /// </summary>
    public interface IJwtTokenService
    {
        /// <summary>
        /// KTUN kullanıcı doğrulama verilerinden MÜDEK JWT tokenı üretir.
        /// </summary>
        /// <param name="userData">KTUN LoginKontrolWithUser'dan dönen kullanıcı verisi</param>
        /// <param name="role">Hesaplanan MÜDEK rol (Student, Instructor, Staff, User)</param>
        /// <returns>(token, expiresAt) ikilisi</returns>
        (string Token, DateTime ExpiresAt) GenerateToken(KtunUserLoginDataDto userData, string role);

        /// <summary>
        /// TipID ve TipAdi bilgisine göre MÜDEK rolünü belirler.
        /// </summary>
        string MapRole(int tipId, string? tipAdi);
    }
}
