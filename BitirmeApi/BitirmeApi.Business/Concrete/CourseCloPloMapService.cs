using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class CourseCloPloMapService : ICourseCloPloMapService
    {
        private readonly ICourseCloPloMapDal _dal;
        private readonly ICourseCloDAL _cloDal;

        public CourseCloPloMapService(ICourseCloPloMapDal dal, ICourseCloDAL cloDal)
        {
            _dal = dal;
            _cloDal = cloDal;
        }

        public async Task<List<CourseCloPloMapDto>> GetByCloIdAsync(int courseCloId) =>
            (await _dal.GetByCloIdAsync(courseCloId)).Select(Map).ToList();

        public async Task<List<CourseCloPloMapDto>> GetByExternalCourseIdAsync(int externalCourseId) =>
            (await _dal.GetByExternalCourseIdAsync(externalCourseId)).Select(m => MapWithClo(m)).ToList();

        public async Task<CourseCloPloMapDto?> GetByIdAsync(int id)
        {
            var entity = await _dal.GetByIdWithCloAsync(id);
            return entity == null ? null : MapWithClo(entity);
        }

        public async Task<CourseCloPloMapDto> CreateAsync(CreateCourseCloPloMapDto dto)
        {
            if (!await _cloDal.ExistsAsync(dto.CourseCloId))
                throw new KeyNotFoundException($"CourseClo {dto.CourseCloId} bulunamadı.");

            if (await _dal.ExistsAsync(dto.CourseCloId, dto.ExternalPloId))
                throw new InvalidOperationException("Bu CLO–PO eşlemesi zaten mevcut.");

            var entity = new CourseCloPloMap
            {
                CourseCloId = dto.CourseCloId,
                ExternalPloId = dto.ExternalPloId,
                PloCode = dto.PloCode?.Trim(),
                Weight = dto.Weight,
                CreatedAt = DateTime.UtcNow
            };
            _dal.AddNew(entity);
            await _dal.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<CourseCloPloMapDto> UpdateAsync(UpdateCourseCloPloMapDto dto)
        {
            var entity = await _dal.GetByIdWithCloAsync(dto.Id)
                ?? throw new KeyNotFoundException($"CLO–PO eşleme {dto.Id} bulunamadı.");

            entity.PloCode = dto.PloCode?.Trim();
            entity.Weight = dto.Weight;
            _dal.UpdateNew(entity);
            await _dal.SaveChangesAsync();
            return MapWithClo(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dal.GetByIdWithCloAsync(id)
                ?? throw new KeyNotFoundException($"CLO–PO eşleme {id} bulunamadı.");
            _dal.DeleteNew(entity);
            await _dal.SaveChangesAsync();
        }

        private static CourseCloPloMapDto Map(CourseCloPloMap e) => new()
        {
            Id = e.Id,
            CourseCloId = e.CourseCloId,
            ExternalPloId = e.ExternalPloId,
            PloCode = e.PloCode,
            Weight = e.Weight,
            CreatedAt = e.CreatedAt
        };

        private static CourseCloPloMapDto MapWithClo(CourseCloPloMap e)
        {
            var dto = Map(e);
            if (e.CourseClo != null)
            {
                dto.CloCode = e.CourseClo.Code;
                dto.CloDescription = e.CourseClo.Description;
            }
            return dto;
        }
    }
}
