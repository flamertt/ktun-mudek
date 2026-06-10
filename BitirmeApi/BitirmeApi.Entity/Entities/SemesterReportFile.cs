using BitirmeApi.Core.Entity;
using System.ComponentModel.DataAnnotations;
using System.ComponentModel.DataAnnotations.Schema;

namespace BitirmeApi.Entity.Entities
{
    /// <summary>
    /// Dönem sonu raporuna yüklenen dosyalar.
    /// Bölüm C (sınav kağıtları), D (diğer ölçme araçları), E (devam çizelgesi),
    /// F (OBS not listesi), G (anket Excel) gibi bölümler için.
    /// Dosyalar sunucuda fiziksel olarak saklanır; DB'de yalnızca metadata tutulur.
    /// </summary>
    public class SemesterReportFile : IEntity
    {
        [Required, Key, DatabaseGenerated(DatabaseGeneratedOption.Identity)]
        public Guid Id { get; set; }

        [Required]
        public Guid SemesterReportId { get; set; }

        /// <summary>Rapor bölümü kodu: "C", "D", "E", "F", "G"</summary>
        [Required, MaxLength(4)]
        public string SectionCode { get; set; } = string.Empty;

        /// <summary>
        /// Dosya kategorisi; örneğin:
        /// "ExamPaper" — sınav soruları
        /// "AnswerKey" — cevap anahtarı
        /// "StudentPaper_High" — en yüksek puanlı öğrenci sınav kağıdı
        /// "StudentPaper_Mid" — orta puanlı öğrenci sınav kağıdı
        /// "StudentPaper_Low" — en düşük puanlı öğrenci sınav kağıdı
        /// "AttendanceSheet" — devam çizelgesi
        /// "GradeList" — OBS başarı notları listesi
        /// "SurveyExcel" — anket Excel dosyası
        /// "OtherTool" — diğer ölçme aracı belgesi
        /// "Other" — diğer
        /// </summary>
        [Required, MaxLength(64)]
        public string FileCategory { get; set; } = string.Empty;

        /// <summary>İlgili sınav ID'si (Bölüm C için opsiyonel)</summary>
        public Guid? ExamId { get; set; }

        /// <summary>
        /// Bölüm C alt kategorisi için sınav türü etiketi.
        /// Örn: "Vize", "Final", "Bütünleme"
        /// </summary>
        [MaxLength(32)]
        public string? ExamTypeLabel { get; set; }

        /// <summary>Orijinal dosya adı</summary>
        [Required, MaxLength(512)]
        public string OriginalFileName { get; set; } = string.Empty;

        /// <summary>Sunucudaki göreli dosya yolu</summary>
        [Required, MaxLength(1024)]
        public string FilePath { get; set; } = string.Empty;

        /// <summary>MIME içerik türü (application/pdf, image/jpeg vb.)</summary>
        [MaxLength(128)]
        public string? ContentType { get; set; }

        /// <summary>Dosya boyutu (byte)</summary>
        public long FileSize { get; set; }

        /// <summary>Opsiyonel açıklama / not</summary>
        [MaxLength(500)]
        public string? Notes { get; set; }

        public DateTime UploadedAt { get; set; } = DateTime.UtcNow;

        // Navigation
        public SemesterReport? SemesterReport { get; set; }
    }
}
