using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using Microsoft.Extensions.Logging;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitirmeApi.Business.Integration.Concrete
{
    public class UniversityApiService : IUniversityApiService
    {
        private readonly HttpClient _http;
        private readonly ILogger<UniversityApiService> _logger;

        // TODO: İleride token'ın name claim'inden alınacak. Şimdilik sabit.
        private const string TeacherEmail = "mgunduz";

        // TODO: İleride token'daki öğrenci Id kullanılacak. Şimdilik sabit.
        private const int StudentId = 12617;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public UniversityApiService(HttpClient http, ILogger<UniversityApiService> logger)
        {
            _http = http;
            _logger = logger;
        }

        // ── Auth ──────────────────────────────────────────────────────────────
        public async Task<UniversityLoginResponseDto?> LoginAsync(string email, string password)
        {
            const string url = "api/v1/Auth/login";
            try
            {
                _logger.LogInformation("Üniversite API login → {BaseAddress}{Url}", _http.BaseAddress, url);
                var resp = await _http.PostAsJsonAsync(url, new { username = email, password });
                var body = await resp.Content.ReadAsStringAsync();
                _logger.LogInformation("Üniversite API login yanıt: {Status} | {Body}", (int)resp.StatusCode, body);
                if (!resp.IsSuccessStatusCode) return null;
                return JsonSerializer.Deserialize<UniversityLoginResponseDto>(body, _jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üniversite API login hatası");
                return null;
            }
        }

        // ── Akademik Dönemler ─────────────────────────────────────────────────
        // GET /api/v1/Mudek/akademik-takvim → [{ "id", "ad" }]
        public async Task<List<UniversityAcademicTermDto>> GetAcademicTermsAsync(string universityToken)
        {
            return await GetAsync<List<UniversityAcademicTermDto>>(
                       "api/v1/Mudek/akademik-takvim", universityToken)
                   ?? new List<UniversityAcademicTermDto>();
        }

        public async Task<UniversityAcademicTermDto?> GetActiveAcademicTermAsync(string universityToken)
        {
            var terms = await GetAcademicTermsAsync(universityToken);
            // API'de isActive alanı yok; ID'si en büyük dönem = en güncel dönem
            return terms.OrderByDescending(t => t.AcademicTermId).FirstOrDefault();
        }

        // ── Öğretim Elemanı Dersleri ──────────────────────────────────────────
        // GET /api/v1/Mudek/ogretim-elemani-dersleri?email={email}&academicTermId={id}
        public async Task<List<UniversityCourseOfferingDto>> GetTeacherOfferingsAsync(
            int teacherId, int academicTermId, string universityToken)
        {
            return await GetAsync<List<UniversityCourseOfferingDto>>(
                       $"api/v1/Mudek/ogretim-elemani-dersleri?email={TeacherEmail}&academicTermId={academicTermId}",
                       universityToken)
                   ?? new List<UniversityCourseOfferingDto>();
        }

        /// <summary>
        /// Tüm dönemlerin ders listelerini birleştirerek döner.
        /// Önce akademik takvimi çeker, her dönem için ayrı istek atar.
        /// </summary>
        public async Task<List<UniversityCourseOfferingDto>> GetAllTeacherOfferingsAsync(
            int teacherId, string universityToken)
        {
            var terms = await GetAcademicTermsAsync(universityToken);
            var all = new List<UniversityCourseOfferingDto>();
            foreach (var term in terms)
            {
                var offerings = await GetTeacherOfferingsAsync(teacherId, term.AcademicTermId, universityToken);
                all.AddRange(offerings);
            }
            return all;
        }

        // ── Öğretim Elemanı Ders Detay ────────────────────────────────────────
        // GET /api/v1/Mudek/ogretim-elemani-ders-detay?email={email}&courseOfferingId={id}
        public async Task<UniversityCourseOfferingDetailDto?> GetCourseOfferingDetailAsync(
            int teacherId, int courseOfferingId, string universityToken)
        {
            return await GetAsync<UniversityCourseOfferingDetailDto>(
                $"api/v1/Mudek/ogretim-elemani-ders-detay?email={TeacherEmail}&courseOfferingId={courseOfferingId}",
                universityToken);
        }

        public async Task<List<UniversityStudentDto>> GetStudentsForOfferingAsync(
            int teacherId, int courseOfferingId, string universityToken)
        {
            var detail = await GetCourseOfferingDetailAsync(teacherId, courseOfferingId, universityToken);
            return detail?.Students ?? new List<UniversityStudentDto>();
        }

        // ── Öğrenci Ders Listesi ──────────────────────────────────────────────
        // GET /api/v1/Mudek/ogrenci-ders-listesi?studentId={id}&academicTermId={id}
        public async Task<List<UniversityCourseOfferingDto>> GetStudentOfferingsAsync(
            int studentId, int academicTermId, string universityToken)
        {
            _ = studentId; // Arayüz uyumluluğu; üniversite isteğinde StudentId sabiti kullanılır.
            return await GetAsync<List<UniversityCourseOfferingDto>>(
                       $"api/v1/Mudek/ogrenci-ders-listesi?studentId={StudentId}&academicTermId={academicTermId}",
                       universityToken)
                   ?? new List<UniversityCourseOfferingDto>();
        }

        // ── Ders Öğrenim Çıktıları (CLO) ──────────────────────────────────────
        // GET /api/v1/Mudek/ders-ogrenim-ciktilari?courseId={id}
        public async Task<List<UniversityCloDto>> GetClosByCourseidAsync(int courseId, string universityToken)
        {
            return await GetAsync<List<UniversityCloDto>>(
                       $"api/v1/Mudek/ders-ogrenim-ciktilari?courseId={courseId}",
                       universityToken)
                   ?? new List<UniversityCloDto>();
        }

        // ── Program Çıktıları ─────────────────────────────────────────────────
        // GET /api/v1/Mudek/program-ciktilari?programId={id}
        public async Task<List<UniversityProgramOutcomeDto>> GetProgramOutcomesAsync(
            int programId, string universityToken)
        {
            return await GetAsync<List<UniversityProgramOutcomeDto>>(
                       $"api/v1/Mudek/program-ciktilari?programId={programId}",
                       universityToken)
                   ?? new List<UniversityProgramOutcomeDto>();
        }

        // ── CLO–Program Matrisi ───────────────────────────────────────────────
        // GET /api/v1/Mudek/ders-program-matrisi?courseId={id}
        public async Task<List<UniversityCloPloMapDto>> GetCloPloMapAsync(int courseId, string universityToken)
        {
            return await GetAsync<List<UniversityCloPloMapDto>>(
                       $"api/v1/Mudek/ders-program-matrisi?courseId={courseId}",
                       universityToken)
                   ?? new List<UniversityCloPloMapDto>();
        }

        // ── Program Listesi ───────────────────────────────────────────────────
        // GET /api/v1/Mudek/program-birimagaci
        public async Task<List<UniversityProgramDto>> GetProgramsAsync(string universityToken)
        {
            return await GetAsync<List<UniversityProgramDto>>(
                       "api/v1/Mudek/program-birimagaci", universityToken)
                   ?? new List<UniversityProgramDto>();
        }

        // ── Helper ────────────────────────────────────────────────────────────
        private async Task<T?> GetAsync<T>(string relativeUrl, string token)
        {
            try
            {
                var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
                if (!string.IsNullOrEmpty(token))
                    req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

                var resp = await _http.SendAsync(req);
                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Üniversite API {Url} → {Status}", relativeUrl, resp.StatusCode);
                    return default;
                }

                return await resp.Content.ReadFromJsonAsync<T>(_jsonOpts);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Üniversite API GET hatası: {Url}", relativeUrl);
                return default;
            }
        }
    }
}
