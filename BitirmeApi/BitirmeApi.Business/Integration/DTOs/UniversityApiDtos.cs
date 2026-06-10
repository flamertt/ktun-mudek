using System.IdentityModel.Tokens.Jwt;

namespace BitirmeApi.Business.Integration.DTOs
{
    // ── Auth ──────────────────────────────────────────────────────────────────
    public class UniversityLoginRequestDto
    {
        public string Username { get; set; } = default!;
        public string Password { get; set; } = default!;
    }

    /// <summary>
    /// Üniversite API login yanıtının "data" nesnesi:
    /// { "token": "...", "expiration": "..." }
    /// </summary>
    public class UniversityLoginDataDto
    {
        public string? Token { get; set; }
        public string? Expiration { get; set; }
    }

    /// <summary>
    /// Üniversite API login yanıtının tam sarmalayıcısı:
    /// { "success": true, "message": "...", "data": { "token": "...", "expiration": "..." } }
    /// Kullanıcı bilgileri (Id, rol, ad) token claim'lerinden decode edilir.
    /// </summary>
    public class UniversityLoginResponseDto
    {
        public bool Success { get; set; }
        public string? Message { get; set; }
        public UniversityLoginDataDto? Data { get; set; }

        /// <summary>JWT token string'ini döner.</summary>
        public string GetToken() => Data?.Token ?? string.Empty;

        /// <summary>Token claim'lerini bir kez decode eder.</summary>
        private JwtSecurityToken? _decoded;
        private JwtSecurityToken Decoded()
        {
            var raw = GetToken();
            if (_decoded == null && !string.IsNullOrEmpty(raw))
                _decoded = new JwtSecurityToken(raw);
            return _decoded!;
        }

        private string? ClaimValue(params string[] types)
        {
            try
            {
                var jwt = Decoded();
                foreach (var t in types)
                {
                    var v = jwt.Claims.FirstOrDefault(c => c.Type == t)?.Value;
                    if (v != null) return v;
                }
            }
            catch { }
            return null;
        }

        /// <summary>Kullanıcının harici ID'si (nameidentifier claim'i).</summary>
        public int GetId()
        {
            var val = ClaimValue(
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier",
                System.Security.Claims.ClaimTypes.NameIdentifier,
                "sub", "nameid");
            return int.TryParse(val, out var id) ? id : 0;
        }

        /// <summary>Kullanıcının görünen adı (name claim'i).</summary>
        public string GetFullName() =>
            ClaimValue(
                "http://schemas.xmlsoap.org/ws/2005/05/identity/claims/name",
                System.Security.Claims.ClaimTypes.Name,
                "name") ?? string.Empty;

        /// <summary>Üniversite token'ındaki rol değeri olduğu gibi döner (Personel, Ogrenci, vb.).</summary>
        public string GetRole() =>
            ClaimValue(
                "http://schemas.microsoft.com/ws/2008/06/identity/claims/role",
                System.Security.Claims.ClaimTypes.Role,
                "role") ?? string.Empty;
    }

    // ── Academic Terms ────────────────────────────────────────────────────────
    // GET /api/v1/Mudek/akademik-takvim → [{ "id": 13, "ad": "2020-21 Güz Dönemi" }]
    public class UniversityAcademicTermDto
    {
        [System.Text.Json.Serialization.JsonPropertyName("id")]
        public int AcademicTermId { get; set; }

        [System.Text.Json.Serialization.JsonPropertyName("ad")]
        public string AcademicTermName { get; set; } = default!;
    }

    // ── Programs ──────────────────────────────────────────────────────────────
    // GET /api/v1/Mudek/program-birimagaci → [{ "programId", "programName" }]
    public class UniversityProgramDto
    {
        public int ProgramId { get; set; }
        public string ProgramName { get; set; } = default!;
    }

    // ── Course Offerings ──────────────────────────────────────────────────────
    // GET /api/v1/Mudek/ogretim-elemani-dersleri  ve  ogrenci-ders-listesi
    // → [{ "courseOfferingId", "courseId", "courseCode", "courseName", "programId" }]
    public class UniversityCourseOfferingDto
    {
        public int CourseOfferingId { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = default!;
        public string CourseName { get; set; } = default!;
        public int ProgramId { get; set; }
    }

    // ── Course Offering Detail (with students) ────────────────────────────────
    // GET /api/v1/Mudek/ogretim-elemani-ders-detay
    // → { "courseOfferingId", ..., "students": [{ "studentId", "studentNumber", "fullName" }] }
    public class UniversityCourseOfferingDetailDto
    {
        public int CourseOfferingId { get; set; }
        public int CourseId { get; set; }
        public string CourseCode { get; set; } = default!;
        public string CourseName { get; set; } = default!;
        public int ProgramId { get; set; }
        public List<UniversityStudentDto> Students { get; set; } = new();
    }

    // ── Student ───────────────────────────────────────────────────────────────
    public class UniversityStudentDto
    {
        public int StudentId { get; set; }
        public string StudentNumber { get; set; } = default!;
        public string FullName { get; set; } = default!;
    }

    // ── CLO ───────────────────────────────────────────────────────────────────
    // GET /api/v1/Mudek/ders-ogrenim-ciktilari → [{ "cloId", "description" }]
    public class UniversityCloDto
    {
        public int CloId { get; set; }
        public string Description { get; set; } = default!;
    }

    // ── Program Outcome ───────────────────────────────────────────────────────
    // GET /api/v1/Mudek/program-ciktilari
    // → [{ "programOutcomeId", "programOutcomeCode": int, "description" }]
    public class UniversityProgramOutcomeDto
    {
        public int ProgramOutcomeId { get; set; }
        public int ProgramOutcomeCode { get; set; }
        public string Description { get; set; } = default!;
    }

    // ── CLO-PLO Matrix ────────────────────────────────────────────────────────
    // GET /api/v1/Mudek/ders-program-matrisi → [{ "cloId", "programOutcomeId", "weight" }]
    public class UniversityCloPloMapDto
    {
        public int CloId { get; set; }
        public int ProgramOutcomeId { get; set; }
        public int Weight { get; set; }
    }
}
