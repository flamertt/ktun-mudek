namespace BitirmeApi.Business.DTO
{
    public class CreateExamQuestionOutcomeMappingDto
    {
        public Guid ExamQuestionId { get; set; }
        public int ExternalCloId { get; set; }
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
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
        public DateTime CreatedAt { get; set; }
    }
}
