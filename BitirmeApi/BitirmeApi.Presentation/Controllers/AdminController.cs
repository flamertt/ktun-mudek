using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.Constants;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Personel")]
    public class AdminController : ControllerBase
    {
        private readonly ICourseEvaluationService _evaluationService;
        private readonly IUniversityApiService _universityApi;
        private readonly IAcademicTermService _academicTermService;
        private readonly ILetterGradeRuleService _letterGradeRuleService;
        private readonly ICourseCloService _courseCloService;
        private readonly ICourseCloPloMapService _courseCloPloMapService;

        public AdminController(
            ICourseEvaluationService evaluationService,
            IUniversityApiService universityApi,
            IAcademicTermService academicTermService,
            ILetterGradeRuleService letterGradeRuleService,
            ICourseCloService courseCloService,
            ICourseCloPloMapService courseCloPloMapService)
        {
            _evaluationService = evaluationService;
            _universityApi = universityApi;
            _academicTermService = academicTermService;
            _letterGradeRuleService = letterGradeRuleService;
            _courseCloService = courseCloService;
            _courseCloPloMapService = courseCloPloMapService;
        }

        /// <summary>Authorization header'daki Bearer token'ı döndürür (üniversite API token'ı).</summary>
        private string GetUniversityToken()
        {
            var header = HttpContext.Request.Headers["Authorization"].FirstOrDefault();
            if (string.IsNullOrEmpty(header) || !header.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
                throw new UnauthorizedAccessException("Authorization header bulunamadı.");
            return header["Bearer ".Length..].Trim();
        }

        // ════════════════════════════════════════════════════════════════════════
        // ÜNİVERSİTE API — SALT OKUMA
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("university/programs")]
        public async Task<IActionResult> GetPrograms()
        {
            try { return Ok(await _universityApi.GetProgramsAsync(GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("university/academic-terms")]
        public async Task<IActionResult> GetAcademicTerms()
        {
            try { return Ok(await _universityApi.GetAcademicTermsAsync(GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("university/academic-terms/active")]
        public async Task<IActionResult> GetActiveTerm()
        {
            try
            {
                var term = await _universityApi.GetActiveAcademicTermAsync(GetUniversityToken());
                return term == null ? NotFound(new { message = "Aktif dönem bulunamadı." }) : Ok(term);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>
        /// Üniversite API'sinden en güncel dönemi çekip DB'ye kaydeder/günceller.
        /// Öğrenciler aktif dönemi DB'den okuyacağından bu endpoint periyodik çağrılmalıdır.
        /// </summary>
        [HttpPost("university/academic-terms/sync")]
        public async Task<IActionResult> SyncActiveTerm()
        {
            try
            {
                var term = await _academicTermService.SyncActiveAsync(GetUniversityToken());
                return Ok(new { message = "Aktif dönem senkronize edildi.", term });
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>DB'deki aktif dönemi döner.</summary>
        [HttpGet("active-term")]
        public async Task<IActionResult> GetDbActiveTerm()
        {
            var term = await _academicTermService.GetActiveAsync();
            return term == null
                ? NotFound(new
                {
                    message =
                        "Veritabanında aktif akademik dönem kaydı yok. Admin panelinden veya POST /api/Admin/university/academic-terms/sync ile üniversite aktif dönemini senkronize edin."
                })
                : Ok(term);
        }

        [HttpGet("university/programs/{programId}/outcomes")]
        public async Task<IActionResult> GetProgramOutcomes(int programId)
        {
            try { return Ok(await _universityApi.GetProgramOutcomesAsync(programId, GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("university/courses/{courseId}/clos")]
        public async Task<IActionResult> GetCourseClos(int courseId)
        {
            try { return Ok(await _universityApi.GetClosByCourseidAsync(courseId, GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("university/courses/{courseId}/clo-po-map")]
        public async Task<IActionResult> GetCloPloMap(int courseId)
        {
            try { return Ok(await _universityApi.GetCloPloMapAsync(courseId, GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // DERS DEĞERLENDİRME — ADMIN BAKIŞ AÇISI
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("course-evaluations")]
        public async Task<IActionResult> GetAllEvaluations()
            => Ok(await _evaluationService.GetAllAsync());

        [HttpGet("course-evaluations/{id}")]
        public async Task<IActionResult> GetEvaluation(Guid id)
        {
            var item = await _evaluationService.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpGet("course-evaluations/by-offering/{externalCourseOfferingId}")]
        public async Task<IActionResult> GetEvaluationByOffering(int externalCourseOfferingId)
        {
            var item = await _evaluationService.GetByOfferingIdAsync(externalCourseOfferingId);
            return item == null ? NotFound(new { message = "Bu açılış için henüz değerlendirme yok." }) : Ok(item);
        }

        [HttpGet("course-evaluations/by-teacher/{externalTeacherId}")]
        public async Task<IActionResult> GetEvaluationsByTeacher(int externalTeacherId)
            => Ok(await _evaluationService.GetByTeacherIdAsync(externalTeacherId));

        // ════════════════════════════════════════════════════════════════════════
        // HARF NOTU KURALLARI — program bazlı (ExternalProgramId)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("letter-grade-rules")]
        public async Task<IActionResult> GetAllLetterGradeRules()
            => Ok(await _letterGradeRuleService.GetAllAsync());

        [HttpGet("programs/{externalProgramId:int}/letter-grade-rules")]
        public async Task<IActionResult> GetLetterGradeRulesByProgram(int externalProgramId)
            => Ok(await _letterGradeRuleService.GetByProgramIdAsync(externalProgramId));

        [HttpGet("letter-grade-rules/{id:guid}")]
        public async Task<IActionResult> GetLetterGradeRule(Guid id)
        {
            var item = await _letterGradeRuleService.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("letter-grade-rules")]
        public async Task<IActionResult> CreateLetterGradeRule([FromBody] CreateLetterGradeRuleDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _letterGradeRuleService.AddAsync(dto);
                return CreatedAtAction(nameof(GetLetterGradeRule), new { id = created.Id }, created);
            }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("letter-grade-rules/{id:guid}")]
        public async Task<IActionResult> UpdateLetterGradeRule(Guid id, [FromBody] UpdateLetterGradeRuleDto dto)
        {
            if (dto.Id != id) return BadRequest(new { message = "URL ile gövde Id uyuşmuyor." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _letterGradeRuleService.UpdateAsync(dto)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("letter-grade-rules/{id:guid}")]
        public async Task<IActionResult> DeleteLetterGradeRule(Guid id)
        {
            try
            {
                await _letterGradeRuleService.DeleteAsync(id);
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ÖĞRETİM ELEMANI DERS LİSTESİ (admin tarafı — CLO ekleme için)
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Belirli bir dönemde bir öğretim elemanının derslerini listeler.
        /// Admin'in CLO ekleyeceği courseId'yi bulmak için kullanılır.
        /// </summary>
        [HttpGet("university/teacher-courses")]
        public async Task<IActionResult> GetTeacherCourses([FromQuery] string email, [FromQuery] int academicTermId)
        {
            if (string.IsNullOrWhiteSpace(email) || academicTermId <= 0)
                return BadRequest(new { message = "email ve academicTermId zorunludur." });
            try
            {
                var offerings = await _universityApi.GetTeacherOfferingsByEmailAsync(email, academicTermId, GetUniversityToken());
                return Ok(offerings);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // YEREL CLO YÖNETİMİ
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Bir derse ait yerel CLO'ları listeler (üniversite API'si boş döndüğünde devreye girer).</summary>
        [HttpGet("courses/{externalCourseId:int}/local-clos")]
        public async Task<IActionResult> GetLocalClos(int externalCourseId)
            => Ok(await _courseCloService.GetByExternalCourseIdAsync(externalCourseId));

        /// <summary>Hem üniversite API'sinden hem DB'den CLO birleşimini döner.</summary>
        [HttpGet("courses/{externalCourseId:int}/clos/merged")]
        public async Task<IActionResult> GetMergedClos(int externalCourseId)
        {
            try
            {
                var apiClos = await _universityApi.GetClosByCourseidAsync(externalCourseId, GetUniversityToken());
                var dbClos = await _courseCloService.GetByExternalCourseIdAsync(externalCourseId);
                return Ok(new
                {
                    fromApi = apiClos.Select(c => new
                    {
                        cloId = c.CloId,
                        description = c.Description,
                        source = CloSourceType.Api,
                        cloKey = $"{CloSourceType.Api}:{c.CloId}"
                    }),
                    fromDb = dbClos,   // CourseCloDto zaten SourceType ve CloKey içeriyor
                    apiEmpty = apiClos.Count == 0
                });
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("local-clos/{id:int}")]
        public async Task<IActionResult> GetLocalClo(int id)
        {
            var item = await _courseCloService.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("local-clos")]
        public async Task<IActionResult> CreateLocalClo([FromBody] CreateCourseCloDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _courseCloService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetLocalClo), new { id = created.Id }, created);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpPut("local-clos/{id:int}")]
        public async Task<IActionResult> UpdateLocalClo(int id, [FromBody] UpdateCourseCloDto dto)
        {
            if (dto.Id != id) return BadRequest(new { message = "URL ile gövde Id uyuşmuyor." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _courseCloService.UpdateAsync(dto)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpDelete("local-clos/{id:int}")]
        public async Task<IActionResult> DeleteLocalClo(int id)
        {
            try { await _courseCloService.DeleteAsync(id); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // YEREL CLO–PO EŞLEMESİ
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("local-clos/{cloId:int}/plo-maps")]
        public async Task<IActionResult> GetPloMapsByClo(int cloId)
            => Ok(await _courseCloPloMapService.GetByCloIdAsync(cloId));

        [HttpGet("courses/{externalCourseId:int}/local-plo-maps")]
        public async Task<IActionResult> GetPloMapsByCourse(int externalCourseId)
            => Ok(await _courseCloPloMapService.GetByExternalCourseIdAsync(externalCourseId));

        [HttpGet("local-plo-maps/{id:int}")]
        public async Task<IActionResult> GetPloMap(int id)
        {
            var item = await _courseCloPloMapService.GetByIdAsync(id);
            return item == null ? NotFound() : Ok(item);
        }

        [HttpPost("local-plo-maps")]
        public async Task<IActionResult> CreatePloMap([FromBody] CreateCourseCloPloMapDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try
            {
                var created = await _courseCloPloMapService.CreateAsync(dto);
                return CreatedAtAction(nameof(GetPloMap), new { id = created.Id }, created);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("local-plo-maps/{id:int}")]
        public async Task<IActionResult> UpdatePloMap(int id, [FromBody] UpdateCourseCloPloMapDto dto)
        {
            if (dto.Id != id) return BadRequest(new { message = "URL ile gövde Id uyuşmuyor." });
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _courseCloPloMapService.UpdateAsync(dto)); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }

        [HttpDelete("local-plo-maps/{id:int}")]
        public async Task<IActionResult> DeletePloMap(int id)
        {
            try { await _courseCloPloMapService.DeleteAsync(id); return NoContent(); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
        }
    }
}
