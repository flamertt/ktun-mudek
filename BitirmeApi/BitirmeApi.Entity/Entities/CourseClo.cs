using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Üniversite API'sinden CLO verisi boş geldiğinde admin tarafından yönetilen yerel CLO kaydı.
    /// Id, ExternalCloId olarak kullanılır (int — üniversite API'siyle uyumlu).
    /// </summary>
    public class CourseClo : IEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        /// <summary>Üniversite sistemindeki ders kataloğu ID'si (courseId).</summary>
        public int ExternalCourseId { get; set; }

        /// <summary>Üniversite sistemindeki program ID'si. CLO–PO matrisinde PO listesini çekmek için kullanılır.</summary>
        public int ExternalProgramId { get; set; }

        /// <summary>CLO kodu (örn. "ÖÇ-1", "CLO-1").</summary>
        [MaxLength(64)]
        public string? Code { get; set; }

        /// <summary>CLO açıklaması.</summary>
        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; } = true;

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

        public ICollection<CourseCloPloMap> PloMaps { get; set; } = new List<CourseCloPloMap>();
    }
}
