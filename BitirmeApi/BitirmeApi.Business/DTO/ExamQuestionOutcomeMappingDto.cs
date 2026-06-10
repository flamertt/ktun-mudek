using BitirmeApi.Business.Constants;

namespace BitirmeApi.Business.DTO
{
    public class CreateExamQuestionOutcomeMappingDto
    {
        public Guid ExamQuestionId { get; set; }
        public int ExternalCloId { get; set; }

        /// <summary>"api" | "db". null ise "api" kabul edilir (geriye dönük uyumluluk).</summary>
        public string? CloSource { get; set; }

        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
    }

    public class UpdateExamQuestionOutcomeMappingDto
    {
        public Guid Id { get; set; }
        public decimal Weight { get; set; }
    }

    public class ExamQuestionOutcomeMappingDto
    {
        public Guid Id { get; set; }
        public Guid ExamQuestionId { get; set; }
        public int ExternalCloId { get; set; }

        /// <summary>"api" | "db" | null (legacy = api).</summary>
        public string? CloSource { get; set; }

        /// <summary>Frontend canonical anahtarı: "api:{id}" veya "db:{id}".</summary>
        public string CloKey => $"{(CloSource ?? CloSourceType.Api)}:{ExternalCloId}";

        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
