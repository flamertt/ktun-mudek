using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Yerel CLO kaydı ile üniversite program çıktısı (PO) arasındaki ağırlıklı eşleme.
    /// Weight: 0.0 – 1.0 arası.
    /// </summary>
    public class CourseCloPloMap : IEntity
    {
        [Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public int Id { get; set; }

        public int CourseCloId { get; set; }

        [ForeignKey(nameof(CourseCloId))]
        public CourseClo CourseClo { get; set; } = default!;

        /// <summary>Üniversite API'sindeki program çıktısı (PO) ID'si.</summary>
        public int ExternalPloId { get; set; }

        /// <summary>PO kodu (görüntüleme için, üniversite API'sinden çekilir).</summary>
        [MaxLength(64)]
        public string? PloCode { get; set; }

        /// <summary>CLO'nun bu PO'ya katkı ağırlığı (0.0 – 1.0).</summary>
        public double Weight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
