using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace BitirmeApi.Presentation.Controllers
{
    [Route("api/[controller]")]
    [ApiController]
    [Authorize(Roles = "Student")]
    public class StudentController : ControllerBase
    {
        private readonly IStudentSurveyService _surveyService;
        private readonly IAcademicTermService _academicTermService;
        private readonly IKtunServiceTokenService _ktunServiceToken;

        public StudentController(
            IStudentSurveyService surveyService,
            IAcademicTermService academicTermService,
            IKtunServiceTokenService ktunServiceToken)
        {
            _surveyService = surveyService;
            _academicTermService = academicTermService;
            _ktunServiceToken = ktunServiceToken;
        }

        /// <summary>MÜDEK JWT'deki numara claim'i (öğrenci numarası).</summary>
        private int GetExternalStudentId()
        {
            var val = User.FindFirst(ClaimTypes.NameIdentifier)?.Value
                   ?? User.FindFirst("sub")?.Value
                   ?? User.FindFirst("numara")?.Value;
            if (string.IsNullOrEmpty(val) || !int.TryParse(val, out var id))
                throw new UnauthorizedAccessException("Kullanıcı ID claim bulunamadı.");
            return id;
        }

        /// <summary>
        /// KTUN servis tokenını döner (usrMudek hesabı ile alınan backend token'ı).
        /// MÜDEK JWT'sinden değil, IKtunServiceTokenService'den alınır.
        /// </summary>
        private async Task<string> GetKtunServiceTokenAsync()
            => await _ktunServiceToken.GetTokenAsync();

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
                    GetExternalStudentId(), activeTerm.Id, await GetKtunServiceTokenAsync()));
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
                return Ok(await _surveyService.GetActiveSurveysAsync(offeringId, GetExternalStudentId(), await GetKtunServiceTokenAsync()));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("surveys/{surveyId}")]
        public async Task<IActionResult> GetSurveyDetail(Guid surveyId)
        {
            try
            {
                return Ok(await _surveyService.GetSurveyDetailAsync(surveyId, GetExternalStudentId(), await GetKtunServiceTokenAsync()));
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("surveys/{surveyId}/submit")]
        public async Task<IActionResult> SubmitSurvey(Guid surveyId, [FromBody] SubmitSurveyDto dto)
        {
            try
            {
                var result = await _surveyService.SubmitAsync(surveyId, GetExternalStudentId(), await GetKtunServiceTokenAsync(), dto);
                return CreatedAtAction(nameof(GetSurveyDetail), new { surveyId }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
