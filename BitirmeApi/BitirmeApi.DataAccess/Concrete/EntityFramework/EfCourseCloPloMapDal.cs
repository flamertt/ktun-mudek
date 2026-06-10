using BitirmeApi.Core.EntityFramework;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using BitirmeApi.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeApi.DataAccess.Concrete.EntityFramework
{
    public class EfCourseCloPloMapDal : EfRepository<CourseCloPloMap, ProjectDbContext>, ICourseCloPloMapDal
    {
        public EfCourseCloPloMapDal(ProjectDbContext context) : base(context) { }

        public async Task<List<CourseCloPloMap>> GetByCloIdAsync(int courseCloId) =>
            await _context.CourseCloPloMaps
                .AsNoTracking()
                .Where(m => m.CourseCloId == courseCloId)
                .ToListAsync();

        public async Task<List<CourseCloPloMap>> GetByExternalCourseIdAsync(int externalCourseId) =>
            await _context.CourseCloPloMaps
                .AsNoTracking()
                .Include(m => m.CourseClo)
                .Where(m => m.CourseClo.ExternalCourseId == externalCourseId && m.CourseClo.IsActive)
                .ToListAsync();

        public async Task<CourseCloPloMap?> GetByIdWithCloAsync(int id) =>
            await _context.CourseCloPloMaps
                .Include(m => m.CourseClo)
                .FirstOrDefaultAsync(m => m.Id == id);

        public async Task<bool> ExistsAsync(int courseCloId, int externalPloId, int? excludeId = null)
        {
            var q = _context.CourseCloPloMaps.Where(m => m.CourseCloId == courseCloId && m.ExternalPloId == externalPloId);
            if (excludeId.HasValue) q = q.Where(m => m.Id != excludeId.Value);
            return await q.AnyAsync();
        }
    }
}
