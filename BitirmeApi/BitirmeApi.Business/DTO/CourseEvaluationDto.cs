namespace BitirmeApi.Business.DTO
{
    public class CourseEvaluationListDto
    {
        public Guid Id { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public int ExternalCourseId { get; set; }
        public int ExternalProgramId { get; set; }
        public int ExternalTeacherId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public int? ExternalAcademicTermId { get; set; }
        public string? AcademicTermName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
        public bool IsCalculationDirty { get; set; }
    }

    public class CourseEvaluationDetailDto
    {
        public Guid Id { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public int ExternalCourseId { get; set; }
        public int ExternalProgramId { get; set; }
        public int ExternalTeacherId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public int? ExternalAcademicTermId { get; set; }
        public string? AcademicTermName { get; set; }
        public DateTime CreatedDate { get; set; }
        public DateTime? UpdatedAt { get; set; }
        public DateTime? LastCalculatedAt { get; set; }
        public bool IsCalculationDirty { get; set; }
        public string? StudentFeedbackEvaluation { get; set; }
        public string? ProgramOutcomeEvaluation { get; set; }
        public string? GeneralEvaluation { get; set; }
        public string? ImprovementSuggestions { get; set; }
    }

    public class CourseEvaluationCreateDto
    {
        public int ExternalCourseOfferingId { get; set; }
        public int ExternalCourseId { get; set; }
        public int ExternalProgramId { get; set; }

        /// <summary>Görüntüleme için ders kodu (denormalized)</summary>
        public string? CourseCode { get; set; }
        /// <summary>Görüntüleme için ders adı (denormalized)</summary>
        public string? CourseName { get; set; }
        /// <summary>Üniversite API'sindeki akademik dönem ID'si; sağlanırsa dönem adı otomatik çözülür.</summary>
        public int? ExternalAcademicTermId { get; set; }
        /// <summary>Görüntüleme için dönem adı (denormalized); boş bırakılırsa ExternalAcademicTermId'den çözülür.</summary>
        public string? AcademicTermName { get; set; }

        public string? StudentFeedbackEvaluation { get; set; }
        public string? ProgramOutcomeEvaluation { get; set; }
        public string? GeneralEvaluation { get; set; }
        public string? ImprovementSuggestions { get; set; }
    }

    public class CourseEvaluationUpdateDto
    {
        public Guid Id { get; set; }
        public string? StudentFeedbackEvaluation { get; set; }
        public string? ProgramOutcomeEvaluation { get; set; }
        public string? GeneralEvaluation { get; set; }
        public string? ImprovementSuggestions { get; set; }
    }
}
