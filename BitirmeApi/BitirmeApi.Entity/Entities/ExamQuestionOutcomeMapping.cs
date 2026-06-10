using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Sınav sorusunun CLO'lara ağırlıklı eşlemesi.
    /// CLO, üniversite API'sinden int ID ya da yerel CourseClo.Id ile temsil edilir.
    /// Unique: (ExamQuestionId, ExternalCloId, CloSource)
    /// </summary>
    public class ExamQuestionOutcomeMapping : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid ExamQuestionId { get; set; }

        [ForeignKey("ExamQuestionId")]
        public ExamQuestion ExamQuestion { get; set; } = default!;

        /// <summary>CLO ID: CloSource="api" → üniversite API, CloSource="db" → CourseClo.Id.</summary>
        [Required]
        public int ExternalCloId { get; set; }

        /// <summary>CLO kaynağı: "api" | "db". null ise legacy kayıt, "api" kabul edilir.</summary>
        [MaxLength(8)]
        public string? CloSource { get; set; }

        /// <summary>CLO kodu (denormalized görüntü için).</summary>
        [MaxLength(64)]
        public string? CloCode { get; set; }

        /// <summary>CLO açıklaması (denormalized).</summary>
        [MaxLength(2000)]
        public string? CloDescription { get; set; }

        [Required]
        public decimal Weight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
