namespace BitirmeApi.Business.DTO
{
    public class CreateAssessmentComponentOutcomeMappingDto
    {
        public Guid AssessmentComponentId { get; set; }
        public int ExternalCloId { get; set; }
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
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public decimal Weight { get; set; }
        public DateTime CreatedAt { get; set; }
        public string? ComponentName { get; set; }
    }
}
