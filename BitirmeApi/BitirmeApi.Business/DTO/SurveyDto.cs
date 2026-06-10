using System.ComponentModel.DataAnnotations;

namespace BitirmeApi.Business.DTO
{
    public class SurveyListDto
    {
        public Guid Id { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int QuestionCount { get; set; }
        public int SubmissionCount { get; set; }
    }

    public class SurveyDetailDto
    {
        public Guid Id { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public string Title { get; set; } = default!;
        public string? Description { get; set; }
        public bool IsActive { get; set; }
        public DateTime CreatedAt { get; set; }
        public int SubmissionCount { get; set; }
        public List<SurveyQuestionDto> Questions { get; set; } = new();
    }

    public class CreateSurveyDto
    {
        [Required]
        public int ExternalCourseOfferingId { get; set; }

        [Required, MaxLength(256)]
        public string Title { get; set; } = default!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; } = true;
    }

    public class UpdateSurveyDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required, MaxLength(256)]
        public string Title { get; set; } = default!;

        [MaxLength(2000)]
        public string? Description { get; set; }

        public bool IsActive { get; set; }
    }

    public class SurveyQuestionDto
    {
        public Guid Id { get; set; }
        public Guid SurveyId { get; set; }
        public string Text { get; set; } = default!;
        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }
        public int ScaleMin { get; set; }
        public int ScaleMax { get; set; }
        public int? ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
    }

    public class CreateSurveyQuestionDto
    {
        [Required]
        public Guid SurveyId { get; set; }

        [Required, MaxLength(1000)]
        public string Text { get; set; } = default!;

        public int OrderIndex { get; set; } = 1;
        public bool IsRequired { get; set; } = true;

        [Range(0, 9)]
        public int ScaleMin { get; set; } = 0;

        [Range(1, 10)]
        public int ScaleMax { get; set; } = 5;

        public int? ExternalCloId { get; set; }
        [MaxLength(64)]
        public string? CloCode { get; set; }
        [MaxLength(2000)]
        public string? CloDescription { get; set; }
    }

    public class UpdateSurveyQuestionDto
    {
        [Required]
        public Guid Id { get; set; }

        [Required, MaxLength(1000)]
        public string Text { get; set; } = default!;

        public int OrderIndex { get; set; }
        public bool IsRequired { get; set; }

        [Range(0, 9)]
        public int ScaleMin { get; set; }

        [Range(1, 10)]
        public int ScaleMax { get; set; }

        public int? ExternalCloId { get; set; }
        [MaxLength(64)]
        public string? CloCode { get; set; }
        [MaxLength(2000)]
        public string? CloDescription { get; set; }
    }

    public class CloSurveyResultDto
    {
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public int QuestionCount { get; set; }
        public decimal? SurveyScore { get; set; }
        public decimal? MudekScore { get; set; }
        public decimal? Difference { get; set; }
        public string? Evaluation { get; set; }
    }

    public class SurveyQuestionResultDto
    {
        public Guid QuestionId { get; set; }
        public string Text { get; set; } = default!;
        public int OrderIndex { get; set; }
        public int? ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public int ResponseCount { get; set; }
        public decimal? AverageScore { get; set; }
        public decimal? ScorePercentage { get; set; }
        public Dictionary<int, int> ScoreDistribution { get; set; } = new();
    }

    public class SurveyResultsDto
    {
        public Guid SurveyId { get; set; }
        public string Title { get; set; } = default!;
        public int EnrolledStudentCount { get; set; }
        public int TotalSubmissions { get; set; }
        public int EvaluatedSubmissions { get; set; }
        public int NotParticipatedCount { get; set; }
        public int SubmittedButExcludedCount { get; set; }
        public bool IsPassingFilterApplied { get; set; }
        public List<SurveyQuestionResultDto> Questions { get; set; } = new();
        public List<CloSurveyResultDto> CloResults { get; set; } = new();
    }
}
