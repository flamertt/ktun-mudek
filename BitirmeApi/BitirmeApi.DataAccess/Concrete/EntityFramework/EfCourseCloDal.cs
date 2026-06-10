using BitirmeApi.Core.EntityFramework;
using BitirmeApi.DataAccess.Abstract;
using BitirmeApi.DataAccess.Concrete.EntityFramework.Context;
using BitirmeApi.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeApi.DataAccess.Concrete.EntityFramework
{
    public class EfCourseCloDal : EfRepository<CourseClo, ProjectDbContext>, ICourseCloDAL
    {
        public EfCourseCloDal(ProjectDbContext context) : base(context) { }

        public async Task<List<CourseClo>> GetByExternalCourseIdAsync(int externalCourseId) =>
            await _context.CourseClos
                .AsNoTracking()
                .Where(c => c.ExternalCourseId == externalCourseId && c.IsActive)
                .OrderBy(c => c.OrderIndex)
                .ToListAsync();

        public async Task<CourseClo?> GetByIdWithMapsAsync(int id) =>
            await _context.CourseClos
                .Include(c => c.PloMaps)
                .FirstOrDefaultAsync(c => c.Id == id);

        public async Task<bool> ExistsAsync(int id) =>
            await _context.CourseClos.AnyAsync(c => c.Id == id);
    }
}
