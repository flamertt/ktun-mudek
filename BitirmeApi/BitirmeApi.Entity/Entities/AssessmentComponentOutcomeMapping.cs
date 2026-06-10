using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Değerlendirme bileşeninin CLO'lara ağırlıklı eşlemesi.
    /// Unique: (AssessmentComponentId, ExternalCloId)
    /// </summary>
    public class AssessmentComponentOutcomeMapping : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid AssessmentComponentId { get; set; }

        [ForeignKey("AssessmentComponentId")]
        public AssessmentComponent AssessmentComponent { get; set; } = default!;

        /// <summary>Üniversite API CLO ID (int)</summary>
        [Required]
        public int ExternalCloId { get; set; }

        [MaxLength(64)]
        public string? CloCode { get; set; }

        [MaxLength(2000)]
        public string? CloDescription { get; set; }

        [Required]
        public decimal Weight { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    }
}
