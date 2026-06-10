using BitirmeApi.Business.DTO;

namespace BitirmeApi.Business.Abstract
{
    public interface ICourseCloPloMapService
    {
        Task<List<CourseCloPloMapDto>> GetByCloIdAsync(int courseCloId);
        Task<List<CourseCloPloMapDto>> GetByExternalCourseIdAsync(int externalCourseId);
        Task<CourseCloPloMapDto?> GetByIdAsync(int id);
        Task<CourseCloPloMapDto> CreateAsync(CreateCourseCloPloMapDto dto);
        Task<CourseCloPloMapDto> UpdateAsync(UpdateCourseCloPloMapDto dto);
        Task DeleteAsync(int id);
    }
}
