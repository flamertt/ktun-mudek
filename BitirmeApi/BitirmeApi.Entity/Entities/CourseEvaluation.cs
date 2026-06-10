using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// MÜDEK ders değerlendirme belgesi.
    /// Üniversite API'sindeki CourseOffering'e bağlıdır (ExternalCourseOfferingId ile).
    /// Unique: (ExternalCourseOfferingId)
    /// </summary>
    public class CourseEvaluation : IEntity
    {
        public CourseEvaluation()
        {
            Exams = new List<Exam>();
            ProgramOutcomeContributions = new List<ProgramOutcomeContribution>();
        }

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        /// <summary>Üniversite API CourseOffering ID (int)</summary>
        [Required]
        public int ExternalCourseOfferingId { get; set; }

        /// <summary>Üniversite API Course ID</summary>
        [Required]
        public int ExternalCourseId { get; set; }

        /// <summary>Üniversite API Program ID</summary>
        public int ExternalProgramId { get; set; }

        /// <summary>Üniversite API Teacher (user) ID</summary>
        public int ExternalTeacherId { get; set; }

        /// <summary>Ders kodu (denormalized görüntü için)</summary>
        [MaxLength(64)]
        public string? CourseCode { get; set; }

        /// <summary>Ders adı (denormalized)</summary>
        [MaxLength(256)]
        public string? CourseName { get; set; }

        /// <summary>Akademik dönem adı (denormalized)</summary>
        [MaxLength(128)]
        public string? AcademicTermName { get; set; }

        public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        public DateTime? LastCalculatedAt { get; set; }
        public bool IsCalculationDirty { get; set; } = true;

        public string? StudentFeedbackEvaluation { get; set; }
        public string? ProgramOutcomeEvaluation { get; set; }
        public string? GeneralEvaluation { get; set; }
        public string? ImprovementSuggestions { get; set; }

        public ICollection<Exam> Exams { get; set; }
        public ICollection<ProgramOutcomeContribution> ProgramOutcomeContributions { get; set; }
    }
}
