using BitirmeApi.Entity.Entities;

namespace BitirmeApi.Business.DTO
{
    // ══════════════════════════════════════════════════════════════════════════════
    // TEMEL RAPOR DTOlari
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>Rapor liste görünümü (özet)</summary>
    public class SemesterReportSummaryDto
    {
        public Guid Id { get; set; }
        public Guid CourseEvaluationId { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? AcademicTermName { get; set; }
        public string? TeacherName { get; set; }
        public SemesterReportStatus Status { get; set; }
        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }
    }

    /// <summary>Rapor detay görünümü (manuel alanlar dahil)</summary>
    public class SemesterReportDetailDto
    {
        public Guid Id { get; set; }
        public Guid CourseEvaluationId { get; set; }
        public int ExternalCourseOfferingId { get; set; }
        public int ExternalTeacherId { get; set; }
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? AcademicTermName { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherTitle { get; set; }
        public SemesterReportStatus Status { get; set; }

        // Kurumsal bilgiler
        public string? UniversityName { get; set; }
        public string? FacultyName { get; set; }
        public string? DepartmentName { get; set; }

        // Bölüm A: Ders bilgileri
        public string? CourseSemester { get; set; }
        public string? CourseCredit { get; set; }
        public string? CourseEcts { get; set; }
        public int? CourseTheoryHours { get; set; }
        public int? CoursePracticeHours { get; set; }
        public string? CourseObjective { get; set; }
        public string? CourseContent { get; set; }
        public string? SectionANotes { get; set; }

        // Tartışma/yorum alanları
        public string? SectionGDiscussion { get; set; }
        public string? SectionHCommentary { get; set; }
        public string? SectionIGeneralEvaluation { get; set; }
        public string? SectionJChangesFromPrevious { get; set; }
        public string? SectionMImprovement { get; set; }

        public string? SignatureName { get; set; }
        public DateTime? SignatureDate { get; set; }

        public DateTime CreatedAt { get; set; }
        public DateTime? UpdatedAt { get; set; }

        public List<SemesterReportWeeklyResourceDto> WeeklyResources { get; set; } = new();
        public List<SemesterReportFileDto> Files { get; set; } = new();
        public List<SemesterReportCloNoteDto> CloNotes { get; set; } = new();
        public List<SemesterReportPloNoteDto> PloNotes { get; set; } = new();
    }

    /// <summary>Rapor oluşturma isteği</summary>
    public class CreateSemesterReportDto
    {
        public int ExternalCourseOfferingId { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherTitle { get; set; }
    }

    /// <summary>Rapor güncelleme isteği (manuel alanlar)</summary>
    public class UpdateSemesterReportDto
    {
        public string? TeacherName { get; set; }
        public string? TeacherTitle { get; set; }
        public string? UniversityName { get; set; }
        public string? FacultyName { get; set; }
        public string? DepartmentName { get; set; }

        // Bölüm A: Ders bilgileri
        public string? CourseSemester { get; set; }
        public string? CourseCredit { get; set; }
        public string? CourseEcts { get; set; }
        public int? CourseTheoryHours { get; set; }
        public int? CoursePracticeHours { get; set; }
        public string? CourseObjective { get; set; }
        public string? CourseContent { get; set; }
        public string? SectionANotes { get; set; }

        // Tartışma/yorum alanları
        public string? SectionGDiscussion { get; set; }
        public string? SectionHCommentary { get; set; }
        public string? SectionIGeneralEvaluation { get; set; }
        public string? SectionJChangesFromPrevious { get; set; }
        public string? SectionMImprovement { get; set; }

        public string? SignatureName { get; set; }
        public DateTime? SignatureDate { get; set; }
        public SemesterReportStatus? Status { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // BÖLÜM B: HAFTALIK KAYNAKLAR
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportWeeklyResourceDto
    {
        public Guid Id { get; set; }
        public int WeekNumber { get; set; }
        public string? Topic { get; set; }
        public string? ResourceType { get; set; }
        public string? ResourceInfo { get; set; }
        public string? ChapterPage { get; set; }
        public string? Description { get; set; }
        public string? ContentSummary { get; set; }
    }

    public class UpsertWeeklyResourceDto
    {
        public int WeekNumber { get; set; }
        public string? Topic { get; set; }
        public string? ResourceType { get; set; }
        public string? ResourceInfo { get; set; }
        public string? ChapterPage { get; set; }
        public string? Description { get; set; }
        public string? ContentSummary { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // DOSYA
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportFileDto
    {
        public Guid Id { get; set; }
        public string SectionCode { get; set; } = string.Empty;
        public string FileCategory { get; set; } = string.Empty;
        public Guid? ExamId { get; set; }
        public string? ExamTypeLabel { get; set; }
        public string OriginalFileName { get; set; } = string.Empty;
        public string? ContentType { get; set; }
        public long FileSize { get; set; }
        public string? Notes { get; set; }
        public DateTime UploadedAt { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // CLO NOTLARI (Bölüm L, M)
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportCloNoteDto
    {
        public Guid Id { get; set; }
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public string? TeacherNote { get; set; }
        public string? ImprovementSuggestion { get; set; }
    }

    public class UpsertCloNoteDto
    {
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public string? TeacherNote { get; set; }
        public string? ImprovementSuggestion { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // PLO NOTLARI (Bölüm K)
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportPloNoteDto
    {
        public Guid Id { get; set; }
        public int ExternalPloId { get; set; }
        public string? PloCode { get; set; }
        public string? PloDescription { get; set; }
        public string? TeacherNote { get; set; }
        public string? ImprovementSuggestion { get; set; }
    }

    public class UpsertPloNoteDto
    {
        public int ExternalPloId { get; set; }
        public string? PloCode { get; set; }
        public string? PloDescription { get; set; }
        public string? TeacherNote { get; set; }
        public string? ImprovementSuggestion { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // DOĞRULAMA
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportValidationDto
    {
        public bool IsComplete { get; set; }
        public List<SemesterReportValidationItem> MissingItems { get; set; } = new();
        public List<SemesterReportValidationItem> Warnings { get; set; } = new();
    }

    public class SemesterReportValidationItem
    {
        public string Section { get; set; } = string.Empty;
        public string Message { get; set; } = string.Empty;
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // BÖLÜM C: ÖRNEK ÖĞRENCİ ÖNERİLERİ
    // ══════════════════════════════════════════════════════════════════════════════

    public class SemesterReportStudentSampleDto
    {
        public int ExternalStudentId { get; set; }
        public string? StudentNumber { get; set; }
        public string? StudentName { get; set; }
        public double? SuccessGrade { get; set; }
        public string? LetterGrade { get; set; }
        public string Level { get; set; } = string.Empty; // "High", "Mid", "Low"
    }

    public class SemesterReportStudentSamplesDto
    {
        public Guid ExamId { get; set; }
        public string? ExamType { get; set; }
        public SemesterReportStudentSampleDto? Highest { get; set; }
        public SemesterReportStudentSampleDto? Middle { get; set; }
        public SemesterReportStudentSampleDto? Lowest { get; set; }
    }

    // ══════════════════════════════════════════════════════════════════════════════
    // BÖLÜM ÖNİZLEME: OTOMATİK HESAPLANAN VERİLER
    // ══════════════════════════════════════════════════════════════════════════════

    /// <summary>Tam rapor önizlemesi — otomatik + manuel veriler bir arada</summary>
    public class SemesterReportPreviewDto
    {
        // Kapak
        public string? CourseCode { get; set; }
        public string? CourseName { get; set; }
        public string? AcademicTermName { get; set; }
        public string? TeacherName { get; set; }
        public string? TeacherTitle { get; set; }

        // Kurumsal bilgiler
        public string? UniversityName { get; set; }
        public string? FacultyName { get; set; }
        public string? DepartmentName { get; set; }

        // Bölüm A: Ders bilgileri
        public string? CourseSemester { get; set; }
        public string? CourseCredit { get; set; }
        public string? CourseEcts { get; set; }
        public int? CourseTheoryHours { get; set; }
        public int? CoursePracticeHours { get; set; }
        public string? CourseObjective { get; set; }
        public string? CourseContent { get; set; }

        // Manuel notlar
        public string? SectionANotes { get; set; }
        public string? SectionGDiscussion { get; set; }
        public string? SectionHCommentary { get; set; }
        public string? SectionIGeneralEvaluation { get; set; }
        public string? SectionJChangesFromPrevious { get; set; }
        public string? SectionMImprovement { get; set; }
        public string? SignatureName { get; set; }
        public DateTime? SignatureDate { get; set; }

        // Bölüm B: Haftalık kaynaklar
        public List<SemesterReportWeeklyResourceDto> WeeklyResources { get; set; } = new();

        // Bölüm D: Ölçme araçları listesi (AssessmentComponent + Exam bazlı)
        public List<SemesterReportAssessmentToolDto> AssessmentTools { get; set; } = new();

        // Bölüm F: Başarı notları tablosu
        public List<SemesterReportStudentGradeDto> StudentGrades { get; set; } = new();

        // Bölüm G: Anket sonuçları (DÖÇ bazlı özet + soru bazlı detay)
        public List<SemesterReportSurveyCloDto> SurveyCloResults { get; set; } = new();
        /// <summary>Soru başına anket dağılımı — sadece geçen öğrenciler</summary>
        public List<SemesterReportSurveyQuestionDto> SurveyQuestions { get; set; } = new();
        public int SurveyPassingStudentCount { get; set; }
        public int SurveyTotalSubmissions { get; set; }
        public string? SurveyDiscussionText { get; set; }

        // Bölüm H: Ders başarı oranları
        public SemesterReportSuccessStatsDto? SuccessStats { get; set; }
        public List<SemesterReportExamStatDto> ExamStats { get; set; } = new();

        // Bölüm K: PÇ başarı tablosu + DÖÇ-PÇ matrisi
        public List<SemesterReportPloResultDto> PloResults { get; set; } = new();
        public List<SemesterReportPloNoteDto> PloNotes { get; set; } = new();
        public SemesterReportCloPlomMatrixDto? CloPlomMatrix { get; set; }

        // Bölüm L: DÖÇ başarı tablosu + soru-DÖÇ ilişkileri
        public List<SemesterReportCloResultDto> CloResults { get; set; } = new();
        public List<SemesterReportCloNoteDto> CloNotes { get; set; } = new();
        public List<SemesterReportQuestionResultDto> QuestionResults { get; set; } = new();

        // Bölüm M: Anket–Ölçme karşılaştırması
        public List<SemesterReportCloComparisonDto> CloComparisons { get; set; } = new();

        // Yüklenen dosyalar (bölüm bazlı)
        public List<SemesterReportFileDto> Files { get; set; } = new();

        // Eksik alanlar
        public SemesterReportValidationDto Validation { get; set; } = new();
    }

    // ── Bölüm D ──────────────────────────────────────────────────────────────────
    public class SemesterReportAssessmentToolDto
    {
        public string Name { get; set; } = string.Empty;
        public string Type { get; set; } = string.Empty;
        public double? WeightPercentage { get; set; }
        public string? Description { get; set; }
        public string? ExamType { get; set; }
    }

    // ── Bölüm F ──────────────────────────────────────────────────────────────────
    public class SemesterReportStudentGradeDto
    {
        public int ExternalStudentId { get; set; }
        public string? StudentNumber { get; set; }
        public string? StudentName { get; set; }
        public double? MidtermScore { get; set; }
        public double? FinalScore { get; set; }
        public double? MakeupScore { get; set; }
        public double? SuccessGrade { get; set; }
        public string? LetterGrade { get; set; }
        public bool IsPassed { get; set; }
    }

    // ── Bölüm G ──────────────────────────────────────────────────────────────────
    public class SemesterReportSurveyCloDto
    {
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public double? LikertAverage { get; set; }
        public double? SurveyPercentage { get; set; }
        public string? Comment { get; set; }
    }

    /// <summary>
    /// Anket sorusu başına istatistik — sadece geçen öğrencilerin yanıtları dahil edilir.
    /// </summary>
    public class SemesterReportSurveyQuestionDto
    {
        public Guid QuestionId { get; set; }
        public string QuestionText { get; set; } = string.Empty;
        public int OrderIndex { get; set; }
        public int ScaleMin { get; set; }
        public int ScaleMax { get; set; }
        public int ResponseCount { get; set; }
        public double? AverageScore { get; set; }
        public double? ScorePercentage { get; set; }
        /// <summary>Likert değeri (1..ScaleMax) → cevap sayısı</summary>
        public Dictionary<int, int> ScoreDistribution { get; set; } = new();
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
    }

    // ── Bölüm H ──────────────────────────────────────────────────────────────────
    public class SemesterReportSuccessStatsDto
    {
        public int TotalStudents { get; set; }
        public int PassedStudents { get; set; }
        public int FailedStudents { get; set; }
        public double SuccessPercentage { get; set; }
        public int GradeFF_FD { get; set; }
        public int GradeDD_DC { get; set; }
        public int GradeCC_Above { get; set; }
        /// <summary>Harf notu → öğrenci sayısı (AA, BA, BB, CB, CC, DC, DD, FD, FF sıralamasında)</summary>
        public Dictionary<string, int> LetterGradeDistribution { get; set; } = new();
    }

    public class SemesterReportExamStatDto
    {
        public Guid ExamId { get; set; }
        public string? ExamType { get; set; }
        public int ParticipantCount { get; set; }
        public double? MaxScore { get; set; }
        public double? MinScore { get; set; }
        public double? AverageScore { get; set; }
        public double? SuccessPercentage { get; set; }
    }

    // ── Bölüm K ──────────────────────────────────────────────────────────────────
    public class SemesterReportPloResultDto
    {
        public int ExternalPloId { get; set; }
        public string? PloCode { get; set; }
        public string? PloTitle { get; set; }
        public string? PloDescription { get; set; }
        public double? AchievementScore { get; set; }
        public string Status { get; set; } = string.Empty; // "Başarılı", "İzlenmeli", "İyileştirme Gerekli"
        public string? AutoComment { get; set; }
    }

    // ── Bölüm L ──────────────────────────────────────────────────────────────────
    public class SemesterReportCloResultDto
    {
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public string? CloDescription { get; set; }
        public double? MidtermAchievement { get; set; }
        public double? FinalAchievement { get; set; }
        public double? MakeupAchievement { get; set; }
        public double? CombinedAchievement { get; set; }
        public string Status { get; set; } = string.Empty;
        public string? AutoComment { get; set; }
    }

    public class SemesterReportQuestionResultDto
    {
        public Guid? ExamQuestionId { get; set; }
        public Guid? AssessmentComponentId { get; set; }
        public string? ExamType { get; set; }
        public int? QuestionNumber { get; set; }
        public string? ComponentName { get; set; }
        public double? MaxScore { get; set; }
        public double? AverageScore { get; set; }
        public double? AchievementRate { get; set; }
        public List<string> LinkedCloCodes { get; set; } = new();
        /// <summary>CloCode → Weight (0-1 arası). Birden fazla DÖÇ bağlıysa ağırlıklar burada.</summary>
        public Dictionary<string, double> LinkedCloWeights { get; set; } = new();
    }

    // ── Bölüm M ──────────────────────────────────────────────────────────────────
    public class SemesterReportCloComparisonDto
    {
        public int ExternalCloId { get; set; }
        public string? CloCode { get; set; }
        public double? MeasurementResult { get; set; }
        public double? SurveyResult { get; set; }
        public double? Difference { get; set; }
        public string? Evaluation { get; set; } // "Tutarlı", "Kısmi Farklılık", "Belirgin Fark"
        public string? AutoComment { get; set; }
    }

    // ── Bölüm K: DÖÇ-PÇ İlişki Matrisi ─────────────────────────────────────────
    public class SemesterReportCloPlomMatrixDto
    {
        /// <summary>PÇ kodları — sütun başlıkları</summary>
        public List<string> PloCodes { get; set; } = new();

        /// <summary>PÇ açıklamaları — sütun başlığına tooltip için</summary>
        public List<string?> PloDescriptions { get; set; } = new();

        /// <summary>DÖÇ satırları</summary>
        public List<SemesterReportCloPlomRowDto> Rows { get; set; } = new();
    }

    public class SemesterReportCloPlomRowDto
    {
        public string CloCode { get; set; } = string.Empty;
        public string? CloDescription { get; set; }
        /// <summary>Her PÇ için ağırlık (PloCodes sırasıyla eşleşir). null = ilişki yok</summary>
        public List<double?> Weights { get; set; } = new();
    }
}
