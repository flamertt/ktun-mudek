using System;
using Microsoft.EntityFrameworkCore.Migrations;

#nullable disable

namespace BitirmeApi.DataAccess.Migrations
{
    /// <inheritdoc />
    public partial class MudekDb : Migration
    {
        /// <inheritdoc />
        protected override void Up(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.CreateTable(
                name: "AcademicTerms",
                columns: table => new
                {
                    Id = table.Column<int>(type: "int", nullable: false),
                    Ad = table.Column<string>(type: "nvarchar(100)", maxLength: 100, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AcademicTerms", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "CourseEvaluations",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExternalCourseId = table.Column<int>(type: "int", nullable: false),
                    ExternalProgramId = table.Column<int>(type: "int", nullable: false),
                    ExternalTeacherId = table.Column<int>(type: "int", nullable: false),
                    CourseCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CourseName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AcademicTermName = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: true),
                    CreatedDate = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    LastCalculatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    IsCalculationDirty = table.Column<bool>(type: "bit", nullable: false),
                    StudentFeedbackEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ProgramOutcomeEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    GeneralEvaluation = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    ImprovementSuggestions = table.Column<string>(type: "nvarchar(max)", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CourseEvaluations", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "LetterGradeRules",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalProgramId = table.Column<int>(type: "int", nullable: false),
                    LetterGrade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: false),
                    MinScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    IsPassing = table.Column<bool>(type: "bit", nullable: false),
                    MinimumFinalScore = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_LetterGradeRules", x => x.Id);
                    table.CheckConstraint("CK_ProgramLetterGradeRule_ScoreBounds", "[MinScore] >= 0 AND [MaxScore] <= 100");
                    table.CheckConstraint("CK_ProgramLetterGradeRule_ScoreRange", "[MaxScore] >= [MinScore]");
                });

            migrationBuilder.CreateTable(
                name: "ProgramOutcomeEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExternalProgramOutcomeId = table.Column<int>(type: "int", nullable: false),
                    ProgramOutcomeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProgramOutcomeTitle = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    AchievementScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramOutcomeEvaluationResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "StudentEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExternalStudentId = table.Column<int>(type: "int", nullable: false),
                    ExternalStudentNumber = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ExternalStudentName = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: true),
                    MidtermScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    FinalScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    MakeupScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    UsedExamType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    SuccessGrade = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    LetterGrade = table.Column<string>(type: "nvarchar(5)", maxLength: 5, nullable: true),
                    IsPassed = table.Column<bool>(type: "bit", nullable: false),
                    IncludedInStatistics = table.Column<bool>(type: "bit", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentEvaluationResults", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Surveys",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    Title = table.Column<string>(type: "nvarchar(256)", maxLength: 256, nullable: false),
                    Description = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Surveys", x => x.Id);
                });

            migrationBuilder.CreateTable(
                name: "Exams",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamType = table.Column<string>(type: "nvarchar(128)", maxLength: 128, nullable: false),
                    WeightPercentage = table.Column<double>(type: "float", nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Exams", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Exams_CourseEvaluations_CourseEvaluationId",
                        column: x => x.CourseEvaluationId,
                        principalTable: "CourseEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ProgramOutcomeContributions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    CourseEvaluationId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCloId = table.Column<int>(type: "int", nullable: false),
                    CloCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    ProgramOutcomeCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: false),
                    ContributionLevel = table.Column<int>(type: "int", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ProgramOutcomeContributions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ProgramOutcomeContributions_CourseEvaluations_CourseEvaluationId",
                        column: x => x.CourseEvaluationId,
                        principalTable: "CourseEvaluations",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Questions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Text = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: false),
                    Type = table.Column<string>(type: "nvarchar(16)", maxLength: 16, nullable: false),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    IsRequired = table.Column<bool>(type: "bit", nullable: false),
                    ScaleMin = table.Column<int>(type: "int", nullable: false),
                    ScaleMax = table.Column<int>(type: "int", nullable: false),
                    Options = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    McqOptionsCsv = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ExternalCloId = table.Column<int>(type: "int", nullable: true),
                    CloCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CloDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Questions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Questions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Submissions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SurveyId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalStudentId = table.Column<int>(type: "int", nullable: false),
                    SubmittedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    IncludeInStatistics = table.Column<bool>(type: "bit", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Submissions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Submissions_Surveys_SurveyId",
                        column: x => x.SurveyId,
                        principalTable: "Surveys",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComponents",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    Name = table.Column<string>(type: "nvarchar(200)", maxLength: 200, nullable: false),
                    ComponentType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    WeightPercentage = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    OrderIndex = table.Column<int>(type: "int", nullable: false),
                    Description = table.Column<string>(type: "nvarchar(1000)", maxLength: 1000, nullable: true),
                    IsActive = table.Column<bool>(type: "bit", nullable: false, defaultValue: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComponents", x => x.Id);
                    table.CheckConstraint("CK_AssessmentComponent_WeightPercentage", "[WeightPercentage] IS NULL OR ([WeightPercentage] >= 0 AND [WeightPercentage] <= 100)");
                    table.ForeignKey(
                        name: "FK_AssessmentComponents_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "CloEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExternalCloId = table.Column<int>(type: "int", nullable: false),
                    CloCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CloDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    ResultType = table.Column<string>(type: "nvarchar(32)", maxLength: 32, nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AchievementScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    CombinedAchievementScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    SurveyScore = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    SurveyDifference = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_CloEvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_CloEvaluationResults_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ParticipantCount = table.Column<int>(type: "int", nullable: false),
                    IncludedStudentCount = table.Column<int>(type: "int", nullable: false),
                    PerfectScoreCount = table.Column<int>(type: "int", nullable: false),
                    MaxTotalScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    MinTotalScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    AverageTotalScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamEvaluationResults", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamEvaluationResults_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestions",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    Title = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    Description = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    QuestionType = table.Column<string>(type: "nvarchar(50)", maxLength: 50, nullable: true),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestions", x => x.Id);
                    table.ForeignKey(
                        name: "FK_ExamQuestions_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "Answers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    SubmissionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    QuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ValueNumeric = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: true),
                    ValueText = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_Answers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_Answers_Questions_QuestionId",
                        column: x => x.QuestionId,
                        principalTable: "Questions",
                        principalColumn: "Id");
                    table.ForeignKey(
                        name: "FK_Answers_Submissions_SubmissionId",
                        column: x => x.SubmissionId,
                        principalTable: "Submissions",
                        principalColumn: "Id");
                });

            migrationBuilder.CreateTable(
                name: "AssessmentComponentOutcomeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCloId = table.Column<int>(type: "int", nullable: false),
                    CloCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CloDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_AssessmentComponentOutcomeMappings", x => x.Id);
                    table.CheckConstraint("CK_AssessmentComponentOutcomeMapping_Weight", "[Weight] >= 0 AND [Weight] <= 1");
                    table.ForeignKey(
                        name: "FK_AssessmentComponentOutcomeMappings_AssessmentComponents_AssessmentComponentId",
                        column: x => x.AssessmentComponentId,
                        principalTable: "AssessmentComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAssessmentComponentScores",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    AssessmentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalStudentId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: true),
                    Notes = table.Column<string>(type: "nvarchar(500)", maxLength: 500, nullable: true),
                    EvaluatedBy = table.Column<string>(type: "nvarchar(max)", nullable: true),
                    EvaluatedAt = table.Column<DateTime>(type: "datetime2", nullable: true),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: true)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAssessmentComponentScores", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAssessmentComponentScores_AssessmentComponents_AssessmentComponentId",
                        column: x => x.AssessmentComponentId,
                        principalTable: "AssessmentComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestionEvaluationResults",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCourseOfferingId = table.Column<int>(type: "int", nullable: false),
                    ExamId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    AssessmentComponentId = table.Column<Guid>(type: "uniqueidentifier", nullable: true),
                    QuestionNumber = table.Column<int>(type: "int", nullable: false),
                    MaxScore = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false),
                    AverageScore = table.Column<decimal>(type: "decimal(9,4)", precision: 9, scale: 4, nullable: true),
                    AchievementRate = table.Column<decimal>(type: "decimal(9,6)", precision: 9, scale: 6, nullable: true),
                    IncludedStudentCount = table.Column<int>(type: "int", nullable: false),
                    UpdatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestionEvaluationResults", x => x.Id);
                    table.CheckConstraint("CK_ExamQuestionEvalResult_SingleTarget", "([ExamQuestionId] IS NOT NULL AND [AssessmentComponentId] IS NULL) OR ([ExamQuestionId] IS NULL AND [AssessmentComponentId] IS NOT NULL)");
                    table.ForeignKey(
                        name: "FK_ExamQuestionEvaluationResults_AssessmentComponents_AssessmentComponentId",
                        column: x => x.AssessmentComponentId,
                        principalTable: "AssessmentComponents",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamQuestionEvaluationResults_ExamQuestions_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "ExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Restrict);
                    table.ForeignKey(
                        name: "FK_ExamQuestionEvaluationResults_Exams_ExamId",
                        column: x => x.ExamId,
                        principalTable: "Exams",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "ExamQuestionOutcomeMappings",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalCloId = table.Column<int>(type: "int", nullable: false),
                    CloCode = table.Column<string>(type: "nvarchar(64)", maxLength: 64, nullable: true),
                    CloDescription = table.Column<string>(type: "nvarchar(2000)", maxLength: 2000, nullable: true),
                    Weight = table.Column<decimal>(type: "decimal(5,2)", precision: 5, scale: 2, nullable: false),
                    CreatedAt = table.Column<DateTime>(type: "datetime2", nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_ExamQuestionOutcomeMappings", x => x.Id);
                    table.CheckConstraint("CK_ExamQuestionOutcomeMapping_Weight", "[Weight] >= 0 AND [Weight] <= 1");
                    table.ForeignKey(
                        name: "FK_ExamQuestionOutcomeMappings_ExamQuestions_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "ExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateTable(
                name: "StudentAnswers",
                columns: table => new
                {
                    Id = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExamQuestionId = table.Column<Guid>(type: "uniqueidentifier", nullable: false),
                    ExternalStudentId = table.Column<int>(type: "int", nullable: false),
                    Score = table.Column<decimal>(type: "decimal(7,2)", precision: 7, scale: 2, nullable: false)
                },
                constraints: table =>
                {
                    table.PrimaryKey("PK_StudentAnswers", x => x.Id);
                    table.ForeignKey(
                        name: "FK_StudentAnswers_ExamQuestions_ExamQuestionId",
                        column: x => x.ExamQuestionId,
                        principalTable: "ExamQuestions",
                        principalColumn: "Id",
                        onDelete: ReferentialAction.Cascade);
                });

            migrationBuilder.CreateIndex(
                name: "IX_Answers_QuestionId_SubmissionId",
                table: "Answers",
                columns: new[] { "QuestionId", "SubmissionId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Answers_SubmissionId",
                table: "Answers",
                column: "SubmissionId");

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponentOutcomeMappings_AssessmentComponentId_ExternalCloId",
                table: "AssessmentComponentOutcomeMappings",
                columns: new[] { "AssessmentComponentId", "ExternalCloId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_AssessmentComponents_ExamId_OrderIndex",
                table: "AssessmentComponents",
                columns: new[] { "ExamId", "OrderIndex" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CloEvaluationResults_ExamId",
                table: "CloEvaluationResults",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_CloEvaluationResults_ExternalCourseOfferingId_ExternalCloId_ResultType",
                table: "CloEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "ExternalCloId", "ResultType" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEvaluations_ExternalCourseOfferingId",
                table: "CourseEvaluations",
                column: "ExternalCourseOfferingId",
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_CourseEvaluations_ExternalTeacherId",
                table: "CourseEvaluations",
                column: "ExternalTeacherId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamEvaluationResults_ExamId",
                table: "ExamEvaluationResults",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamEvaluationResults_ExternalCourseOfferingId_ExamId",
                table: "ExamEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "ExamId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionEvaluationResults_AssessmentComponentId",
                table: "ExamQuestionEvaluationResults",
                column: "AssessmentComponentId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionEvaluationResults_ExamId",
                table: "ExamQuestionEvaluationResults",
                column: "ExamId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionEvaluationResults_ExamQuestionId",
                table: "ExamQuestionEvaluationResults",
                column: "ExamQuestionId");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionEvaluationResults_ExternalCourseOfferingId_AssessmentComponentId",
                table: "ExamQuestionEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "AssessmentComponentId" },
                unique: true,
                filter: "[AssessmentComponentId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionEvaluationResults_ExternalCourseOfferingId_ExamQuestionId",
                table: "ExamQuestionEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "ExamQuestionId" },
                unique: true,
                filter: "[ExamQuestionId] IS NOT NULL");

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestionOutcomeMappings_ExamQuestionId_ExternalCloId",
                table: "ExamQuestionOutcomeMappings",
                columns: new[] { "ExamQuestionId", "ExternalCloId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ExamQuestions_ExamId_QuestionNumber",
                table: "ExamQuestions",
                columns: new[] { "ExamId", "QuestionNumber" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Exams_CourseEvaluationId",
                table: "Exams",
                column: "CourseEvaluationId");

            migrationBuilder.CreateIndex(
                name: "IX_LetterGradeRules_ExternalProgramId_LetterGrade",
                table: "LetterGradeRules",
                columns: new[] { "ExternalProgramId", "LetterGrade" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOutcomeContributions_CourseEvaluationId_ExternalCloId_ProgramOutcomeCode",
                table: "ProgramOutcomeContributions",
                columns: new[] { "CourseEvaluationId", "ExternalCloId", "ProgramOutcomeCode" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_ProgramOutcomeEvaluationResults_ExternalCourseOfferingId_ExternalProgramOutcomeId",
                table: "ProgramOutcomeEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "ExternalProgramOutcomeId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Questions_SurveyId",
                table: "Questions",
                column: "SurveyId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentAnswers_ExamQuestionId_ExternalStudentId",
                table: "StudentAnswers",
                columns: new[] { "ExamQuestionId", "ExternalStudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentAssessmentComponentScores_AssessmentComponentId_ExternalStudentId",
                table: "StudentAssessmentComponentScores",
                columns: new[] { "AssessmentComponentId", "ExternalStudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluationResults_ExternalCourseOfferingId",
                table: "StudentEvaluationResults",
                column: "ExternalCourseOfferingId");

            migrationBuilder.CreateIndex(
                name: "IX_StudentEvaluationResults_ExternalCourseOfferingId_ExternalStudentId",
                table: "StudentEvaluationResults",
                columns: new[] { "ExternalCourseOfferingId", "ExternalStudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Submissions_SurveyId_ExternalStudentId",
                table: "Submissions",
                columns: new[] { "SurveyId", "ExternalStudentId" },
                unique: true);

            migrationBuilder.CreateIndex(
                name: "IX_Surveys_ExternalCourseOfferingId",
                table: "Surveys",
                column: "ExternalCourseOfferingId");
        }

        /// <inheritdoc />
        protected override void Down(MigrationBuilder migrationBuilder)
        {
            migrationBuilder.DropTable(
                name: "AcademicTerms");

            migrationBuilder.DropTable(
                name: "Answers");

            migrationBuilder.DropTable(
                name: "AssessmentComponentOutcomeMappings");

            migrationBuilder.DropTable(
                name: "CloEvaluationResults");

            migrationBuilder.DropTable(
                name: "ExamEvaluationResults");

            migrationBuilder.DropTable(
                name: "ExamQuestionEvaluationResults");

            migrationBuilder.DropTable(
                name: "ExamQuestionOutcomeMappings");

            migrationBuilder.DropTable(
                name: "LetterGradeRules");

            migrationBuilder.DropTable(
                name: "ProgramOutcomeContributions");

            migrationBuilder.DropTable(
                name: "ProgramOutcomeEvaluationResults");

            migrationBuilder.DropTable(
                name: "StudentAnswers");

            migrationBuilder.DropTable(
                name: "StudentAssessmentComponentScores");

            migrationBuilder.DropTable(
                name: "StudentEvaluationResults");

            migrationBuilder.DropTable(
                name: "Questions");

            migrationBuilder.DropTable(
                name: "Submissions");

            migrationBuilder.DropTable(
                name: "ExamQuestions");

            migrationBuilder.DropTable(
                name: "AssessmentComponents");

            migrationBuilder.DropTable(
                name: "Surveys");

            migrationBuilder.DropTable(
                name: "Exams");

            migrationBuilder.DropTable(
                name: "CourseEvaluations");
        }
    }
}
