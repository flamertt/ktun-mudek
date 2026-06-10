using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize]
    public class StudentController : ControllerBase
    {
        private readonly IStudentSurveyService _surveyService;
        private readonly IAcademicTermService _academicTermService;

        public StudentController(IStudentSurveyService surveyService, IAcademicTermService academicTermService)
        {
            _surveyService = surveyService;
            _academicTermService = academicTermService;
        }

        /// <summary>Üniversite JWT'sindeki kullanıcı ID'si (sub claim).</summary>
        private int GetExternalStudentId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? User.FindFirst("nameid")?.Value;
            if (string.IsNullOrEmpty(val) || !int.TryParse(val, out var id))
                throw new UnauthorizedAccessException("Kullanıcı ID claim bulunamadı.");
            return id;
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
        // DERSLERİM — aktif dönem DB'den alınır
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Öğrencinin aktif dönemdeki derslerini listeler.
        /// Aktif dönem DB'den okunur — admin tarafında /sync ile güncellenir.
        /// </summary>
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            try
            {
                var activeTerm = await _academicTermService.GetActiveAsync();
                if (activeTerm == null)
                    return BadRequest(new { message = "Aktif dönem bulunamadı. Admin /university/academic-terms/sync endpointini çağırmalıdır." });

                return Ok(await _surveyService.GetActiveTermCoursesAsync(
                    GetExternalStudentId(), activeTerm.Id, GetUniversityToken()));
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ANKETLER
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("my-courses/{offeringId}/surveys")]
        public async Task<IActionResult> GetSurveys(int offeringId)
        {
            try
            {
                return Ok(await _surveyService.GetActiveSurveysAsync(offeringId, GetExternalStudentId(), GetUniversityToken()));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("surveys/{surveyId}")]
        public async Task<IActionResult> GetSurveyDetail(Guid surveyId)
        {
            try
            {
                return Ok(await _surveyService.GetSurveyDetailAsync(surveyId, GetExternalStudentId(), GetUniversityToken()));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("surveys/{surveyId}/submit")]
        public async Task<IActionResult> SubmitSurvey(Guid surveyId, [FromBody] SubmitSurveyDto dto)
        {
            try
            {
                var result = await _surveyService.SubmitAsync(surveyId, GetExternalStudentId(), GetUniversityToken(), dto);
                return CreatedAtAction(nameof(GetSurveyDetail), new { surveyId }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
