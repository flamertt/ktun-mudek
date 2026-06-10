using AutoMapper;
using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.Business.Integration.Abstract;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class CourseEvaluationService : ICourseEvaluationService
    {
        private readonly ICourseEvaluationDal _evalDal;
        private readonly IUniversityApiService _universityApi;
        private readonly IMapper _mapper;

        public CourseEvaluationService(
            ICourseEvaluationDal evalDal,
            IUniversityApiService universityApi,
            IMapper mapper)
        {
            _evalDal = evalDal;
            _universityApi = universityApi;
            _mapper = mapper;
        }

        public async Task<List<CourseEvaluationListDto>> GetAllAsync()
        {
            var list = await _evalDal.GetAllWithDetailsAsync();
            return _mapper.Map<List<CourseEvaluationListDto>>(list);
        }

        public async Task<CourseEvaluationDetailDto?> GetByIdAsync(Guid id)
        {
            var entity = await _evalDal.GetByIdWithDetailsAsync(id);
            return entity != null ? _mapper.Map<CourseEvaluationDetailDto>(entity) : null;
        }

        public async Task<CourseEvaluationDetailDto?> GetByOfferingIdAsync(int externalCourseOfferingId)
        {
            var entity = await _evalDal.GetByOfferingIdAsync(externalCourseOfferingId);
            return entity != null ? _mapper.Map<CourseEvaluationDetailDto>(entity) : null;
        }

        public async Task<List<CourseEvaluationListDto>> GetByTeacherIdAsync(int externalTeacherId)
        {
            var list = await _evalDal.GetByTeacherIdAsync(externalTeacherId);
            return _mapper.Map<List<CourseEvaluationListDto>>(list);
        }

        public async Task<CourseEvaluationDetailDto> CreateForTeacherAsync(CourseEvaluationCreateDto dto, int externalTeacherId, string universityToken)
        {
            // Üniversite API'sinden offering'i doğrula
            var offeringDetail = await _universityApi.GetCourseOfferingDetailAsync(externalTeacherId, dto.ExternalCourseOfferingId, universityToken);
            if (offeringDetail == null)
                throw new KeyNotFoundException("Ders açılışı üniversite sisteminde bulunamadı.");

            // Zaten değerlendirme var mı?
            var existing = await _evalDal.GetByOfferingIdAsync(dto.ExternalCourseOfferingId);
            if (existing != null)
                throw new InvalidOperationException("Bu ders açılışı için zaten bir değerlendirme mevcut.");

            var entity = new CourseEvaluation
            {
                Id = Guid.NewGuid(),
                ExternalCourseOfferingId = dto.ExternalCourseOfferingId,
                ExternalCourseId = dto.ExternalCourseId > 0 ? dto.ExternalCourseId : offeringDetail.CourseId,
                ExternalProgramId = dto.ExternalProgramId > 0 ? dto.ExternalProgramId : offeringDetail.ProgramId,
                ExternalTeacherId = externalTeacherId,
                CourseCode = dto.CourseCode ?? offeringDetail.CourseCode,
                CourseName = dto.CourseName ?? offeringDetail.CourseName,
                AcademicTermName = dto.AcademicTermName,
                CreatedDate = DateTime.UtcNow,
                StudentFeedbackEvaluation = dto.StudentFeedbackEvaluation,
                ProgramOutcomeEvaluation = dto.ProgramOutcomeEvaluation,
                GeneralEvaluation = dto.GeneralEvaluation,
                ImprovementSuggestions = dto.ImprovementSuggestions
            };

            _evalDal.Add(entity);
            await _evalDal.SaveChangesAsync();

            var result = await _evalDal.GetByIdWithDetailsAsync(entity.Id);
            return _mapper.Map<CourseEvaluationDetailDto>(result!);
        }

        public async Task<CourseEvaluationDetailDto> UpdateForTeacherAsync(CourseEvaluationUpdateDto dto, int externalTeacherId)
        {
            var entity = await _evalDal.GetByIdWithDetailsAsync(dto.Id)
                ?? throw new KeyNotFoundException("Değerlendirme bulunamadı.");

            if (entity.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu değerlendirmeyi güncelleme yetkiniz yok.");

            entity.StudentFeedbackEvaluation = dto.StudentFeedbackEvaluation;
            entity.ProgramOutcomeEvaluation = dto.ProgramOutcomeEvaluation;
            entity.GeneralEvaluation = dto.GeneralEvaluation;
            entity.ImprovementSuggestions = dto.ImprovementSuggestions;
            entity.UpdatedAt = DateTime.UtcNow;

            _evalDal.Update(entity);
            await _evalDal.SaveChangesAsync();

            var result = await _evalDal.GetByIdWithDetailsAsync(entity.Id);
            return _mapper.Map<CourseEvaluationDetailDto>(result!);
        }

        public async Task DeleteForTeacherAsync(Guid id, int externalTeacherId)
        {
            var entity = await _evalDal.GetByIdWithDetailsAsync(id)
                ?? throw new KeyNotFoundException("Değerlendirme bulunamadı.");

            if (entity.ExternalTeacherId != externalTeacherId)
                throw new UnauthorizedAccessException("Bu değerlendirmeyi silme yetkiniz yok.");

            _evalDal.Delete(entity);
            await _evalDal.SaveChangesAsync();
        }
    }
}
