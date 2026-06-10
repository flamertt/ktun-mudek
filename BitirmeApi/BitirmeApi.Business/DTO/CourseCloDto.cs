using System.ComponentModel.DataAnnotations;

namespace BitirmeApi.Business.DTO
{
    // ── Çıktı DTOs ──────────────────────────────────────────────────────────────

    public class CourseCloDto
    {
        public int Id { get; set; }
        public int ExternalCourseId { get; set; }
        public int ExternalProgramId { get; set; }
        public string? Code { get; set; }
        public string Description { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }

        /// <summary>Her zaman "db". Üniversite API'sinden gelen CLO'larla karıştırılmaması için.</summary>
        public string SourceType => "db";

        /// <summary>Frontend canonical anahtarı: "db:{Id}". Soru–CLO eşlemesinde kullanılır.</summary>
        public string CloKey => $"db:{Id}";
    }

    public class CourseCloPloMapDto
    {
        public int Id { get; set; }
        public int CourseCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public int ExternalPloId { get; set; }
        public string? PloCode { get; set; }
        public double Weight { get; set; }
        public DateTime CreatedAt { get; set; }
    }

    // ── Giriş DTOs ──────────────────────────────────────────────────────────────

    public class CreateCourseCloDto
    {
        [Required]
        public int ExternalCourseId { get; set; }

        /// <summary>Üniversite programı ID'si. CLO–PO matrisinde PO listesini getirmek için gereklidir.</summary>
        [Required]
        public int ExternalProgramId { get; set; }

        [MaxLength(64)]
        public string? Code { get; set; }

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int OrderIndex { get; set; }
    }

    public class UpdateCourseCloDto
    {
        [Required]
        public int Id { get; set; }

        [MaxLength(64)]
        public string? Code { get; set; }

        [Required, MaxLength(2000)]
        public string Description { get; set; } = string.Empty;

        public int OrderIndex { get; set; }

        public bool IsActive { get; set; }
    }

    public class CreateCourseCloPloMapDto
    {
        [Required]
        public int CourseCloId { get; set; }

        [Required]
        public int ExternalPloId { get; set; }

        [MaxLength(64)]
        public string? PloCode { get; set; }

        /// <summary>0.0 – 1.0 arası.</summary>
        [Range(0.0, 1.0)]
        public double Weight { get; set; }
    }

    public class UpdateCourseCloPloMapDto
    {
        [Required]
        public int Id { get; set; }

        [MaxLength(64)]
        public string? PloCode { get; set; }

        [Range(0.0, 1.0)]
        public double Weight { get; set; }
    }
}
