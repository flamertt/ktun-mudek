using BitirmeApi.Business.Constants;

namespace BitirmeApi.Business.DTO
{
    public class CreateAssessmentComponentOutcomeMappingDto
    {
        public Guid AssessmentComponentId { get; set; }
        public int ExternalCloId { get; set; }

        /// <summary>"api" | "db". null ise "api" kabul edilir.</summary>
        public string? CloSource { get; set; }

        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
    }

    public class UpdateAssessmentComponentOutcomeMappingDto
    {
        public Guid Id { get; set; }
        public decimal Weight { get; set; }
    }

    public class AssessmentComponentOutcomeMappingDto
    {
        public Guid Id { get; set; }
        public Guid AssessmentComponentId { get; set; }
        public int ExternalCloId { get; set; }

        /// <summary>"api" | "db" | null (legacy = api).</summary>
        public string? CloSource { get; set; }

        /// <summary>Frontend canonical anahtarı: "api:{id}" veya "db:{id}".</summary>
        public string CloKey => $"{(CloSource ?? CloSourceType.Api)}:{ExternalCloId}";

        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ComponentName { get; set; }
    }
}
