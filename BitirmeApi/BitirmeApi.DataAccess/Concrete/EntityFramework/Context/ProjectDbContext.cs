using BitirmeApi.Entity.Entities;
using Microsoft.EntityFrameworkCore;

namespace BitirmeApi.DataAccess.Concrete.EntityFramework.Context
{
    public class ProjectDbContext : DbContext
    {
        public ProjectDbContext(DbContextOptions<ProjectDbContext> options) : base(options) { }

        // ───── Akademik Dönem ────────────────────────────────────────────────────
        public DbSet<AcademicTerm> AcademicTerms => Set<AcademicTerm>();

        // ───── Anket Sistemi ─────────────────────────────────────────────────────
        public DbSet<Survey> Surveys => Set<Survey>();
        public DbSet<Question> Questions => Set<Question>();
        public DbSet<Submission> Submissions => Set<Submission>();
        public DbSet<Answer> Answers => Set<Answer>();

        // ───── MÜDEK Ders Değerlendirme Sistemi ──────────────────────────────────
        public DbSet<CourseEvaluation> CourseEvaluations => Set<CourseEvaluation>();
        public DbSet<Exam> Exams => Set<Exam>();
        public DbSet<ExamQuestion> ExamQuestions => Set<ExamQuestion>();
        public DbSet<ProgramOutcomeContribution> ProgramOutcomeContributions => Set<ProgramOutcomeContribution>();

        // ───── Ölçme/Değerlendirme ───────────────────────────────────────────────
        public DbSet<ExamQuestionOutcomeMapping> ExamQuestionOutcomeMappings => Set<ExamQuestionOutcomeMapping>();
        public DbSet<AssessmentComponent> AssessmentComponents => Set<AssessmentComponent>();
        public DbSet<AssessmentComponentOutcomeMapping> AssessmentComponentOutcomeMappings => Set<AssessmentComponentOutcomeMapping>();

        // ───── Öğrenci Performans Verileri ────────────────────────────────────────
        public DbSet<StudentAssessmentComponentScore> StudentAssessmentComponentScores => Set<StudentAssessmentComponentScore>();
        public DbSet<StudentAnswer> StudentAnswers => Set<StudentAnswer>();

        // ───── Harf notu kuralları (program bazlı) ────────────────────────────────
        public DbSet<LetterGradeRule> LetterGradeRules => Set<LetterGradeRule>();

        // ───── MÜDEK önceden hesaplanmış sonuçlar ────────────────────────────────
        public DbSet<StudentEvaluationResult> StudentEvaluationResults => Set<StudentEvaluationResult>();
        public DbSet<ExamEvaluationResult> ExamEvaluationResults => Set<ExamEvaluationResult>();
        public DbSet<ExamQuestionEvaluationResult> ExamQuestionEvaluationResults => Set<ExamQuestionEvaluationResult>();
        public DbSet<CloEvaluationResult> CloEvaluationResults => Set<CloEvaluationResult>();
        public DbSet<ProgramOutcomeEvaluationResult> ProgramOutcomeEvaluationResults => Set<ProgramOutcomeEvaluationResult>();
        //protected override void OnConfiguring(DbContextOptionsBuilder optionsBuilder)
        //{
        //    optionsBuilder.UseSqlServer((@"Server=(localdb)\mssqllocaldb;database=MudekDbAcademicc;Trusted_Connection=True;"));
        //}
        protected override void OnModelCreating(ModelBuilder b)
        {
            // ═══════════════════════════════════════════════════════════════════════
            // ANKET SİSTEMİ
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<Survey>(e =>
            {
                e.Property(x => x.Title).IsRequired().HasMaxLength(256);
                e.Property(x => x.Description).HasMaxLength(2000);
                e.Property(x => x.IsActive).HasDefaultValue(true);
                e.HasIndex(x => x.ExternalCourseOfferingId);
            });

            b.Entity<Question>(e =>
            {
                e.Property(x => x.Text).IsRequired().HasMaxLength(1000);
                e.Property(x => x.Type).HasConversion<string>().HasMaxLength(16);
                e.Property(x => x.McqOptionsCsv).HasMaxLength(2000);
                e.Property(x => x.CloCode).HasMaxLength(64);
                e.Property(x => x.CloDescription).HasMaxLength(2000);

                e.HasOne(x => x.Survey)
                 .WithMany(s => s.Questions)
                 .HasForeignKey(x => x.SurveyId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<Submission>(e =>
            {
                e.HasOne(x => x.Survey)
                 .WithMany(s => s.Submissions)
                 .HasForeignKey(x => x.SurveyId)
                 .OnDelete(DeleteBehavior.Cascade);

                // Bir öğrenci aynı ankete yalnızca bir kez gönderim yapabilir
                e.HasIndex(x => new { x.SurveyId, x.ExternalStudentId }).IsUnique();
            });

            b.Entity<Answer>(e =>
            {
                e.Property(x => x.ValueNumeric).HasPrecision(5, 2);

                e.HasOne(x => x.Submission)
                 .WithMany(s => s.Answers)
                 .HasForeignKey(x => x.SubmissionId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasOne(x => x.Question)
                 .WithMany(q => q.Answers)
                 .HasForeignKey(x => x.QuestionId)
                 .OnDelete(DeleteBehavior.NoAction);

                e.HasIndex(x => new { x.QuestionId, x.SubmissionId }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // MÜDEK DERS DEĞERLENDİRME
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<CourseEvaluation>(e =>
            {
                e.HasIndex(x => x.ExternalCourseOfferingId).IsUnique();
                e.HasIndex(x => x.ExternalTeacherId);
            });

            b.Entity<Exam>(e =>
            {
                e.Property(x => x.ExamType).IsRequired().HasMaxLength(128);

                e.HasOne(x => x.CourseEvaluation)
                 .WithMany(c => c.Exams)
                 .HasForeignKey(x => x.CourseEvaluationId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ExamQuestion>(e =>
            {
                e.Property(x => x.Title).HasMaxLength(500);
                e.Property(x => x.QuestionType).HasMaxLength(50);
                e.Property(x => x.MaxScore).HasPrecision(7, 2).IsRequired();

                e.HasOne(x => x.Exam)
                 .WithMany(ex => ex.Questions)
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ExamId, x.QuestionNumber }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // PROGRAM ÇIKTI KATKISI
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<ProgramOutcomeContribution>(e =>
            {
                e.Property(x => x.ProgramOutcomeCode).IsRequired().HasMaxLength(64);
                e.Property(x => x.CloCode).HasMaxLength(64);

                e.HasOne(x => x.CourseEvaluation)
                 .WithMany(ce => ce.ProgramOutcomeContributions)
                 .HasForeignKey(x => x.CourseEvaluationId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.CourseEvaluationId, x.ExternalCloId, x.ProgramOutcomeCode }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // ÖLÇME EŞLEMELERİ
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<ExamQuestionOutcomeMapping>(e =>
            {
                e.Property(x => x.Weight).HasPrecision(5, 2).IsRequired();
                e.Property(x => x.CloCode).HasMaxLength(64);
                e.Property(x => x.CloDescription).HasMaxLength(2000);

                e.HasOne(x => x.ExamQuestion)
                 .WithMany(q => q.OutcomeMappings)
                 .HasForeignKey(x => x.ExamQuestionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ExamQuestionId, x.ExternalCloId }).IsUnique();
                e.HasCheckConstraint("CK_ExamQuestionOutcomeMapping_Weight", "[Weight] >= 0 AND [Weight] <= 1");
            });

            b.Entity<AssessmentComponent>(e =>
            {
                e.Property(x => x.Name).IsRequired().HasMaxLength(200);
                e.Property(x => x.ComponentType).IsRequired().HasMaxLength(50);
                e.Property(x => x.MaxScore).HasPrecision(7, 2).IsRequired();
                e.Property(x => x.WeightPercentage).HasPrecision(5, 2);
                e.Property(x => x.Description).HasMaxLength(1000);
                e.Property(x => x.IsActive).HasDefaultValue(true);

                e.HasOne(x => x.Exam)
                 .WithMany()
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ExamId, x.OrderIndex }).IsUnique();
                e.HasCheckConstraint("CK_AssessmentComponent_WeightPercentage",
                    "[WeightPercentage] IS NULL OR ([WeightPercentage] >= 0 AND [WeightPercentage] <= 100)");
            });

            b.Entity<AssessmentComponentOutcomeMapping>(e =>
            {
                e.Property(x => x.Weight).HasPrecision(5, 2).IsRequired();
                e.Property(x => x.CloCode).HasMaxLength(64);
                e.Property(x => x.CloDescription).HasMaxLength(2000);

                e.HasOne(x => x.AssessmentComponent)
                 .WithMany(ac => ac.OutcomeMappings)
                 .HasForeignKey(x => x.AssessmentComponentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.AssessmentComponentId, x.ExternalCloId }).IsUnique();
                e.HasCheckConstraint("CK_AssessmentComponentOutcomeMapping_Weight", "[Weight] >= 0 AND [Weight] <= 1");
            });

            // ═══════════════════════════════════════════════════════════════════════
            // ÖĞRENCİ PERFORMANS VERİLERİ
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<StudentAssessmentComponentScore>(e =>
            {
                e.Property(x => x.Score).HasPrecision(7, 2);
                e.Property(x => x.Notes).HasMaxLength(500);

                e.HasOne(x => x.AssessmentComponent)
                 .WithMany(ac => ac.StudentScores)
                 .HasForeignKey(x => x.AssessmentComponentId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.AssessmentComponentId, x.ExternalStudentId }).IsUnique();
            });

            b.Entity<StudentAnswer>(e =>
            {
                e.Property(x => x.Score).HasPrecision(7, 2).IsRequired();

                e.HasOne(x => x.ExamQuestion)
                 .WithMany(q => q.StudentAnswers)
                 .HasForeignKey(x => x.ExamQuestionId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasIndex(x => new { x.ExamQuestionId, x.ExternalStudentId }).IsUnique();
            });

            // ═══════════════════════════════════════════════════════════════════════
            // HARF NOTU KURALLARI
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<LetterGradeRule>(e =>
            {
                e.Property(x => x.LetterGrade).IsRequired().HasMaxLength(5);
                e.Property(x => x.MinScore).HasPrecision(5, 2).IsRequired();
                e.Property(x => x.MaxScore).HasPrecision(5, 2).IsRequired();
                e.Property(x => x.MinimumFinalScore).HasPrecision(5, 2);
                e.Property(x => x.Description).HasMaxLength(500);

                e.HasIndex(x => new { x.ExternalProgramId, x.LetterGrade }).IsUnique();
                e.HasCheckConstraint("CK_ProgramLetterGradeRule_ScoreRange", "[MaxScore] >= [MinScore]");
                e.HasCheckConstraint("CK_ProgramLetterGradeRule_ScoreBounds", "[MinScore] >= 0 AND [MaxScore] <= 100");
            });

            // ═══════════════════════════════════════════════════════════════════════
            // MÜDEK HESAP SONUÇLARI
            // ═══════════════════════════════════════════════════════════════════════
            b.Entity<StudentEvaluationResult>(e =>
            {
                e.Property(x => x.MidtermScore).HasPrecision(7, 2);
                e.Property(x => x.FinalScore).HasPrecision(7, 2);
                e.Property(x => x.MakeupScore).HasPrecision(7, 2);
                e.Property(x => x.SuccessGrade).HasPrecision(7, 2);
                e.Property(x => x.ExternalStudentNumber).HasMaxLength(64);
                e.Property(x => x.ExternalStudentName).HasMaxLength(256);

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.ExternalStudentId }).IsUnique();
                e.HasIndex(x => x.ExternalCourseOfferingId);
            });

            b.Entity<ExamEvaluationResult>(e =>
            {
                e.Property(x => x.MaxTotalScore).HasPrecision(9, 4);
                e.Property(x => x.MinTotalScore).HasPrecision(9, 4);
                e.Property(x => x.AverageTotalScore).HasPrecision(9, 4);

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.ExamId }).IsUnique();

                e.HasOne(x => x.Exam)
                 .WithMany()
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ExamQuestionEvaluationResult>(e =>
            {
                e.Property(x => x.MaxScore).HasPrecision(7, 2);
                e.Property(x => x.AverageScore).HasPrecision(9, 4);
                e.Property(x => x.AchievementRate).HasPrecision(9, 6);

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.ExamQuestionId })
                 .IsUnique()
                 .HasFilter("[ExamQuestionId] IS NOT NULL");

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.AssessmentComponentId })
                 .IsUnique()
                 .HasFilter("[AssessmentComponentId] IS NOT NULL");

                e.HasCheckConstraint("CK_ExamQuestionEvalResult_SingleTarget",
                    "([ExamQuestionId] IS NOT NULL AND [AssessmentComponentId] IS NULL) OR ([ExamQuestionId] IS NULL AND [AssessmentComponentId] IS NOT NULL)");

                e.HasOne(x => x.Exam)
                 .WithMany()
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Cascade);

                e.HasOne(x => x.ExamQuestion)
                 .WithMany()
                 .HasForeignKey(x => x.ExamQuestionId)
                 .OnDelete(DeleteBehavior.Restrict);

                e.HasOne(x => x.AssessmentComponent)
                 .WithMany()
                 .HasForeignKey(x => x.AssessmentComponentId)
                 .OnDelete(DeleteBehavior.Restrict);
            });

            b.Entity<CloEvaluationResult>(e =>
            {
                e.Property(x => x.AchievementScore).HasPrecision(9, 6);
                e.Property(x => x.CombinedAchievementScore).HasPrecision(9, 6);
                e.Property(x => x.SurveyScore).HasPrecision(9, 6);
                e.Property(x => x.SurveyDifference).HasPrecision(9, 6);
                e.Property(x => x.CloCode).HasMaxLength(64);
                e.Property(x => x.CloDescription).HasMaxLength(2000);

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.ExternalCloId, x.ResultType }).IsUnique();

                e.HasOne(x => x.Exam)
                 .WithMany()
                 .HasForeignKey(x => x.ExamId)
                 .OnDelete(DeleteBehavior.Cascade);
            });

            b.Entity<ProgramOutcomeEvaluationResult>(e =>
            {
                e.Property(x => x.AchievementScore).HasPrecision(9, 6);
                e.Property(x => x.ProgramOutcomeCode).HasMaxLength(64);
                e.Property(x => x.ProgramOutcomeTitle).HasMaxLength(256);

                e.HasIndex(x => new { x.ExternalCourseOfferingId, x.ExternalProgramOutcomeId }).IsUnique();
            });
        }
    }
}
