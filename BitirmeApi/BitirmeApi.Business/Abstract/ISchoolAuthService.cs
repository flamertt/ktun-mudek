using BitirmeApi.Business.DTO;

namespace BitirmeApi.Business.Abstract
{
    /// <summary>
    /// Öğrenci ve öğretim elemanı login akışını yönetir.
    /// KTUN kullanıcı doğrulama → MÜDEK JWT üretme → KTUN servis token hazırlama.
    /// </summary>
    public interface ISchoolAuthService
    {
        /// <summary>
        /// Öğrenci login akışı.
        /// girisTipAdi = "ogr" olarak otomatik gönderilir.
        /// </summary>
        Task<AuthResult> StudentLoginAsync(LoginDto loginDto, string? remoteIp = null);

        /// <summary>
        /// Öğretim elemanı login akışı.
        /// girisTipAdi = "aka" olarak otomatik gönderilir.
        /// </summary>
        Task<AuthResult> TeacherLoginAsync(LoginDto loginDto, string? remoteIp = null);
    }
}
