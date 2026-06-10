using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class ExamQuestionOutcomeMappingService : IExamQuestionOutcomeMappingService
    {
        private readonly IExamQuestionOutcomeMappingDal _mappingDal;
        private readonly IExamQuestionDal _questionDal;
        private readonly IMudekEvaluationCalculatorService _mudekStale;

        public ExamQuestionOutcomeMappingService(
            IExamQuestionOutcomeMappingDal mappingDal,
            IExamQuestionDal questionDal,
            IMudekEvaluationCalculatorService mudekStale)
        {
            _mappingDal = mappingDal;
            _questionDal = questionDal;
            _mudekStale = mudekStale;
        }

        public async Task<List<ExamQuestionOutcomeMappingDto>> GetByQuestionIdForTeacherAsync(Guid questionId, int externalTeacherId)
        {
            await VerifyQuestionOwnershipAsync(questionId, externalTeacherId);
            var items = await _mappingDal.GetByQuestionIdWithDetailsAsync(questionId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<ExamQuestionOutcomeMappingDto> AddForTeacherAsync(CreateExamQuestionOutcomeMappingDto createDto, int externalTeacherId)
        {
            var question = await VerifyQuestionOwnershipAsync(createDto.ExamQuestionId, externalTeacherId);
            if (createDto.Weight < 0 || createDto.Weight > 1)
                throw new InvalidOperationException("Weight değeri 0 ile 1 arasında olmalıdır.");
            if (await _mappingDal.ExistsAsync(createDto.ExamQuestionId, createDto.ExternalCloId))
                throw new InvalidOperationException("Bu soru için CLO eşlemesi zaten mevcut.");

            var entity = new ExamQuestionOutcomeMapping
            {
                Id = Guid.NewGuid(),
                ExamQuestionId = createDto.ExamQuestionId,
                ExternalCloId = createDto.ExternalCloId,
                CloCode = createDto.CloCode,
                CloDescription = createDto.CloDescription,
                Weight = createDto.Weight,
                CreatedAt = DateTime.UtcNow
            };
            _mappingDal.Add(entity);
            await _mappingDal.SaveChangesAsync();
            await _mudekStale.MarkStaleByExamIdAsync(question.ExamId);
            return MapToDto(entity);
        }

        public async Task<ExamQuestionOutcomeMappingDto> UpdateForTeacherAsync(UpdateExamQuestionOutcomeMappingDto updateDto, int externalTeacherId)
        {
            var existing = await _mappingDal.GetByIdWithOwnershipAsync(updateDto.Id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            VerifyEvaluationOwnership(existing.ExamQuestion?.Exam?.CourseEvaluation);
            if (updateDto.Weight < 0 || updateDto.Weight > 1)
                throw new InvalidOperationException("Weight değeri 0 ile 1 arasında olmalıdır.");

            var tracked = await _mappingDal.GetAsync(m => m.Id == updateDto.Id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            tracked.Weight = updateDto.Weight;
            _mappingDal.Update(tracked);
            await _mappingDal.SaveChangesAsync();

            if (existing.ExamQuestion?.ExamId != null)
                await _mudekStale.MarkStaleByExamIdAsync(existing.ExamQuestion.ExamId);
            return MapToDto(tracked);
        }

        public async Task DeleteForTeacherAsync(Guid id, int externalTeacherId)
        {
            var existing = await _mappingDal.GetByIdWithOwnershipAsync(id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            VerifyEvaluationOwnership(existing.ExamQuestion?.Exam?.CourseEvaluation);
            var examId = existing.ExamQuestion?.ExamId;
            var tracked = await _mappingDal.GetAsync(m => m.Id == id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            _mappingDal.Delete(tracked);
            await _mappingDal.SaveChangesAsync();
            if (examId.HasValue) await _mudekStale.MarkStaleByExamIdAsync(examId.Value);
        }

        private async Task<ExamQuestion> VerifyQuestionOwnershipAsync(Guid questionId, int externalTeacherId)
        {
            var question = await _questionDal.GetByIdWithOwnershipAsync(questionId)
                ?? throw new KeyNotFoundException("Soru bulunamadı.");
            VerifyEvaluationOwnership(question.Exam?.CourseEvaluation);
            return question;
        }

        private static void VerifyEvaluationOwnership(CourseEvaluation? evaluation)
        {
            if (evaluation == null)
                throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
        }

        private static ExamQuestionOutcomeMappingDto MapToDto(ExamQuestionOutcomeMapping m) =>
            new ExamQuestionOutcomeMappingDto
            {
                Id = m.Id,
                ExamQuestionId = m.ExamQuestionId,
                ExternalCloId = m.ExternalCloId,
                CloCode = m.CloCode,
                CloDescription = m.CloDescription,
                Weight = m.Weight,
                CreatedAt = m.CreatedAt
            };
    }
}
