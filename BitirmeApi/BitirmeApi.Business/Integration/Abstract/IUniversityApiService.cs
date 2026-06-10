using BitirmeApi.Business.Integration.DTOs;

namespace BitirmeApi.Business.Integration.Abstract
{
    /// <summary>
    /// KTÜN Üniversite API'si ile entegrasyon servisi.
    /// Base URL: https://coreapiv1.ktun.edu.tr
    /// Tüm /Mudek/ endpoint'leri Bearer token ile çağrılır.
    /// </summary>
    public interface IUniversityApiService
    {
        /// <summary>Üniversite API ile kullanıcı doğrulama</summary>
        Task<UniversityLoginResponseDto?> LoginAsync(string email, string password);

        /// <summary>GET /api/v1/Mudek/akademik-takvim — Tüm akademik dönemler</summary>
        Task<List<UniversityAcademicTermDto>> GetAcademicTermsAsync(string universityToken);

        /// <summary>Akademik dönemlerden sonuncusunu döner (aktif dönem)</summary>
        Task<UniversityAcademicTermDto?> GetActiveAcademicTermAsync(string universityToken);

        /// <summary>GET /api/v1/Mudek/ogretim-elemani-dersleri?email={email}&academicTermId={id}</summary>
        Task<List<UniversityCourseOfferingDto>> GetTeacherOfferingsAsync(int teacherId, int academicTermId, string universityToken);

        /// <summary>Tüm dönemlerdeki dersleri birleştirerek döner (her dönem için ayrı çağrı yapar)</summary>
        Task<List<UniversityCourseOfferingDto>> GetAllTeacherOfferingsAsync(int teacherId, string universityToken);

        /// <summary>GET /api/v1/Mudek/ogretim-elemani-ders-detay?email={email}&courseOfferingId={id}</summary>
        Task<UniversityCourseOfferingDetailDto?> GetCourseOfferingDetailAsync(int teacherId, int courseOfferingId, string universityToken);

        /// <summary>Ders detayından öğrenci listesini döner</summary>
        Task<List<UniversityStudentDto>> GetStudentsForOfferingAsync(int teacherId, int courseOfferingId, string universityToken);

        /// <summary>GET /api/v1/Mudek/ogrenci-ders-listesi?studentId={id}&academicTermId={id}</summary>
        Task<List<UniversityCourseOfferingDto>> GetStudentOfferingsAsync(int studentId, int academicTermId, string universityToken);

        /// <summary>GET /api/v1/Mudek/ders-ogrenim-ciktilari?courseId={id}</summary>
        Task<List<UniversityCloDto>> GetClosByCourseidAsync(int courseId, string universityToken);

        /// <summary>GET /api/v1/Mudek/program-ciktilari?programId={id}</summary>
        Task<List<UniversityProgramOutcomeDto>> GetProgramOutcomesAsync(int programId, string universityToken);

        /// <summary>GET /api/v1/Mudek/ders-program-matrisi?courseId={id}</summary>
        Task<List<UniversityCloPloMapDto>> GetCloPloMapAsync(int courseId, string universityToken);

        /// <summary>GET /api/v1/Mudek/program-birimagaci — Program listesi</summary>
        Task<List<UniversityProgramDto>> GetProgramsAsync(string universityToken);
    }
}
