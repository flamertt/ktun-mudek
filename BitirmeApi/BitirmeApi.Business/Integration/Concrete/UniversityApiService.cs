using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.Business.Integration.DTOs;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Logging;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Security.Claims;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace BitirmeApi.Business.Integration.Concrete
{
    public class UniversityApiService : IUniversityApiService
    {
        private readonly HttpClient _http;
        private readonly IKtunServiceTokenService _ktunServiceToken;
        private readonly IHttpContextAccessor _httpContextAccessor;
        private readonly ILogger<UniversityApiService> _logger;

        private static readonly JsonSerializerOptions _jsonOpts = new()
        {
            PropertyNameCaseInsensitive = true,
            NumberHandling = JsonNumberHandling.AllowReadingFromString
        };

        public UniversityApiService(
            HttpClient http,
            IKtunServiceTokenService ktunServiceToken,
            IHttpContextAccessor httpContextAccessor,
            ILogger<UniversityApiService> logger)
        {
            _http = http;
            _ktunServiceToken = ktunServiceToken;
            _httpContextAccessor = httpContextAccessor;
            _logger = logger;
        }

        // ── Claim Yardımcıları ────────────────────────────────────────────────────

        /// <summary>
        /// MÜDEK JWT'deki "email" claim'ini döner.
        /// Bu değer KTUN'dan gelen kullanıcı adı/mail önekidir (örn. "mgunduz", "mskiran").
        /// Üniversite API'lerine gönderilecek email parametresi budur.
        /// </summary>
        private string GetCurrentUserEmail()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            var email = user?.FindFirst("email")?.Value
                     ?? user?.FindFirst(ClaimTypes.Email)?.Value
                     ?? string.Empty;
            return email;
        }

        /// <summary>
        /// MÜDEK JWT'deki "numara" claim'ini döner (öğrenci numarası veya personel numarası).
        /// </summary>
        private string GetCurrentUserNumara()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("numara")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value
                ?? string.Empty;
        }

        /// <summary>
        /// Öğrenci API'leri için dahili üniversite ID'sini döner.
        /// OgrID claim'i varsa onu, yoksa sub/numara'ya düşer.
        /// </summary>
        private string GetCurrentUserOgrId()
        {
            var user = _httpContextAccessor.HttpContext?.User;
            return user?.FindFirst("ogrId")?.Value
                ?? user?.FindFirst(ClaimTypes.NameIdentifier)?.Value
                ?? user?.FindFirst("sub")?.Value
                ?? string.Empty;
        }

        // ── Auth ──────────────────────────────────────────────────────────────────
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

        // ── Akademik Dönemler ─────────────────────────────────────────────────────
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
            return terms.OrderByDescending(t => t.AcademicTermId).FirstOrDefault();
        }

        // ── Öğretim Elemanı Dersleri ──────────────────────────────────────────────
        // GET /api/v1/Mudek/ogretim-elemani-dersleri?email={email}&academicTermId={id}
        public async Task<List<UniversityCourseOfferingDto>> GetTeacherOfferingsAsync(
            int teacherId, int academicTermId, string universityToken)
        {
            var email = GetCurrentUserEmail();
            return await GetAsync<List<UniversityCourseOfferingDto>>(
                       $"api/v1/Mudek/ogretim-elemani-dersleri?email={email}&academicTermId={academicTermId}",
                       universityToken)
                   ?? new List<UniversityCourseOfferingDto>();
        }

        public async Task<List<UniversityCourseOfferingDto>> GetTeacherOfferingsByEmailAsync(
            string email, int academicTermId, string universityToken)
        {
            return await GetAsync<List<UniversityCourseOfferingDto>>(
                       $"api/v1/Mudek/ogretim-elemani-dersleri?email={Uri.EscapeDataString(email)}&academicTermId={academicTermId}",
                       universityToken)
                   ?? new List<UniversityCourseOfferingDto>();
        }

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

        public async Task<List<UniversityTermCourseOfferingDto>> GetTeacherOfferingsWithTermAsync(
            int teacherId, int academicTermId, string universityToken)
        {
            var terms = await GetAcademicTermsAsync(universityToken);
            var term = terms.FirstOrDefault(t => t.AcademicTermId == academicTermId);
            var offerings = await GetTeacherOfferingsAsync(teacherId, academicTermId, universityToken);
            return offerings.Select(o => new UniversityTermCourseOfferingDto
            {
                CourseOfferingId = o.CourseOfferingId,
                CourseId = o.CourseId,
                CourseCode = o.CourseCode,
                CourseName = o.CourseName,
                ProgramId = o.ProgramId,
                AcademicTermId = academicTermId,
                AcademicTermName = term?.AcademicTermName ?? string.Empty,
            }).ToList();
        }

        // ── Öğretim Elemanı Ders Detay ────────────────────────────────────────────
        // GET /api/v1/Mudek/ogretim-elemani-ders-detay?email={email}&courseOfferingId={id}
        public async Task<UniversityCourseOfferingDetailDto?> GetCourseOfferingDetailAsync(
            int teacherId, int courseOfferingId, string universityToken)
        {
            var email = GetCurrentUserEmail();
            return await GetAsync<UniversityCourseOfferingDetailDto>(
                $"api/v1/Mudek/ogretim-elemani-ders-detay?email={email}&courseOfferingId={courseOfferingId}",
                universityToken);
        }

        public async Task<List<UniversityStudentDto>> GetStudentsForOfferingAsync(
            int teacherId, int courseOfferingId, string universityToken)
        {
            var detail = await GetCourseOfferingDetailAsync(teacherId, courseOfferingId, universityToken);
            return detail?.Students ?? new List<UniversityStudentDto>();
        }

        // ── Öğrenci Ders Listesi ──────────────────────────────────────────────────
        // GET /api/v1/Mudek/ogrenci-ders-listesi?studentId={id}&academicTermId={id}
        public async Task<List<UniversityCourseOfferingDto>> GetStudentOfferingsAsync(
            int studentId, int academicTermId, string universityToken)
        {
            var ogrId = GetCurrentUserOgrId();
            return await GetAsync<List<UniversityCourseOfferingDto>>(
                       $"api/v1/Mudek/ogrenci-ders-listesi?studentId={ogrId}&academicTermId={academicTermId}",
                       universityToken)
                   ?? new List<UniversityCourseOfferingDto>();
        }

        // ── Ders Öğrenim Çıktıları (CLO) ──────────────────────────────────────────
        // GET /api/v1/Mudek/ders-ogrenim-ciktilari?courseId={id}
        public async Task<List<UniversityCloDto>> GetClosByCourseidAsync(int courseId, string universityToken)
        {
            return await GetAsync<List<UniversityCloDto>>(
                       $"api/v1/Mudek/ders-ogrenim-ciktilari?courseId={courseId}",
                       universityToken)
                   ?? new List<UniversityCloDto>();
        }

        // ── Program Çıktıları ─────────────────────────────────────────────────────
        // GET /api/v1/Mudek/program-ciktilari?programId={id}
        public async Task<List<UniversityProgramOutcomeDto>> GetProgramOutcomesAsync(
            int programId, string universityToken)
        {
            return await GetAsync<List<UniversityProgramOutcomeDto>>(
                       $"api/v1/Mudek/program-ciktilari?programId={programId}",
                       universityToken)
                   ?? new List<UniversityProgramOutcomeDto>();
        }

        // ── CLO–Program Matrisi ───────────────────────────────────────────────────
        // GET /api/v1/Mudek/ders-program-matrisi?courseId={id}
        public async Task<List<UniversityCloPloMapDto>> GetCloPloMapAsync(int courseId, string universityToken)
        {
            return await GetAsync<List<UniversityCloPloMapDto>>(
                       $"api/v1/Mudek/ders-program-matrisi?courseId={courseId}",
                       universityToken)
                   ?? new List<UniversityCloPloMapDto>();
        }

        // ── Program Listesi ───────────────────────────────────────────────────────
        // GET /api/v1/Mudek/program-birimagaci
        public async Task<List<UniversityProgramDto>> GetProgramsAsync(string universityToken)
        {
            return await GetAsync<List<UniversityProgramDto>>(
                       "api/v1/Mudek/program-birimagaci", universityToken)
                   ?? new List<UniversityProgramDto>();
        }

        // ── Helper ────────────────────────────────────────────────────────────────

        /// <summary>
        /// KTUN API'sine GET isteği atar.
        /// KTUN 401 dönerse token yenilenir ve istek bir kez daha denenir.
        /// </summary>
        private async Task<T?> GetAsync<T>(string relativeUrl, string token)
        {
            try
            {
                var resp = await SendGetAsync(relativeUrl, token);

                if (resp.StatusCode == HttpStatusCode.Unauthorized)
                {
                    _logger.LogWarning("KTUN API 401 döndü, token yenileniyor: {Url}", relativeUrl);
                    _ktunServiceToken.InvalidateToken();
                    var newToken = await _ktunServiceToken.GetTokenAsync();
                    resp = await SendGetAsync(relativeUrl, newToken);
                }

                if (!resp.IsSuccessStatusCode)
                {
                    _logger.LogWarning("Üniversite API {Url} → {Status}", relativeUrl, (int)resp.StatusCode);
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

        private async Task<HttpResponseMessage> SendGetAsync(string relativeUrl, string token)
        {
            var req = new HttpRequestMessage(HttpMethod.Get, relativeUrl);
            if (!string.IsNullOrEmpty(token))
                req.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            return await _http.SendAsync(req);
        }
    }
}
