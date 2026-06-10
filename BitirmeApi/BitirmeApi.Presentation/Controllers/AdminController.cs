using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class AdminController : ControllerBase
    {
        private readonly ICourseEvaluationService _evaluationService;
        private readonly IUniversityApiService _universityApi;
        private readonly IAcademicTermService _academicTermService;
        private readonly ILetterGradeRuleService _letterGradeRuleService;

        public AdminController(
            ICourseEvaluationService evaluationService,
            IUniversityApiService universityApi,
            IAcademicTermService academicTermService,
            ILetterGradeRuleService letterGradeRuleService)
        {
            _evaluationService = evaluationService;
            _universityApi = universityApi;
            _academicTermService = academicTermService;
            _letterGradeRuleService = letterGradeRuleService;
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
    }
}
