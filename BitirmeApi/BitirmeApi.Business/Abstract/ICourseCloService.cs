using BitirmeApi.Business.DTO;

namespace BitirmeApi.Business.Abstract
{
    public interface ICourseCloService
    {
        Task<List<CourseCloDto>> GetByExternalCourseIdAsync(int externalCourseId);
        Task<CourseCloDto?> GetByIdAsync(int id);
        Task<CourseCloDto> CreateAsync(CreateCourseCloDto dto);
        Task<CourseCloDto> UpdateAsync(UpdateCourseCloDto dto);
        Task DeleteAsync(int id);
    }
}
