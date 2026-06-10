using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Bölüm L ve M: DÖÇ bazlı öğretim üyesi notları ve iyileştirme önerileri.
    /// Unique: (SemesterReportId, ExternalCloId)
    /// </summary>
    public class SemesterReportCloNote : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SemesterReportId { get; set; }

        /// <summary>Üniversite API CLO ID (ExternalCloId)</summary>
        public int ExternalCloId { get; set; }

        /// <summary>DÖÇ kodu (ör: "DÖÇ1")</summary>
        [MaxLength(64)]
        public string? CloCode { get; set; }

        /// <summary>DÖÇ açıklaması (denormalized)</summary>
        [MaxLength(2000)]
        public string? CloDescription { get; set; }

        /// <summary>Öğretim üyesi yorumu (ölçme sonucuna göre)</summary>
        public string? TeacherNote { get; set; }

        /// <summary>İyileştirme önerisi</summary>
        public string? ImprovementSuggestion { get; set; }

        // Navigation
        public SemesterReport? SemesterReport { get; set; }
    }
}
