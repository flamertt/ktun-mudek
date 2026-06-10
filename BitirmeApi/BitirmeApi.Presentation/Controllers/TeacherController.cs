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
    [Authorize]
    public class TeacherController : ControllerBase
    {
        private readonly IUniversityApiService _universityApi;
        private readonly ICourseEvaluationService _evaluationService;
        private readonly IExamService _examService;
        private readonly IExamQuestionService _questionService;
        private readonly IExamQuestionOutcomeMappingService _questionOutcomeService;
        private readonly IAssessmentComponentService _componentService;
        private readonly IAssessmentComponentOutcomeMappingService _componentOutcomeService;
        private readonly IStudentAnswerService _studentAnswerService;
        private readonly IStudentAssessmentComponentScoreService _componentScoreService;
        private readonly IMudekEvaluationCalculatorService _mudekCalculator;
        private readonly ISurveyService _surveyService;

        public TeacherController(
            IUniversityApiService universityApi,
            ICourseEvaluationService evaluationService,
            IExamService examService,
            IExamQuestionService questionService,
            IExamQuestionOutcomeMappingService questionOutcomeService,
            IAssessmentComponentService componentService,
            IAssessmentComponentOutcomeMappingService componentOutcomeService,
            IStudentAnswerService studentAnswerService,
            IStudentAssessmentComponentScoreService componentScoreService,
            IMudekEvaluationCalculatorService mudekCalculator,
            ISurveyService surveyService)
        {
            _universityApi = universityApi;
            _evaluationService = evaluationService;
            _examService = examService;
            _questionService = questionService;
            _questionOutcomeService = questionOutcomeService;
            _componentService = componentService;
            _componentOutcomeService = componentOutcomeService;
            _studentAnswerService = studentAnswerService;
            _componentScoreService = componentScoreService;
            _mudekCalculator = mudekCalculator;
            _surveyService = surveyService;
        }

        /// <summary>Üniversite JWT'sindeki kullanıcı ID'si (sub claim).</summary>
        private int GetExternalTeacherId()
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
        // AKADEMİK DÖNEMLER
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Üniversite API'sindeki tüm akademik dönemleri listeler.</summary>
        [HttpGet("academic-terms")]
        public async Task<IActionResult> GetAcademicTerms()
        {
            try { return Ok(await _universityApi.GetAcademicTermsAsync(GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ÜNİVERSİTE API — DERSLERİM
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>
        /// Öğretmenin üniversite API'sinde tanımlı aktif dönemdeki (en yüksek dönem Id) derslerini çeker.
        /// </summary>
        [HttpGet("my-courses")]
        public async Task<IActionResult> GetMyCourses()
        {
            try
            {
                var token = GetUniversityToken();
                var activeTerm = await _universityApi.GetActiveAcademicTermAsync(token);
                if (activeTerm == null)
                    return NotFound(new { message = "Aktif akademik dönem bulunamadı." });

                var courses = await _universityApi.GetTeacherOfferingsAsync(
                    GetExternalTeacherId(), activeTerm.AcademicTermId, token);
                return Ok(courses);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Verilen akademik dönem Id için öğretmenin derslerini üniversite API'sinden çeker.</summary>
        [HttpGet("my-courses/academic-terms")]
        public async Task<IActionResult> GetMyCoursesByAcademicTerm([FromQuery] int termId)
        {
            try
            {
                var courses = await _universityApi.GetTeacherOfferingsAsync(
                    GetExternalTeacherId(), termId, GetUniversityToken());
                return Ok(courses);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Belirtilen ders açılışının detayını ve öğrenci listesini üniversite API'sinden çeker.</summary>
        [HttpGet("my-courses/{offeringId}/detail")]
        public async Task<IActionResult> GetCourseDetail(int offeringId)
        {
            try
            {
                var detail = await _universityApi.GetCourseOfferingDetailAsync(GetExternalTeacherId(), offeringId, GetUniversityToken());
                return detail == null
                    ? NotFound(new { message = "Ders açılışı bulunamadı." })
                    : Ok(detail);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Ders açılışına kayıtlı öğrenci listesini üniversite API'sinden çeker.</summary>
        [HttpGet("my-courses/{offeringId}/students")]
        public async Task<IActionResult> GetCourseStudents(int offeringId)
        {
            try
            {
                var students = await _universityApi.GetStudentsForOfferingAsync(GetExternalTeacherId(), offeringId, GetUniversityToken());
                return students == null
                    ? NotFound(new { message = "Ders açılışı bulunamadı." })
                    : Ok(students);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Belirtilen ders açılışına ait DÖÇ listesini üniversite API'sinden çeker (anket sorusu eklemek için).</summary>
        [HttpGet("my-courses/{offeringId}/clos")]
        public async Task<IActionResult> GetClosByOffering(int offeringId)
        {
            try
            {
                // Önce courseId'yi öğren
                var detail = await _universityApi.GetCourseOfferingDetailAsync(GetExternalTeacherId(), offeringId, GetUniversityToken());
                if (detail == null) return NotFound(new { message = "Ders açılışı bulunamadı." });

                var clos = await _universityApi.GetClosByCourseidAsync(detail.CourseId, GetUniversityToken());
                return Ok(clos);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Dersin programına ait program çıktılarını üniversite API'sinden çeker.</summary>
        [HttpGet("my-courses/{offeringId}/program-outcomes")]
        public async Task<IActionResult> GetProgramOutcomesByOffering(int offeringId)
        {
            try
            {
                var detail = await _universityApi.GetCourseOfferingDetailAsync(GetExternalTeacherId(), offeringId, GetUniversityToken());
                if (detail == null) return NotFound(new { message = "Ders açılışı bulunamadı." });

                var outcomes = await _universityApi.GetProgramOutcomesAsync(detail.ProgramId, GetUniversityToken());
                return Ok(outcomes);
            }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        /// <summary>Verilen courseId için CLO–Program çıktısı matrisini üniversite API'sinden çeker.</summary>
        [HttpGet("courses/{courseId}/clo-po-map")]
        public async Task<IActionResult> GetCloPloMap(int courseId)
        {
            try { return Ok(await _universityApi.GetCloPloMapAsync(courseId, GetUniversityToken())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // MÜDEK DEĞERLENDİRME (CourseEvaluation)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("my-evaluations")]
        public async Task<IActionResult> GetMyEvaluations()
        {
            try { return Ok(await _evaluationService.GetByTeacherIdAsync(GetExternalTeacherId())); }
            catch (Exception ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpGet("my-courses/{offeringId}/evaluation")]
        public async Task<IActionResult> GetEvaluation(int offeringId)
        {
            var evaluation = await _evaluationService.GetByOfferingIdAsync(offeringId);
            return evaluation == null
                ? NotFound(new { message = "Bu ders açılışı için henüz değerlendirme oluşturulmamış." })
                : Ok(evaluation);
        }

        [HttpPost("my-courses/{offeringId}/evaluation")]
        public async Task<IActionResult> CreateEvaluation(int offeringId, [FromBody] CourseEvaluationCreateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ExternalCourseOfferingId = offeringId;
            try
            {
                var result = await _evaluationService.CreateForTeacherAsync(dto, GetExternalTeacherId(), GetUniversityToken());
                return CreatedAtAction(nameof(GetEvaluation), new { offeringId }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("evaluations/{evaluationId}")]
        public async Task<IActionResult> UpdateEvaluation(Guid evaluationId, [FromBody] CourseEvaluationUpdateDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = evaluationId;
            try { return Ok(await _evaluationService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("evaluations/{evaluationId}")]
        public async Task<IActionResult> DeleteEvaluation(Guid evaluationId)
        {
            try
            {
                await _evaluationService.DeleteForTeacherAsync(evaluationId, GetExternalTeacherId());
                return Ok(new { message = "Değerlendirme silindi." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SINAV (Exam)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("evaluations/{evaluationId}/exams")]
        public async Task<IActionResult> GetExams(Guid evaluationId)
        {
            try { return Ok(await _examService.GetByEvaluationIdForTeacherAsync(evaluationId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("exams/{examId}")]
        public async Task<IActionResult> GetExam(Guid examId)
        {
            try
            {
                var item = await _examService.GetByIdForTeacherAsync(examId, GetExternalTeacherId());
                return item == null ? NotFound() : Ok(item);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("evaluations/{evaluationId}/exams")]
        public async Task<IActionResult> CreateExam(Guid evaluationId, [FromBody] CreateExamDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.CourseEvaluationId = evaluationId;
            try
            {
                var result = await _examService.CreateAsync(dto, GetExternalTeacherId());
                return CreatedAtAction(nameof(GetExam), new { examId = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("exams/{examId}")]
        public async Task<IActionResult> UpdateExam(Guid examId, [FromBody] UpdateExamDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = examId;
            try { return Ok(await _examService.UpdateAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("exams/{examId}")]
        public async Task<IActionResult> DeleteExam(Guid examId)
        {
            try
            {
                await _examService.DeleteAsync(examId, GetExternalTeacherId());
                return Ok(new { message = "Sınav silindi." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // SINAV SORUSU (ExamQuestion)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("exams/{examId}/questions")]
        public async Task<IActionResult> GetQuestions(Guid examId)
        {
            try { return Ok(await _questionService.GetByExamIdForTeacherAsync(examId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("exam-questions/{questionId}")]
        public async Task<IActionResult> GetQuestion(Guid questionId)
        {
            try
            {
                var item = await _questionService.GetByIdForTeacherAsync(questionId, GetExternalTeacherId());
                return item == null ? NotFound() : Ok(item);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("exams/{examId}/questions")]
        public async Task<IActionResult> CreateQuestion(Guid examId, [FromBody] CreateExamQuestionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ExamId = examId;
            try
            {
                var result = await _questionService.CreateAsync(dto, GetExternalTeacherId());
                return CreatedAtAction(nameof(GetQuestion), new { questionId = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("exam-questions/{questionId}")]
        public async Task<IActionResult> UpdateQuestion(Guid questionId, [FromBody] UpdateExamQuestionDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = questionId;
            try { return Ok(await _questionService.UpdateAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("exam-questions/{questionId}")]
        public async Task<IActionResult> DeleteQuestion(Guid questionId)
        {
            try
            {
                await _questionService.DeleteAsync(questionId, GetExternalTeacherId());
                return Ok(new { message = "Soru silindi." });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpGet("exam-questions/{questionId}/clos")]
        public async Task<IActionResult> GetQuestionClos(Guid questionId)
        {
            try { return Ok(await _questionOutcomeService.GetByQuestionIdForTeacherAsync(questionId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("exam-questions/{questionId}/clos")]
        public async Task<IActionResult> AddQuestionCloMapping(Guid questionId, [FromBody] CreateExamQuestionOutcomeMappingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ExamQuestionId = questionId;
            try { return Ok(await _questionOutcomeService.AddForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("exam-question-outcome-mappings/{mappingId}")]
        public async Task<IActionResult> UpdateQuestionCloMapping(Guid mappingId, [FromBody] UpdateExamQuestionOutcomeMappingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = mappingId;
            try { return Ok(await _questionOutcomeService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("exam-question-outcome-mappings/{mappingId}")]
        public async Task<IActionResult> DeleteQuestionCloMapping(Guid mappingId)
        {
            try { await _questionOutcomeService.DeleteForTeacherAsync(mappingId, GetExternalTeacherId()); return Ok(new { message = "Mapping silindi." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ASSESSMENT COMPONENT
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("exams/{examId}/components")]
        public async Task<IActionResult> GetComponents(Guid examId)
        {
            try { return Ok(await _componentService.GetByExamIdForTeacherAsync(examId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("assessment-components/{componentId}")]
        public async Task<IActionResult> GetComponent(Guid componentId)
        {
            try
            {
                var item = await _componentService.GetByIdForTeacherAsync(componentId, GetExternalTeacherId());
                return item == null ? NotFound() : Ok(item);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("exams/{examId}/components")]
        public async Task<IActionResult> CreateComponent(Guid examId, [FromBody] CreateAssessmentComponentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ExamId = examId;
            try { return Ok(await _componentService.AddForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("assessment-components/{componentId}")]
        public async Task<IActionResult> UpdateComponent(Guid componentId, [FromBody] UpdateAssessmentComponentDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = componentId;
            try { return Ok(await _componentService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("assessment-components/{componentId}")]
        public async Task<IActionResult> DeleteComponent(Guid componentId)
        {
            try { await _componentService.DeleteForTeacherAsync(componentId, GetExternalTeacherId()); return Ok(new { message = "Component silindi." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpGet("assessment-components/{componentId}/clos")]
        public async Task<IActionResult> GetComponentClos(Guid componentId)
        {
            try { return Ok(await _componentOutcomeService.GetByComponentIdForTeacherAsync(componentId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("assessment-components/{componentId}/clos")]
        public async Task<IActionResult> AddComponentClo(Guid componentId, [FromBody] CreateAssessmentComponentOutcomeMappingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.AssessmentComponentId = componentId;
            try { return Ok(await _componentOutcomeService.AddForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPut("assessment-component-outcome-mappings/{mappingId}")]
        public async Task<IActionResult> UpdateComponentClo(Guid mappingId, [FromBody] UpdateAssessmentComponentOutcomeMappingDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = mappingId;
            try { return Ok(await _componentOutcomeService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpDelete("assessment-component-outcome-mappings/{mappingId}")]
        public async Task<IActionResult> DeleteComponentClo(Guid mappingId)
        {
            try { await _componentOutcomeService.DeleteForTeacherAsync(mappingId, GetExternalTeacherId()); return Ok(new { message = "Mapping silindi." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ÖĞRENCİ CEVAPLARI (StudentAnswer)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("exam-questions/{questionId}/answers")]
        public async Task<IActionResult> GetAnswers(Guid questionId)
        {
            try { return Ok(await _studentAnswerService.GetByQuestionForTeacherAsync(questionId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("exam-questions/{questionId}/answers")]
        public async Task<IActionResult> AddAnswer(Guid questionId, [FromBody] CreateStudentAnswerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.ExamQuestionId = questionId;
            try { return Ok(await _studentAnswerService.AddForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPost("exam-questions/{questionId}/answers/bulk")]
        public async Task<IActionResult> AddAnswersBulk(Guid questionId, [FromBody] BulkStudentAnswerRequestDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _studentAnswerService.AddBulkForTeacherAsync(questionId, dto.Items, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("student-answers/{answerId}")]
        public async Task<IActionResult> UpdateAnswer(Guid answerId, [FromBody] UpdateStudentAnswerDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = answerId;
            try { return Ok(await _studentAnswerService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("student-answers/{answerId}")]
        public async Task<IActionResult> DeleteAnswer(Guid answerId)
        {
            try { await _studentAnswerService.DeleteForTeacherAsync(answerId, GetExternalTeacherId()); return Ok(new { message = "Answer silindi." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // COMPONENT SCORE
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("assessment-components/{componentId}/scores")]
        public async Task<IActionResult> GetScores(Guid componentId)
        {
            try { return Ok(await _componentScoreService.GetByComponentForTeacherAsync(componentId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("assessment-components/{componentId}/scores")]
        public async Task<IActionResult> AddScore(Guid componentId, [FromBody] CreateStudentAssessmentComponentScoreDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.AssessmentComponentId = componentId;
            try { return Ok(await _componentScoreService.AddForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return Conflict(new { message = ex.Message }); }
        }

        [HttpPost("assessment-components/{componentId}/scores/bulk")]
        public async Task<IActionResult> AddScoresBulk(Guid componentId, [FromBody] BulkStudentScoreDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            try { return Ok(await _componentScoreService.AddBulkForTeacherAsync(componentId, dto.Scores, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("student-assessment-component-scores/{scoreId}")]
        public async Task<IActionResult> UpdateScore(Guid scoreId, [FromBody] UpdateStudentAssessmentComponentScoreDto dto)
        {
            if (!ModelState.IsValid) return BadRequest(ModelState);
            dto.Id = scoreId;
            try { return Ok(await _componentScoreService.UpdateForTeacherAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
        }

        [HttpDelete("student-assessment-component-scores/{scoreId}")]
        public async Task<IActionResult> DeleteScore(Guid scoreId)
        {
            try { await _componentScoreService.DeleteForTeacherAsync(scoreId, GetExternalTeacherId()); return Ok(new { message = "Score silindi." }); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // MÜDEK HESAPLAMA
        // ════════════════════════════════════════════════════════════════════════

        /// <summary>Kaydedilmiş MÜDEK sonuç özeti.</summary>
        [HttpGet("my-courses/{offeringId}/mudek-evaluation/results")]
        public async Task<IActionResult> GetMudekResults(int offeringId)
        {
            try { return Ok(await _mudekCalculator.GetSnapshotAsync(offeringId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        /// <summary>MÜDEK hesapla (yeni veya güncelle).</summary>
        [HttpPost("my-courses/{offeringId}/mudek-evaluation/calculate")]
        public Task<IActionResult> CalculateMudek(int offeringId) => ExecuteMudekRecalculateAsync(offeringId);

        [HttpPost("my-courses/{offeringId}/mudek-evaluation/recalculate")]
        public Task<IActionResult> RecalculateMudek(int offeringId) => ExecuteMudekRecalculateAsync(offeringId);

        [HttpPost("evaluations/{evaluationId}/mudek-evaluation/calculate")]
        public async Task<IActionResult> CalculateMudekForEvaluation(Guid evaluationId)
        {
            var eval = await _evaluationService.GetByIdAsync(evaluationId);
            if (eval == null)
                return NotFound(new { message = "Değerlendirme bulunamadı." });
            if (eval.ExternalTeacherId != GetExternalTeacherId())
                return Forbid();
            return await ExecuteMudekRecalculateAsync(eval.ExternalCourseOfferingId);
        }

        private async Task<IActionResult> ExecuteMudekRecalculateAsync(int externalCourseOfferingId)
        {
            try
            {
                return Ok(await _mudekCalculator.RecalculateAsync(externalCourseOfferingId, GetExternalTeacherId(), GetUniversityToken()));
            }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("evaluations/{evaluationId}/full-detail")]
        public async Task<IActionResult> GetEvaluationFullDetail(Guid evaluationId)
        {
            try
            {
                var eval = await _evaluationService.GetByIdAsync(evaluationId);
                if (eval == null) return NotFound(new { message = "Değerlendirme bulunamadı." });
                if (eval.ExternalTeacherId != GetExternalTeacherId()) return Forbid();

                var exams = await _examService.GetByEvaluationIdForTeacherAsync(evaluationId, GetExternalTeacherId());
                var componentsByExam = new Dictionary<Guid, object>();
                foreach (var exam in exams)
                {
                    componentsByExam[exam.Id] = await _componentService.GetByExamIdForTeacherAsync(exam.Id, GetExternalTeacherId());
                }
                return Ok(new { evaluationId, exams, componentsByExam });
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        // ════════════════════════════════════════════════════════════════════════
        // ANKET (SURVEY)
        // ════════════════════════════════════════════════════════════════════════

        [HttpGet("my-courses/{offeringId}/surveys")]
        public async Task<IActionResult> GetSurveys(int offeringId)
        {
            try { return Ok(await _surveyService.GetByOfferingIdAsync(offeringId, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("surveys/{surveyId}")]
        public async Task<IActionResult> GetSurvey(Guid surveyId)
        {
            try
            {
                var result = await _surveyService.GetByIdAsync(surveyId, GetExternalTeacherId());
                return result == null
                    ? NotFound(new { message = "Anket bulunamadı." })
                    : Ok(result);
            }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("surveys")]
        public async Task<IActionResult> CreateSurvey([FromBody] CreateSurveyDto dto)
        {
            try
            {
                var result = await _surveyService.CreateAsync(dto, GetExternalTeacherId());
                return CreatedAtAction(nameof(GetSurvey), new { surveyId = result.Id }, result);
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("surveys/{surveyId}")]
        public async Task<IActionResult> UpdateSurvey(Guid surveyId, [FromBody] UpdateSurveyDto dto)
        {
            if (dto.Id != surveyId)
                return BadRequest(new { message = "URL ile body Id uyuşmuyor." });
            try { return Ok(await _surveyService.UpdateAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("surveys/{surveyId}")]
        public async Task<IActionResult> DeleteSurvey(Guid surveyId)
        {
            try
            {
                await _surveyService.DeleteAsync(surveyId, GetExternalTeacherId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPatch("surveys/{surveyId}/toggle-active")]
        public async Task<IActionResult> ToggleSurveyActive(Guid surveyId)
        {
            try
            {
                await _surveyService.ToggleActiveAsync(surveyId, GetExternalTeacherId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPost("surveys/{surveyId}/questions")]
        public async Task<IActionResult> AddSurveyQuestion(Guid surveyId, [FromBody] CreateSurveyQuestionDto dto)
        {
            if (dto.SurveyId != surveyId)
                return BadRequest(new { message = "URL ile body surveyId uyuşmuyor." });
            try { return Ok(await _surveyService.AddQuestionAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpPut("surveys/{surveyId}/questions/{questionId}")]
        public async Task<IActionResult> UpdateSurveyQuestion(Guid surveyId, Guid questionId, [FromBody] UpdateSurveyQuestionDto dto)
        {
            if (dto.Id != questionId)
                return BadRequest(new { message = "URL ile body Id uyuşmuyor." });
            try { return Ok(await _surveyService.UpdateQuestionAsync(dto, GetExternalTeacherId())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (InvalidOperationException ex) { return BadRequest(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpDelete("surveys/{surveyId}/questions/{questionId}")]
        public async Task<IActionResult> DeleteSurveyQuestion(Guid surveyId, Guid questionId)
        {
            try
            {
                await _surveyService.DeleteQuestionAsync(questionId, GetExternalTeacherId());
                return NoContent();
            }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }

        [HttpGet("surveys/{surveyId}/results")]
        public async Task<IActionResult> GetSurveyResults(Guid surveyId)
        {
            try { return Ok(await _surveyService.GetResultsAsync(surveyId, GetExternalTeacherId(), GetUniversityToken())); }
            catch (KeyNotFoundException ex) { return NotFound(new { message = ex.Message }); }
            catch (UnauthorizedAccessException) { return Forbid(); }
        }
    }
}
