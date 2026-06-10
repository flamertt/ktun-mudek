using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Bölüm K: PÇ bazlı öğretim üyesi notları ve iyileştirme önerileri.
    /// Unique: (SemesterReportId, ExternalPloId)
    /// </summary>
    public class SemesterReportPloNote : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SemesterReportId { get; set; }

        /// <summary>Üniversite API Program Outcome ID</summary>
        public int ExternalPloId { get; set; }

        /// <summary>PÇ kodu (ör: "PÇ1")</summary>
        [MaxLength(64)]
        public string? PloCode { get; set; }

        /// <summary>PÇ açıklaması (denormalized)</summary>
        [MaxLength(2000)]
        public string? PloDescription { get; set; }

        /// <summary>Öğretim üyesi yorumu (başarı oranına göre)</summary>
        public string? TeacherNote { get; set; }

        /// <summary>İyileştirme önerisi</summary>
        public string? ImprovementSuggestion { get; set; }

        // Navigation
        public SemesterReport? SemesterReport { get; set; }
    }
}
