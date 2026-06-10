using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class AssessmentComponentOutcomeMappingService : IAssessmentComponentOutcomeMappingService
    {
        private readonly IAssessmentComponentOutcomeMappingDal _mappingDal;
        private readonly IAssessmentComponentDal _componentDal;
        private readonly IMudekEvaluationCalculatorService _mudekStale;

        public AssessmentComponentOutcomeMappingService(
            IAssessmentComponentOutcomeMappingDal mappingDal,
            IAssessmentComponentDal componentDal,
            IMudekEvaluationCalculatorService mudekStale)
        {
            _mappingDal = mappingDal;
            _componentDal = componentDal;
            _mudekStale = mudekStale;
        }

        public async Task<List<AssessmentComponentOutcomeMappingDto>> GetByComponentIdForTeacherAsync(Guid componentId, int externalTeacherId)
        {
            await EnsureComponentOwnershipAsync(componentId, externalTeacherId);
            var items = await _mappingDal.GetByComponentIdWithDetailsAsync(componentId);
            return items.Select(MapToDto).ToList();
        }

        public async Task<AssessmentComponentOutcomeMappingDto> AddForTeacherAsync(CreateAssessmentComponentOutcomeMappingDto createDto, int externalTeacherId)
        {
            var component = await EnsureComponentOwnershipAsync(createDto.AssessmentComponentId, externalTeacherId);
            if (createDto.Weight < 0 || createDto.Weight > 1)
                throw new InvalidOperationException("Weight değeri 0 ile 1 arasında olmalıdır.");
            if (await _mappingDal.ExistsAsync(createDto.AssessmentComponentId, createDto.ExternalCloId))
                throw new InvalidOperationException("Bu component için CLO eşlemesi zaten var.");

            var entity = new AssessmentComponentOutcomeMapping
            {
                Id = Guid.NewGuid(),
                AssessmentComponentId = createDto.AssessmentComponentId,
                ExternalCloId = createDto.ExternalCloId,
                CloCode = createDto.CloCode,
                CloDescription = createDto.CloDescription,
                Weight = createDto.Weight,
                CreatedAt = DateTime.UtcNow
            };
            _mappingDal.Add(entity);
            await _mappingDal.SaveChangesAsync();
            await _mudekStale.MarkStaleByExamIdAsync(component.ExamId);
            return MapToDto(entity);
        }

        public async Task<AssessmentComponentOutcomeMappingDto> UpdateForTeacherAsync(UpdateAssessmentComponentOutcomeMappingDto updateDto, int externalTeacherId)
        {
            var snapshot = await _mappingDal.GetByIdWithOwnershipAsync(updateDto.Id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            VerifyEvaluationOwnership(snapshot.AssessmentComponent?.Exam?.CourseEvaluation);
            if (updateDto.Weight < 0 || updateDto.Weight > 1)
                throw new InvalidOperationException("Weight değeri 0 ile 1 arasında olmalıdır.");

            var tracked = await _mappingDal.GetAsync(m => m.Id == updateDto.Id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            tracked.Weight = updateDto.Weight;
            _mappingDal.Update(tracked);
            await _mappingDal.SaveChangesAsync();
            if (snapshot.AssessmentComponent != null)
                await _mudekStale.MarkStaleByExamIdAsync(snapshot.AssessmentComponent.ExamId);
            return MapToDto(tracked);
        }

        public async Task DeleteForTeacherAsync(Guid id, int externalTeacherId)
        {
            var snapshot = await _mappingDal.GetByIdWithOwnershipAsync(id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            VerifyEvaluationOwnership(snapshot.AssessmentComponent?.Exam?.CourseEvaluation);
            var examId = snapshot.AssessmentComponent?.ExamId;
            var tracked = await _mappingDal.GetAsync(m => m.Id == id)
                ?? throw new KeyNotFoundException("Mapping bulunamadı.");
            _mappingDal.Delete(tracked);
            await _mappingDal.SaveChangesAsync();
            if (examId.HasValue) await _mudekStale.MarkStaleByExamIdAsync(examId.Value);
        }

        private async Task<AssessmentComponent> EnsureComponentOwnershipAsync(Guid componentId, int externalTeacherId)
        {
            var component = await _componentDal.GetByIdWithOwnershipAsync(componentId)
                ?? throw new KeyNotFoundException("Component bulunamadı.");
            VerifyEvaluationOwnership(component.Exam?.CourseEvaluation);
            return component;
        }

        private static void VerifyEvaluationOwnership(CourseEvaluation? evaluation)
        {
            if (evaluation == null)
                throw new UnauthorizedAccessException("Bu işlem için yetkiniz yok.");
        }

        private static AssessmentComponentOutcomeMappingDto MapToDto(AssessmentComponentOutcomeMapping m) =>
            new AssessmentComponentOutcomeMappingDto
            {
                Id = m.Id,
                AssessmentComponentId = m.AssessmentComponentId,
                ExternalCloId = m.ExternalCloId,
                CloCode = m.CloCode,
                CloDescription = m.CloDescription,
                Weight = m.Weight,
                CreatedAt = m.CreatedAt
            };
    }
}
