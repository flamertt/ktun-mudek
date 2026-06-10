using AutoMapper;
using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class SurveyService : ISurveyService
    {
        private readonly ISurveyDal _surveyDal;
        private readonly IQuestionDal _questionDal;
        private readonly ISubmissionDal _submissionDal;
        private readonly ICourseEvaluationDal _courseEvalDal;
        private readonly ICloEvaluationResultDal _cloEvalDal;
        private readonly IStudentEvaluationResultDal _studentEvalDal;
        private readonly IUniversityApiService _universityApi;
        private readonly IMapper _mapper;

        public SurveyService(
            ISurveyDal surveyDal,
            IQuestionDal questionDal,
            ISubmissionDal submissionDal,
            ICourseEvaluationDal courseEvalDal,
            ICloEvaluationResultDal cloEvalDal,
            IStudentEvaluationResultDal studentEvalDal,
            IUniversityApiService universityApi,
            IMapper mapper)
        {
            _surveyDal = surveyDal;
            _questionDal = questionDal;
            _submissionDal = submissionDal;
            _courseEvalDal = courseEvalDal;
            _cloEvalDal = cloEvalDal;
            _studentEvalDal = studentEvalDal;
            _universityApi = universityApi;
            _mapper = mapper;
        }

        // ── Anket CRUD ─────────────────────────────────────────────────────────────

        public async Task<List<SurveyListDto>> GetByOfferingIdAsync(int externalCourseOfferingId, int externalTeacherId)
        {
            await VerifyOfferingOwnershipAsync(externalCourseOfferingId, externalTeacherId);
            return _mapper.Map<List<SurveyListDto>>(await _surveyDal.GetByOfferingIdAsync(externalCourseOfferingId));
        }

        public async Task<SurveyDetailDto?> GetByIdAsync(Guid id, int externalTeacherId)
        {
            var survey = await _surveyDal.GetByIdWithDetailsAsync(id);
            if (survey == null) return null;
            await VerifyOfferingOwnershipAsync(survey.ExternalCourseOfferingId, externalTeacherId);
            return _mapper.Map<SurveyDetailDto>(survey);
        }

        public async Task<SurveyDetailDto> CreateAsync(CreateSurveyDto dto, int externalTeacherId)
        {
            await VerifyOfferingOwnershipAsync(dto.ExternalCourseOfferingId, externalTeacherId);

            var entity = new Survey
            {
                Id = Guid.NewGuid(),
                ExternalCourseOfferingId = dto.ExternalCourseOfferingId,
                Title = dto.Title,
                Description = dto.Description,
                IsActive = dto.IsActive,
                CreatedAt = DateTime.UtcNow
            };

            _surveyDal.Add(entity);
            await _surveyDal.SaveChangesAsync();

            return _mapper.Map<SurveyDetailDto>((await _surveyDal.GetByIdWithDetailsAsync(entity.Id))!);
        }

        public async Task<SurveyDetailDto> UpdateAsync(UpdateSurveyDto dto, int externalTeacherId)
        {
            var snapshot = await _surveyDal.GetByIdWithDetailsAsync(dto.Id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");
            await VerifyOfferingOwnershipAsync(snapshot.ExternalCourseOfferingId, externalTeacherId);

            var tracked = await _surveyDal.GetAsync(s => s.Id == dto.Id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");

            tracked.Title = dto.Title;
            tracked.Description = dto.Description;
            tracked.IsActive = dto.IsActive;

            _surveyDal.Update(tracked);
            await _surveyDal.SaveChangesAsync();

            return _mapper.Map<SurveyDetailDto>((await _surveyDal.GetByIdWithDetailsAsync(dto.Id))!);
        }

        public async Task DeleteAsync(Guid id, int externalTeacherId)
        {
            var snapshot = await _surveyDal.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");
            await VerifyOfferingOwnershipAsync(snapshot.ExternalCourseOfferingId, externalTeacherId);

            if (snapshot.Submissions.Any())
                throw new InvalidOperationException("Ankete ait gönderimler bulunduğu için silinemez.");

            var tracked = await _surveyDal.GetAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");

            _surveyDal.Delete(tracked);
            await _surveyDal.SaveChangesAsync();
        }

        public async Task ToggleActiveAsync(Guid id, int externalTeacherId)
        {
            var snapshot = await _surveyDal.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");
            await VerifyOfferingOwnershipAsync(snapshot.ExternalCourseOfferingId, externalTeacherId);

            var tracked = await _surveyDal.GetAsync(s => s.Id == id)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");

            tracked.IsActive = !tracked.IsActive;
            _surveyDal.Update(tracked);
            await _surveyDal.SaveChangesAsync();
        }

        // ── Soru (Likert) CRUD ─────────────────────────────────────────────────────

        public async Task<SurveyQuestionDto> AddQuestionAsync(CreateSurveyQuestionDto dto, int externalTeacherId)
        {
            var survey = await _surveyDal.GetByIdWithDetailsAsync(dto.SurveyId)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");
            await VerifyOfferingOwnershipAsync(survey.ExternalCourseOfferingId, externalTeacherId);
            ValidateScale(dto.ScaleMin, dto.ScaleMax);

            var entity = new Question
            {
                Id = Guid.NewGuid(),
                SurveyId = dto.SurveyId,
                Text = dto.Text,
                Type = QuestionType.Likert,
                OrderIndex = dto.OrderIndex,
                IsRequired = dto.IsRequired,
                ScaleMin = dto.ScaleMin,
                ScaleMax = dto.ScaleMax,
                ExternalCloId = dto.ExternalCloId,
                CloCode = dto.CloCode,
                CloDescription = dto.CloDescription
            };

            _questionDal.Add(entity);
            await _questionDal.SaveChangesAsync();

            var saved = await _questionDal.GetByIdWithSurveyAsync(entity.Id);
            return _mapper.Map<SurveyQuestionDto>(saved!);
        }

        public async Task<SurveyQuestionDto> UpdateQuestionAsync(UpdateSurveyQuestionDto dto, int externalTeacherId)
        {
            var question = await _questionDal.GetByIdWithSurveyAsync(dto.Id)
                ?? throw new KeyNotFoundException("Soru bulunamadı.");
            await VerifyOfferingOwnershipAsync(question.Survey.ExternalCourseOfferingId, externalTeacherId);
            ValidateScale(dto.ScaleMin, dto.ScaleMax);

            var tracked = await _questionDal.GetAsync(q => q.Id == dto.Id)
                ?? throw new KeyNotFoundException("Soru bulunamadı.");

            tracked.Text = dto.Text;
            tracked.OrderIndex = dto.OrderIndex;
            tracked.IsRequired = dto.IsRequired;
            tracked.ScaleMin = dto.ScaleMin;
            tracked.ScaleMax = dto.ScaleMax;
            tracked.ExternalCloId = dto.ExternalCloId;
            tracked.CloCode = dto.CloCode;
            tracked.CloDescription = dto.CloDescription;

            _questionDal.Update(tracked);
            await _questionDal.SaveChangesAsync();

            var saved = await _questionDal.GetByIdWithSurveyAsync(tracked.Id);
            return _mapper.Map<SurveyQuestionDto>(saved!);
        }

        public async Task DeleteQuestionAsync(Guid questionId, int externalTeacherId)
        {
            var question = await _questionDal.GetByIdWithSurveyAsync(questionId)
                ?? throw new KeyNotFoundException("Soru bulunamadı.");
            await VerifyOfferingOwnershipAsync(question.Survey.ExternalCourseOfferingId, externalTeacherId);

            var tracked = await _questionDal.GetAsync(q => q.Id == questionId)
                ?? throw new KeyNotFoundException("Soru bulunamadı.");

            _questionDal.Delete(tracked);
            await _questionDal.SaveChangesAsync();
        }

        // ── Sonuçlar ───────────────────────────────────────────────────────────────

        public async Task<SurveyResultsDto> GetResultsAsync(Guid surveyId, int externalTeacherId, string universityToken)
        {
            var survey = await _surveyDal.GetByIdWithDetailsAsync(surveyId)
                ?? throw new KeyNotFoundException("Anket bulunamadı.");
            await VerifyOfferingOwnershipAsync(survey.ExternalCourseOfferingId, externalTeacherId);

            // 1. Üniversite API'den kayıtlı öğrenci sayısını al
            var students = await _universityApi.GetStudentsForOfferingAsync(
                externalTeacherId, survey.ExternalCourseOfferingId, universityToken);
            var enrolledCount = students?.Count ?? 0;

            // 2. Geçen öğrenci ID'leri (MÜDEK hesaplanmışsa)
            var passingIds = await _studentEvalDal.GetPassingStudentIdsAsync(survey.ExternalCourseOfferingId);
            bool filterApplied = passingIds.Count > 0;

            // 3. Tüm gönderimler
            var allSubmissions = await _submissionDal.GetBySurveyIdWithAnswersAsync(surveyId);
            var submissions = filterApplied
                ? allSubmissions.Where(s => passingIds.Contains(s.ExternalStudentId)).ToList()
                : allSubmissions.ToList();

            var questions = survey.Questions.OrderBy(q => q.OrderIndex).ToList();

            // 4. Soru bazlı hesaplama
            var questionResults = questions.Select(q =>
            {
                var nonZeroAnswers = submissions
                    .SelectMany(s => s.Answers)
                    .Where(a => a.QuestionId == q.Id
                             && a.ValueNumeric.HasValue
                             && a.ValueNumeric.Value > 0)
                    .ToList();

                var distribution = new Dictionary<int, int>();
                for (int i = q.ScaleMin; i <= q.ScaleMax; i++)
                    distribution[i] = 0;

                var allAnswers = submissions
                    .SelectMany(s => s.Answers)
                    .Where(a => a.QuestionId == q.Id && a.ValueNumeric.HasValue);

                foreach (var a in allAnswers)
                {
                    var val = (int)Math.Round(a.ValueNumeric!.Value);
                    if (distribution.ContainsKey(val))
                        distribution[val]++;
                }

                decimal? avg = nonZeroAnswers.Count > 0
                    ? Math.Round((decimal)nonZeroAnswers.Average(a => (double)a.ValueNumeric!.Value), 2)
                    : null;

                decimal? pct = avg.HasValue
                    ? Math.Round(avg.Value / q.ScaleMax * 100m, 2)
                    : null;

                return new SurveyQuestionResultDto
                {
                    QuestionId = q.Id,
                    Text = q.Text,
                    OrderIndex = q.OrderIndex,
                    ExternalCloId = q.ExternalCloId,
                    CloCode = q.CloCode,
                    ResponseCount = nonZeroAnswers.Count,
                    AverageScore = avg,
                    ScorePercentage = pct,
                    ScoreDistribution = distribution
                };
            }).ToList();

            // 5. DÖÇ bazlı hesaplama
            var cloResults = new List<CloSurveyResultDto>();

            var cloGroups = questions
                .Where(q => q.ExternalCloId.HasValue)
                .GroupBy(q => q.ExternalCloId!.Value)
                .ToList();

            var mudekRows = await _cloEvalDal.GetCombinedByOfferingAsync(survey.ExternalCourseOfferingId);

            foreach (var group in cloGroups)
            {
                var cloId = group.Key;
                var firstQ = group.First();

                var questionPcts = group
                    .Select(q => questionResults.FirstOrDefault(r => r.QuestionId == q.Id)?.ScorePercentage)
                    .Where(p => p.HasValue)
                    .Select(p => p!.Value)
                    .ToList();

                decimal? surveyScore = questionPcts.Count > 0
                    ? Math.Round(questionPcts.Average(), 2)
                    : null;

                var mudekRow = mudekRows.FirstOrDefault(m => m.ExternalCloId == cloId);
                decimal? mudekScore = mudekRow?.AchievementScore.HasValue == true
                    ? Math.Round(mudekRow.AchievementScore!.Value * 100m, 2)
                    : null;

                decimal? diff = (surveyScore.HasValue && mudekScore.HasValue)
                    ? Math.Round(surveyScore.Value - mudekScore.Value, 2)
                    : null;

                string? evaluation = diff.HasValue
                    ? diff.Value switch
                    {
                        var d when Math.Abs(d) <= 10m => "Uyumlu",
                        var d when d > 10m => "Anket Yüksek",
                        _ => "Anket Düşük"
                    }
                    : null;

                cloResults.Add(new CloSurveyResultDto
                {
                    ExternalCloId = cloId,
                    CloCode = firstQ.CloCode,
                    CloDescription = firstQ.CloDescription,
                    QuestionCount = group.Count(),
                    SurveyScore = surveyScore,
                    MudekScore = mudekScore,
                    Difference = diff,
                    Evaluation = evaluation
                });

                if (mudekRow != null && surveyScore.HasValue)
                {
                    mudekRow.SurveyScore = surveyScore.Value / 100m;
                    mudekRow.SurveyDifference = diff.HasValue ? diff.Value / 100m : null;
                    _cloEvalDal.Update(mudekRow);
                }
            }

            if (cloGroups.Count > 0)
                await _cloEvalDal.SaveChangesAsync();

            return new SurveyResultsDto
            {
                SurveyId = surveyId,
                Title = survey.Title,
                EnrolledStudentCount = enrolledCount,
                TotalSubmissions = allSubmissions.Count,
                EvaluatedSubmissions = submissions.Count,
                NotParticipatedCount = enrolledCount - allSubmissions.Count,
                SubmittedButExcludedCount = allSubmissions.Count - submissions.Count,
                IsPassingFilterApplied = filterApplied,
                Questions = questionResults,
                CloResults = cloResults
            };
        }

        // ── Yardımcı metotlar ──────────────────────────────────────────────────────

        private async Task VerifyOfferingOwnershipAsync(int externalCourseOfferingId, int externalTeacherId)
        {
            var evaluation = await _courseEvalDal.GetByOfferingIdAsync(externalCourseOfferingId);
            if (evaluation == null)
                throw new KeyNotFoundException("Bu ders açılışına ait değerlendirme bulunamadı.");
            if (evaluation.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu ders açılışı size ait değil.");
        }

        private static void ValidateScale(int min, int max)
        {
            if (min < 0 || max > 10 || min >= max)
                throw new InvalidOperationException("Geçersiz Likert ölçeği. min ≥ 0, max ≤ 10 ve min < max olmalıdır.");
        }
    }
}
