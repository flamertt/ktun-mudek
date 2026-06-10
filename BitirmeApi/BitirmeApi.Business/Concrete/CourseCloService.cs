using BitirmeApi.Business.Abstract;
using BitirmeApi.Business.DTO;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.Concrete
{
    public class CourseCloService : ICourseCloService
    {
        private readonly ICourseCloDAL _dal;

        public CourseCloService(ICourseCloDAL dal) => _dal = dal;

        public async Task<List<CourseCloDto>> GetByExternalCourseIdAsync(int externalCourseId) =>
            (await _dal.GetByExternalCourseIdAsync(externalCourseId)).Select(Map).ToList();

        public async Task<CourseCloDto?> GetByIdAsync(int id)
        {
            var entity = await _dal.GetByIdWithMapsAsync(id);
            return entity == null ? null : Map(entity);
        }

        public async Task<CourseCloDto> CreateAsync(CreateCourseCloDto dto)
        {
            var entity = new CourseClo
            {
                ExternalCourseId = dto.ExternalCourseId,
                ExternalProgramId = dto.ExternalProgramId,
                Code = dto.Code?.Trim(),
                Description = dto.Description.Trim(),
                OrderIndex = dto.OrderIndex,
                IsActive = true,
                CreatedAt = DateTime.UtcNow
            };
            _dal.AddNew(entity);
            await _dal.SaveChangesAsync();
            return Map(entity);
        }

        public async Task<CourseCloDto> UpdateAsync(UpdateCourseCloDto dto)
        {
            var entity = await _dal.GetByIdWithMapsAsync(dto.Id)
                ?? throw new KeyNotFoundException($"CLO {dto.Id} bulunamadı.");

            entity.Code = dto.Code?.Trim();
            entity.Description = dto.Description.Trim();
            entity.OrderIndex = dto.OrderIndex;
            entity.IsActive = dto.IsActive;
            _dal.UpdateNew(entity);
            await _dal.SaveChangesAsync();
            return Map(entity);
        }

        public async Task DeleteAsync(int id)
        {
            var entity = await _dal.GetByIdWithMapsAsync(id)
                ?? throw new KeyNotFoundException($"CLO {id} bulunamadı.");
            _dal.DeleteNew(entity);
            await _dal.SaveChangesAsync();
        }

        private static CourseCloDto Map(CourseClo e) => new()
        {
            Id = e.Id,
            ExternalCourseId = e.ExternalCourseId,
            ExternalProgramId = e.ExternalProgramId,
            Code = e.Code,
            Description = e.Description,
            OrderIndex = e.OrderIndex,
            IsActive = e.IsActive,
            CreatedAt = e.CreatedAt
        };
    }
}
