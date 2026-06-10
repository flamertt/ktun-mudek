using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// MÜDEK Dönem Sonu Ders Değerlendirme Raporu.
    /// Her CourseEvaluation için en fazla bir rapor oluşturulabilir.
    /// </summary>
    public class SemesterReport : IEntity
    {
        public SemesterReport()
        {
            WeeklyResources = new List<SemesterReportWeeklyResource>();
            Files = new List<SemesterReportFile>();
            CloNotes = new List<SemesterReportCloNote>();
            PloNotes = new List<SemesterReportPloNote>();
        }

        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid CourseEvaluationId { get; set; }

        public int ExternalCourseOfferingId { get; set; }
        public int ExternalTeacherId { get; set; }

        // Denormalized bilgiler (görüntüleme için)
        [MaxLength(64)]
        public string? CourseCode { get; set; }

        [MaxLength(256)]
        public string? CourseName { get; set; }

        [MaxLength(128)]
        public string? AcademicTermName { get; set; }

        [MaxLength(256)]
        public string? TeacherName { get; set; }

        [MaxLength(128)]
        public string? TeacherTitle { get; set; }

        public SemesterReportStatus Status { get; set; } = SemesterReportStatus.Draft;

        // ── Kurumsal bilgiler (Bölüm A başlığı) ──────────────────────────────────
        [MaxLength(256)]
        public string? UniversityName { get; set; }

        [MaxLength(256)]
        public string? FacultyName { get; set; }

        [MaxLength(256)]
        public string? DepartmentName { get; set; }

        // ── Bölüm A: Ders tanıtım bilgileri ──────────────────────────────────────
        [MaxLength(16)]
        public string? CourseSemester { get; set; }      // "1", "2" vb.

        [MaxLength(16)]
        public string? CourseCredit { get; set; }         // "4"

        [MaxLength(16)]
        public string? CourseEcts { get; set; }           // "6"

        public int? CourseTheoryHours { get; set; }       // 3

        public int? CoursePracticeHours { get; set; }     // 2

        public string? CourseObjective { get; set; }      // Dersin amacı

        public string? CourseContent { get; set; }        // Dersin içeriği

        public string? SectionANotes { get; set; }        // Ek notlar

        // ── Bölüm G: Anket sonuçları tartışma metni ──────────────────────────────
        public string? SectionGDiscussion { get; set; }

        // ── Bölüm H: Başarı istatistikleri yorumu ────────────────────────────────
        public string? SectionHCommentary { get; set; }

        // ── Bölüm İ: Öğretim üyesi genel değerlendirmesi (manuel) ───────────────
        public string? SectionIGeneralEvaluation { get; set; }

        // ── Bölüm J: Geçmiş dönemden farklı yapılan değişiklikler (manuel) ───────
        public string? SectionJChangesFromPrevious { get; set; }

        // ── Bölüm M: İyileştirme önerileri ───────────────────────────────────────
        public string? SectionMImprovement { get; set; }

        // ── İmza alanı ────────────────────────────────────────────────────────────
        [MaxLength(256)]
        public string? SignatureName { get; set; }

        public DateTime? SignatureDate { get; set; }

        public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
        public DateTime? UpdatedAt { get; set; }

        // Navigation
        public CourseEvaluation? CourseEvaluation { get; set; }
        public ICollection<SemesterReportWeeklyResource> WeeklyResources { get; set; }
        public ICollection<SemesterReportFile> Files { get; set; }
        public ICollection<SemesterReportCloNote> CloNotes { get; set; }
        public ICollection<SemesterReportPloNote> PloNotes { get; set; }
    }

    public enum SemesterReportStatus
    {
        /// <summary>Taslak — henüz tamamlanmamış</summary>
        Draft = 0,
        /// <summary>Hazır — MÜDEK komisyonuna gönderilebilir</summary>
        Ready = 1
    }
}
