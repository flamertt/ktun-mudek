using BitirmeApi.Core;
using BitirmeApi.Entity.Entities;

namespace BitirmeApi.DataAccess.Abstract
{
    public interface ICourseCloPloMapDal : IRepository<CourseCloPloMap>
    {
        Task<List<CourseCloPloMap>> GetByCloIdAsync(int courseCloId);
        Task<List<CourseCloPloMap>> GetByExternalCourseIdAsync(int externalCourseId);
        Task<CourseCloPloMap?> GetByIdWithCloAsync(int id);
        Task<bool> ExistsAsync(int courseCloId, int externalPloId, int? excludeId = null);
    }
}
