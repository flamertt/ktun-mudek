using BitirmeApi.Core;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.DataAccess.Abstract
{
    public interface ICourseCloDAL : IRepository<CourseClo>
    {
        Task<List<CourseClo>> GetByExternalCourseIdAsync(int externalCourseId);
        Task<CourseClo?> GetByIdWithMapsAsync(int id);
        Task<bool> ExistsAsync(int id);
    }
}
